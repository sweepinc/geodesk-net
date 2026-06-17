/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Clarisma.Common.Util;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

using Java.Util.Concurrent;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

// TODO: Rename to "Cursor"?

// PORT: Java's Query implements Iterator<Feature> and Bounds. In .NET it implements
// IEnumerator<Feature> (so it can back a foreach over a view) and Bounds. The Java Iterator
// surface (HasNext()/Next()) is preserved as well, because other ported iterators (e.g.
// NodeParentView) call Query.Next() directly and rely on it returning null once the query is
// exhausted.
/// <remarks>Ported from Java <c>com.geodesk.feature.query.Query</c>.</remarks>
public class Query : IEnumerator<Feature>, Bounds
{

    readonly FeatureStore _store;
    int _minX;
    readonly int _minY;
    int _maxX;
    readonly int _maxY;
    readonly int _types;
    readonly Matcher _matcher;
    readonly ExecutorService _executor;
    readonly TileIndexWalker _tileWalker;
    QueryResults _currentResults;
    int _currentPos;
    Feature? _nextFeature;
    int _pendingTiles;
    bool _allTilesRequested;
    readonly BlockingQueue<TileQueryTask> _queue;
    volatile Exception? _error;

    Feature? _enumeratorCurrent;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query(WorldView)</c>.</remarks>
    public Query(WorldView view)
    {
        _store = view.store;
        _executor = _store.Executor();
        _types = view.types;
        _matcher = view.matcher;
        var bbox = view.bounds;
        _minX = bbox.MinX;
        _minY = bbox.MinY;
        _maxX = bbox.MaxX;
        _maxY = bbox.MaxY;
        _queue = new LinkedBlockingQueue<TileQueryTask>();
        _tileWalker = new TileIndexWalker(_store);
        _currentResults = QueryResults.Empty;
        Start(view.filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.store()</c>.</remarks>
    public FeatureStore Store()
    {
        return _store;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.types()</c>.</remarks>
    public int Types()
    {
        return _types;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.matcher()</c>.</remarks>
    public Matcher Matcher()
    {
        return _matcher;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.minX()</c>.</remarks>
    public int MinX => _minX;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.minY()</c>.</remarks>
    public int MinY => _minY;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.maxX()</c>.</remarks>
    public int MaxX => _maxX;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.maxY()</c>.</remarks>
    public int MaxY => _maxY;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.put(TileQueryTask)</c>.</remarks>
    internal void Put(TileQueryTask task)
    {
        // TODO

        try
        {
            if (!_queue.Add(task)) Log.Error("Couldn't add");
        }
        catch (Exception e)
        {
            Log.Error("%s", e);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.setError(Throwable)</c>.</remarks>
    internal void SetError(Exception error)
    {
        _error = error;
            // TODO: proper type for query-related exceptions
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.take()</c>.</remarks>
    internal TileQueryTask? Take()
    {
        try
        {
            return _queue.Take();
        }
        catch (InterruptedException)
        {
            return null;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.start(Filter)</c>.</remarks>
    public void Start(Filter? filter)
    {
        _tileWalker.Start(this, filter);
        _currentResults = QueryResults.Empty;
        _currentPos = -1;

        // Submit initial tasks
        var maxPendingTiles = _store.MaxPendingTiles();
        while (_pendingTiles < maxPendingTiles)
        {
            RequestTile();
            if (!_tileWalker.Next())
            {
                // We've traversed all tiles
                _allTilesRequested = true;
                break;
            }
        }
        FetchNext();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.requestTile()</c>.</remarks>
    void RequestTile()
    {
        var pool = (ForkJoinPool)_executor; // TODO!
        var entry = _store.TileIndexEntry(_tileWalker.Tip());
        if ((entry & 2) != 0)
        {
            pool.Submit(new TileQueryTask(this, (int)((uint)entry >> 2),
                _tileWalker.NorthwestFlags(), _tileWalker.CurrentFilter()));
            _pendingTiles++;
        }
        else
        {
            _tileWalker.SkipChildren();
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
                    // We've consumed all retrieved results

                    if (_pendingTiles == 0)
                    {
                        // no further tasks are pending, we're done
                        _nextFeature = null;
                        return;
                    }

                    // Retrieve the next task from the queue, blocking if necessary

                    var task = Take()!;
                    _pendingTiles -= task.TilesProcessed();
                    while (!_allTilesRequested)
                    {
                        RequestTile();
                        if (_tileWalker.Next())
                        {
                            if (_pendingTiles == _store.MaxPendingTiles()) break;
                        }
                        else
                        {
                            _allTilesRequested = true;
                        }
                    }

                    _currentResults = task.GetRawResult();
                    continue;    // go back to loop since batch could be empty
                }
                _currentResults = _currentResults.next;
                continue;   // go back to loop since batch could be empty
            }

            var buf = _currentResults.buf!;
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
        if (_nextFeature != null) return true;
        if (_error != null) throw _error;
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.Query.next()</c>.</remarks>
    public Feature? Next()
    {
        var f = _nextFeature;
        FetchNext();
        return f;
    }

    // --- IEnumerator<Feature> adapter over the Java Iterator surface ---

    public Feature Current => _enumeratorCurrent!;

    object IEnumerator.Current => _enumeratorCurrent!;

    public bool MoveNext()
    {
        if (!HasNext()) return false;
        _enumeratorCurrent = Next();
        return true;
    }

    public void Reset()
    {
        throw new NotSupportedException();
    }

    public void Dispose()
    {
    }

}
