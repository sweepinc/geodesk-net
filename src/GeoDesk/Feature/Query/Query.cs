/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Query;

// TODO: Rename to "Cursor"?

// PORT: Java's Query implements Iterator<Feature> and Bounds. In .NET it implements
// IEnumerator<Feature> (so it can back a foreach over a view) and Bounds. The Java Iterator
// surface (HasNext()/Next()) is preserved as well, because other ported iterators (e.g.
// NodeParentView) call Query.Next() directly and rely on it returning null once the query is
// exhausted.
//
// PORT: the Java engine used a ForkJoinPool shared by the store, throttling submitted tiles via
// a pending-count and a BlockingCollection. In .NET the tiles are scanned by a background producer
// (Parallel.ForEachAsync, bounded to MaxPendingTiles) that writes each tile's QueryResults into a
// bounded Channel; the consumer (this cursor) drains the Channel synchronously. Within a tile the
// index buckets are scanned in parallel by TileScanner. Cancellation is wired through _cts so that
// abandoning the cursor (Dispose) stops the producer before the store can unmap its buffers.
/// <remarks>Ported from Java <c>com.geodesk.feature.query.Query</c>.</remarks>
internal class Query : IEnumerator<IFeature>, IBounds
{

    readonly FeatureStore _store;

    readonly int _minX;
    readonly int _minY;
    readonly int _maxX;
    readonly int _maxY;

    readonly int _types;
    readonly Matcher _matcher;
    readonly TileIndexWalker _tileWalker;

    readonly Channel<QueryResults> _channel;
    readonly CancellationTokenSource _cts;
    Task _producer = Task.CompletedTask;

    QueryResults _currentResults;
    int _currentPos;
    IFeature? _nextFeature;
    volatile Exception? _error;

