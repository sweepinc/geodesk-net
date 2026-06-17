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
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

// TODO: Rename to "Cursor"?

// PORT: Java's Query implements Iterator<Feature> and Bounds. In .NET it implements
// IEnumerator<Feature> (so it can back a foreach over a view) and Bounds. The Java
// Iterator surface (HasNext()/Next()) is preserved as well, because other ported
// iterators (e.g. NodeParentView) call Query.Next() directly and rely on it returning
// null once the query is exhausted.
public class Query : IEnumerator<Feature>, Bounds
{
    private readonly FeatureStore store;
    private int minX;
    private readonly int minY;
    private int maxX;
    private readonly int maxY;
    private readonly int types;
    private readonly Matcher matcher;
    private ExecutorService executor;
    private TileIndexWalker tileWalker;
    private QueryResults currentResults;
    private int currentPos;
    private Feature? nextFeature;
    private int pendingTiles;
    private bool allTilesRequested;
    private BlockingQueue<TileQueryTask> queue;
    private volatile Exception? error;

    private Feature? enumeratorCurrent;

    public Query(WorldView view)
    {
        this.store = view.store;
        this.executor = store.Executor();
        this.types = view.types;
        this.matcher = view.matcher;
        Bounds bbox = view.bounds;
        minX = bbox.MinX;
        minY = bbox.MinY;
        maxX = bbox.MaxX;
        maxY = bbox.MaxY;
        queue = new LinkedBlockingQueue<TileQueryTask>();
        tileWalker = new TileIndexWalker(store);
        currentResults = QueryResults.EMPTY;
        Start(view.filter);
    }

    public FeatureStore Store()
    {
        return store;
    }

    public int Types()
    {
        return types;
    }

    public Matcher Matcher()
    {
        return matcher;
    }

    public int MinX => minX;

    public int MinY => minY;

    public int MaxX => maxX;

    public int MaxY => maxY;

    internal void Put(TileQueryTask task)
    {
        // TODO

        try
        {
            if (!queue.Add(task))
            {
                Log.Error("Couldn't add");
            }
        }
        catch (Exception e)
        {
            Log.Error("%s", e);
        }
    }

    internal void SetError(Exception error)
    {
        this.error = error;
            // TODO: proper type for query-related exceptions
    }

    internal TileQueryTask? Take()
    {
        try
        {
            return queue.Take();
        }
        catch (InterruptedException)
        {
            return null;
        }
    }

    public void Start(Filter? filter)
    {
        tileWalker.Start(this, filter);
        currentResults = QueryResults.EMPTY;
        currentPos = -1;

        // Submit initial tasks
        int maxPendingTiles = store.MaxPendingTiles();
        while (pendingTiles < maxPendingTiles)
        {
            RequestTile();
            if (!tileWalker.Next())
            {
                // We've traversed all tiles
                allTilesRequested = true;
                break;
            }
        }
        FetchNext();
    }

    private void RequestTile()
    {
        ForkJoinPool pool = (ForkJoinPool)executor; // TODO!
        int entry = store.TileIndexEntry(tileWalker.Tip());
        if ((entry & 2) != 0)
        {
            pool.Submit(new TileQueryTask(this, (int)((uint)entry >> 2),
                tileWalker.NorthwestFlags(), tileWalker.CurrentFilter()));
            pendingTiles++;
        }
        else
        {
            tileWalker.SkipChildren();
        }
    }

    private void FetchNext()
    {
        currentPos++;
        for (; ; )
        {
            if (currentPos >= currentResults.size)
            {
                // We're finished with the current batch of results

                currentPos = 0;
                if (currentResults.next == null)
                {
                    // We've consumed all retrieved results

                    if (pendingTiles == 0)
                    {
                        // no further tasks are pending, we're done
                        nextFeature = null;
                        return;
                    }

                    // Retrieve the next task from the queue, blocking if necessary

                    TileQueryTask task = Take()!;
                    pendingTiles -= task.TilesProcessed();
                    while (!allTilesRequested)
                    {
                        RequestTile();
                        if (tileWalker.Next())
                        {
                            if (pendingTiles == store.MaxPendingTiles()) break;
                        }
                        else
                        {
                            allTilesRequested = true;
                        }
                    }

                    currentResults = task.GetRawResult();
                    continue;    // go back to loop since batch could be empty
                }
                currentResults = currentResults.next;
                continue;   // go back to loop since batch could be empty
            }

            NioBuffer buf = currentResults.buf!;
            int pFeature = currentResults.pointers[currentPos];
            int type = pFeature & 3;
            pFeature ^= type;
            if (type == 1)
            {
                nextFeature = new StoredWay(store, buf, pFeature);
                return;
            }
            if (type == 0)
            {
                nextFeature = new StoredNode(store, buf, pFeature);
                return;
            }
            System.Diagnostics.Debug.Assert(type == 2);
            nextFeature = new StoredRelation(store, buf, pFeature);
            return;
        }
    }

    public bool HasNext()
    {
        if (nextFeature != null) return true;
        if (error != null) throw error;
        return false;
    }

    public Feature? Next()
    {
        Feature? f = nextFeature;
        FetchNext();
        return f;
    }

    // --- IEnumerator<Feature> adapter over the Java Iterator surface ---

    public Feature Current => enumeratorCurrent!;

    object IEnumerator.Current => enumeratorCurrent!;

    public bool MoveNext()
    {
        if (!HasNext()) return false;
        enumeratorCurrent = Next();
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
