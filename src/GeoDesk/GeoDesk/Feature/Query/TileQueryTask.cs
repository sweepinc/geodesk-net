/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

// TODO: Maybe give task the tile page instead of TIP
public class TileQueryTask : QueryTask
{
    private readonly int tilePage;
    // PORT: accessed by sibling RTreeQueryTask instances (parent.buf etc.), which C#
    // protected does not allow across sibling types, so these are internal.
    internal int bboxFlags;
    private int tilesProcessed;
    internal NioBuffer? buf;
    internal Filter? filter;

    public TileQueryTask(Query query, int tilePage, int northwestFlags, Filter? filter)
        : base(query)
    {
        this.tilePage = tilePage;
        this.bboxFlags = northwestFlags;
        this.filter = filter;
        // Log.debug("Tile %s with filter %s", Tile.toString(tile), filter);
    }

    private RTreeQueryTask? SearchRTree(int ppTree, Matcher matcher, RTreeQueryTask? task)
    {
        int p = buf!.GetInt(ppTree);
        if (p == 0) return task;
        p = ppTree + p;
        for (; ; )
        {
            int last = buf.GetInt(p) & 1;
            int keyBits = buf.GetInt(p + 4);
            if (matcher.AcceptIndex(keyBits))
            {
                task = new RTreeQueryTask(this, p, matcher, task);
                task.Fork();
            }
            if (last != 0) break;
            p += 8;
        }
        return task;
    }

    private RTreeQueryTask? SearchNodeRTree(int ppTree, Matcher matcher, RTreeQueryTask? task)
    {
        int p = buf!.GetInt(ppTree);
        if (p == 0) return task;
        p = ppTree + p;
        for (; ; )
        {
            int last = buf.GetInt(p) & 1;
            int keyBits = buf.GetInt(p + 4);
            if (matcher.AcceptIndex(keyBits))
            {
                task = new RTreeQueryTask.Nodes(this, p, matcher, task);
                task.Fork();
            }
            if (last != 0) break;
            p += 8;
        }
        return task;
    }

    protected override bool Exec()
    {
        // System.out.format("Searching tile at page %d\n", tilePage);

        try
        {
            FeatureStore store = query.Store();
            buf = store.BufferOfPage(tilePage);
            int pTile = store.OffsetOfPage(tilePage);

            Matcher matcher = query.Matcher();
            RTreeQueryTask? task = null;

            int types = query.Types();
            if ((types & TypeBits.NODES) != 0)
            {
                task = SearchNodeRTree(pTile + 8, matcher, task);
            }
            if ((types & TypeBits.NONAREA_WAYS) != 0)
            {
                task = SearchRTree(pTile + 12, matcher, task);
            }
            if ((types & TypeBits.AREAS) != 0)
            {
                task = SearchRTree(pTile + 16, matcher, task);
            }
            if ((types & TypeBits.NONAREA_RELATIONS) != 0)
            {
                task = SearchRTree(pTile + 20, matcher, task);
            }

            QueryResults res = QueryResults.EMPTY;
            while (task != null)
            {
                res = QueryResults.Merge(res, task.Join());
                task = task.next;
            }
            results = res;
        }
        catch (Exception ex)
        {
            query.SetError(ex);
            results = QueryResults.EMPTY;
        }
        tilesProcessed = 1;
        query.Put(this);
        return true;
    }

    public int TilesProcessed()
    {
        return tilesProcessed;
    }
}