    IFeature? _enumeratorCurrent;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query(WorldView)</c>.</remarks>
    public Query(WorldView view)
    {
        _store = view.store;
        _types = view.types;
        _matcher = view.matcher;

        var bbox = view.bounds;
        _minX = bbox.MinX;
        _minY = bbox.MinY;
        _maxX = bbox.MaxX;
        _maxY = bbox.MaxY;

        _channel = Channel.CreateBounded<QueryResults>(new BoundedChannelOptions(_store.MaxPendingTiles)
        {
            SingleReader = true,
            SingleWriter = false,
        });
        _cts = CancellationTokenSource.CreateLinkedTokenSource(_store.QueryCancellation);
        _tileWalker = new TileIndexWalker(_store);
        _currentResults = QueryResults.Empty;

        Start(view.filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.store()</c>.</remarks>
    public FeatureStore Store => _store;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.types()</c>.</remarks>
    public int Types => _types;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.matcher()</c>.</remarks>
    public Matcher Matcher => _matcher;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.minX()</c>.</remarks>
    public int MinX => _minX;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.minY()</c>.</remarks>
    public int MinY => _minY;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.maxX()</c>.</remarks>
    public int MaxX => _maxX;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.maxY()</c>.</remarks>
    public int MaxY => _maxY;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.start(Filter)</c>.</remarks>
    public void Start(IFilter? filter)
    {
        _currentResults = QueryResults.Empty;
        _currentPos = -1;

        _producer = ProduceAsync(filter, _cts.Token);
        _store.TrackProducer(_producer);

        FetchNext();
    }

    // PORT: replaces requestTile() + the ForkJoinPool submission throttle. Bounded by
    // MaxPendingTiles via Parallel.ForEachAsync; each tile's results are written to the channel.
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.requestTile()</c>.</remarks>
    async Task ProduceAsync(IFilter? filter, CancellationToken ct)
    {
        try
        {
            await Parallel.ForEachAsync(
                EnumerateTiles(filter),
                new ParallelOptions { MaxDegreeOfParallelism = _store.MaxPendingTiles, CancellationToken = ct },
                async (tile, ct2) =>
                {
                    var results = await new TileScanner(this, tile.Page, tile.Flags, tile.Filter).ScanAsync();
                    await _channel.Writer.WriteAsync(results, ct2);
                }).ConfigureAwait(false);

            _channel.Writer.TryComplete();
        }
        catch (OperationCanceledException)
        {
            _channel.Writer.TryComplete();
        }
        catch (Exception ex)
        {
            _channel.Writer.TryComplete(ex);
        }
    }

    // PORT: the leaf-tile walk from start()/requestTile(), expressed as a lazy sequence. The walker
    // is stateful and not thread-safe, but Parallel.ForEachAsync enumerates the source on a single
    // thread, while the scan bodies run in parallel over the already-extracted TileRefs.
    IEnumerable<TileRef> EnumerateTiles(IFilter? filter)
    {
        _tileWalker.Start(this, filter);
        do
        {
            var entry = _store.TileIndexEntry(_tileWalker.Tip());
            if (FeatureStore.IsTileLoadedAndCurrent(entry))
                yield return new TileRef(FeatureStore.PageFromEntry(entry), _tileWalker.NorthwestFlags(), _tileWalker.CurrentFilter());
            else
                _tileWalker.SkipChildren();
        }
        while (_tileWalker.Next());
    }

    // PORT: replaces take(). Blocks the consumer until the next tile's results arrive; returns null
    // once the producer has completed and the channel is drained. A producer fault surfaces as the
    // channel's completion exception, which is stashed and re-thrown from HasNext().
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.take()</c>.</remarks>
    QueryResults? TakeBatch()
    {
        try
        {
            return _channel.Reader.ReadAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (ChannelClosedException e)
        {
            _error = e.InnerException;
            return null;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.fetchNext()</c>.</remarks>
    void FetchNext()
    {
        _currentPos++;
        for (; ; )
        {
            if (_currentPos >= _currentResults.size)
            {
                // We're finished with the current batch of results

                _currentPos = 0;
                if (_currentResults.next == null)
                {
                    // We've consumed all retrieved results; block for the next tile's results

                    var batch = TakeBatch();
                    if (batch == null)
                    {
                        // producer is done (or faulted): no more results
                        _nextFeature = null;
                        return;
                    }

                    _currentResults = batch;
                    continue;    // go back to loop since batch could be empty
                }

                _currentResults = _currentResults.next;
                continue;   // go back to loop since batch could be empty
            }

            var buf = _currentResults.buf;
            var pFeature = _currentResults.pointers[_currentPos];
            var type = pFeature & 3;
            pFeature ^= type;

            if (type == 1)
            {
                _nextFeature = new StoredWay(_store, buf, pFeature);
                return;
            }

            if (type == 0)
            {
                _nextFeature = new StoredNode(_store, buf, pFeature);
                return;
            }

            System.Diagnostics.Debug.Assert(type == 2);
            _nextFeature = new StoredRelation(_store, buf, pFeature);
            return;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.hasNext()</c>.</remarks>
    public bool HasNext()
    {
        if (_nextFeature != null)
            return true;

        if (_error != null)
            throw _error;

        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.next()</c>.</remarks>
    public IFeature? Next()
    {
        var f = _nextFeature;
        FetchNext();
        return f;
    }

    // --- IEnumerator<Feature> adapter over the Java Iterator surface ---

    public IFeature Current => _enumeratorCurrent!;

    object IEnumerator.Current => _enumeratorCurrent!;

    public bool MoveNext()
    {
        if (!HasNext())
            return false;
        _enumeratorCurrent = Next();
        return true;
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }

    // PORT: stops the background producer and waits for any in-flight tile scans to finish before
    // returning, so the store can safely unmap buffers once all cursors are disposed.
    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _producer.Wait();
        }
        catch
        {
            // producer completes via cancellation/fault; nothing to surface here
        }
        _cts.Dispose();
    }

    // The data a tile scan needs, extracted from the (single-threaded) tile walk so the parallel
    // scan bodies never touch the stateful walker.
    readonly struct TileRef
    {
        public readonly int Page;
        public readonly int Flags;
        public readonly IFilter? Filter;

        public TileRef(int page, int flags, IFilter? filter)
        {
            Page = page;
            Flags = flags;
            Filter = filter;
        }
    }

}
