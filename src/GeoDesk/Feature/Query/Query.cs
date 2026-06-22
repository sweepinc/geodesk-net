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
/// <summary>
/// Executes a spatial feature query over a <see cref="FeatureStore"/> and exposes the matching features as
/// a forward-only cursor. A background producer walks the tile index within the query's bounding box,
/// scans each loaded tile in parallel, and feeds batches of results through a bounded channel; this object
/// drains that channel, materializing one feature at a time. Supports both the synchronous Java iterator
/// surface (<see cref="HasNext"/>/<see cref="Next"/>) and the .NET <see cref="IEnumerator{T}"/> /
/// <see cref="IAsyncEnumerator{T}"/> patterns.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.Query</c>.</remarks>
internal class Query : IEnumerator<IFeature>, IAsyncEnumerator<IFeature>, IBounds
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
    public Query(WorldView view) :
        this(view, prefetch: true)
    {
    }

    /// <summary>
    /// Core constructor. When <paramref name="prefetch"/> is false the constructor does not block on
    /// the first batch — async consumers let <see cref="MoveNextAsync"/> drive the fetching — and
    /// <paramref name="externalCt"/> links an <c>await foreach</c> cancellation token into this query's
    /// cancellation source.
    /// </summary>
    internal Query(WorldView view, bool prefetch, CancellationToken externalCt = default)
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
        _cts = CancellationTokenSource.CreateLinkedTokenSource(_store.QueryCancellation, externalCt);
        _tileWalker = new TileIndexWalker(_store);
        _currentResults = QueryResults.Empty;

        Start(view.filter, prefetch);
    }

    /// <summary>
    /// The feature store this query runs against.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.store()</c>.</remarks>
    public FeatureStore Store => _store;

    /// <summary>
    /// The bit mask of feature types this query accepts.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.types()</c>.</remarks>
    public int Types => _types;

    /// <summary>
    /// The matcher that decides whether a candidate feature satisfies the query's tag criteria.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.matcher()</c>.</remarks>
    public Matcher Matcher => _matcher;

    /// <summary>
    /// The minimum X (west) bound of the query's bounding box, in Mercator imps.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.minX()</c>.</remarks>
    public int MinX => _minX;

    /// <summary>
    /// The minimum Y (south) bound of the query's bounding box, in Mercator imps.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.minY()</c>.</remarks>
    public int MinY => _minY;

    /// <summary>
    /// The maximum X (east) bound of the query's bounding box, in Mercator imps.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.maxX()</c>.</remarks>
    public int MaxX => _maxX;

    /// <summary>
    /// The maximum Y (north) bound of the query's bounding box, in Mercator imps.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.maxY()</c>.</remarks>
    public int MaxY => _maxY;

    /// <summary>
    /// (Re)starts the query with the given optional spatial filter, launching the background tile producer
    /// and prefetching the first result.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.start(Filter)</c>.</remarks>
    public void Start(IFilter? filter)
    {
        Start(filter, prefetch: true);
    }

    /// <summary>
    /// Starts the background tile-scanning producer and, for synchronous consumers
    /// (<paramref name="prefetch"/> true), prefetches the first feature. Async consumers pass false so
    /// the constructor never blocks; <see cref="MoveNextAsync"/> drives the fetching instead.
    /// </summary>
    void Start(IFilter? filter, bool prefetch)
    {
        _currentResults = QueryResults.Empty;
        _currentPos = -1;

        _producer = ProduceAsync(filter, _cts.Token);
        _store.TrackProducer(_producer);

        // Sync consumers prefetch the first feature here (blocking); async consumers skip this and
        // let MoveNextAsync await the first batch instead.
        if (prefetch)
            FetchNext();
    }

    // PORT: replaces requestTile() + the ForkJoinPool submission throttle. Bounded by
    // MaxPendingTiles via Parallel.ForEachAsync; each tile's results are written to the channel.
    /// <summary>
    /// The background producer: walks the candidate tiles in the query's bounding box, scans each in
    /// parallel (bounded by the store's max pending tiles), and writes each tile's results to the channel,
    /// completing the channel (with any fault) when finished.
    /// </summary>
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
    /// <summary>
    /// Blocks until the next tile's results arrive, returning null once the producer has completed and the
    /// channel is drained. A producer fault is stashed in <c>_error</c> and re-thrown later from
    /// <see cref="HasNext"/>.
    /// </summary>
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

    /// <summary>
    /// The async counterpart of <see cref="TakeBatch"/>: awaits the next tile's results instead of
    /// blocking the calling thread, so a web request enumerating a query releases its thread between
    /// tiles. Returns null when the channel completes; a producer fault is stashed in the error field.
    /// </summary>
    async ValueTask<QueryResults?> TakeBatchAsync(CancellationToken ct)
    {
        try
        {
            return await _channel.Reader.ReadAsync(ct).ConfigureAwait(false);
        }
        catch (ChannelClosedException e)
        {
            _error = e.InnerException;
            return null;
        }
    }

    /// <summary>
    /// Advances to the next matching feature, walking across result batches and blocking for the next tile's
    /// results at a batch boundary. Sets <c>_nextFeature</c> to null once the query is exhausted.
    /// </summary>
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

            BuildFeatureAtCurrentPos();
            return;
        }
    }

    /// <summary>
    /// The async counterpart of <see cref="FetchNext"/>: walks the result batches the same way, but
    /// awaits the next tile's results at a batch boundary instead of blocking. Within a batch it
    /// completes synchronously.
    /// </summary>
    async ValueTask FetchNextAsync(CancellationToken ct)
    {
        _currentPos++;
        for (; ; )
        {
            if (_currentPos >= _currentResults.size)
            {
                _currentPos = 0;
                if (_currentResults.next == null)
                {
                    var batch = await TakeBatchAsync(ct).ConfigureAwait(false);
                    if (batch == null)
                    {
                        _nextFeature = null;
                        return;
                    }

                    _currentResults = batch;
                    continue;
                }

                _currentResults = _currentResults.next;
                continue;
            }

            BuildFeatureAtCurrentPos();
            return;
        }
    }

    /// <summary>
    /// Materializes the feature at the current batch position into the next-feature field. The low two
    /// bits of the stored pointer encode the feature type (0 = node, 1 = way, 2 = relation).
    /// </summary>
    void BuildFeatureAtCurrentPos()
    {
        var segment = _currentResults.segment
            ?? throw new InvalidOperationException("Query results have no backing segment");
        var pFeature = _currentResults.pointers[_currentPos];
        var type = pFeature & 3;
        pFeature ^= type;

        if (type == 1)
        {
            _nextFeature = new StoredWay(_store, segment, pFeature);
        }
        else if (type == 0)
        {
            _nextFeature = new StoredNode(_store, segment, pFeature);
        }
        else
        {
            System.Diagnostics.Debug.Assert(type == 2);
            _nextFeature = new StoredRelation(_store, segment, pFeature);
        }
    }

    /// <summary>
    /// Returns whether another feature is available, re-throwing any fault raised by the background producer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.hasNext()</c>.</remarks>
    public bool HasNext()
    {
        if (_nextFeature != null)
            return true;

        if (_error != null)
            throw _error;

        return false;
    }

    /// <summary>
    /// Returns the current feature and advances the cursor to the next one. Returns null when the query is
    /// exhausted.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.next()</c>.</remarks>
    public IFeature? Next()
    {
        var f = _nextFeature;
        FetchNext();
        return f;
    }

    // --- IEnumerator<Feature> adapter over the Java Iterator surface ---

    /// <summary>
    /// The feature at the current position of the synchronous enumeration.
    /// </summary>
    public IFeature Current => _enumeratorCurrent!;

    /// <summary>
    /// Non-generic current-element accessor, returning the current feature boxed as <see cref="object"/>.
    /// </summary>
    object IEnumerator.Current => _enumeratorCurrent!;

    /// <summary>
    /// Advances the synchronous enumerator to the next feature, returning false once the query is
    /// exhausted. This may block while the next tile's results are produced.
    /// </summary>
    public bool MoveNext()
    {
        if (!HasNext())
            return false;
        _enumeratorCurrent = Next();
        return true;
    }

    /// <summary>
    /// Not supported — a streaming query cursor cannot be rewound; always throws
    /// <see cref="NotSupportedException"/>.
    /// </summary>
    public void Reset()
    {
        throw new NotSupportedException();
    }

    // --- IAsyncEnumerator<Feature>: the non-blocking consumer path. MoveNextAsync awaits the next
    // batch only at tile boundaries; within a tile's results it completes synchronously. ---

    /// <summary>
    /// Advances the async cursor to the next feature, awaiting the next tile's results only at a batch
    /// boundary. Returns false when the query is exhausted, and rethrows any producer fault.
    /// </summary>
    public async ValueTask<bool> MoveNextAsync()
    {
        await FetchNextAsync(_cts.Token).ConfigureAwait(false);
        if (_nextFeature == null)
        {
            if (_error != null)
                throw _error;
            return false;
        }

        _enumeratorCurrent = _nextFeature;
        return true;
    }

    // PORT: async counterpart of Dispose — awaits the background producer (after cancelling) rather
    // than blocking on it, so the store can safely unmap buffers once all cursors are disposed.
    /// <summary>
    /// Asynchronously disposes the cursor: cancels and then awaits the background producer (rather than
    /// blocking on it), so the store can safely unmap its buffers once all cursors are disposed.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        try
        {
            await _producer.ConfigureAwait(false);
        }
        catch
        {
            // producer completes via cancellation/fault; nothing to surface here
        }
        _cts.Dispose();
    }

    // PORT: stops the background producer and waits for any in-flight tile scans to finish before
    // returning, so the store can safely unmap buffers once all cursors are disposed.
    /// <summary>
    /// Disposes the cursor: cancels the background producer and blocks until any in-flight tile scans
    /// finish, so the store can safely unmap its buffers once all cursors are disposed.
    /// </summary>
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

    /// <summary>
    /// The data a single tile scan needs (page, bbox flags, filter), extracted from the
    /// single-threaded tile walk so the parallel scan bodies never touch the stateful walker.
    /// </summary>
    readonly struct TileRef
    {
        public readonly int Page;
        public readonly int Flags;
        public readonly IFilter? Filter;

        /// <summary>
        /// Captures the page, bbox flags, and filter for one tile to be scanned.
        /// </summary>
        public TileRef(int page, int flags, IFilter? filter)
        {
            Page = page;
            Flags = flags;
            Filter = filter;
        }
    }

}
