/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

// TODO: Maybe give task the tile page instead of TIP
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask</c>.</remarks>
internal class TileQueryTask : QueryTask
{

    readonly int _tilePage;
    // PORT: accessed by sibling RTreeQueryTask instances (parent.buf etc.), which C# protected
    // does not allow across sibling types, so these are internal.
    internal int bboxFlags;
    int _tilesProcessed;
    internal NioBuffer? buf;
    internal IFilter? filter;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask(Query, int, int, Filter)</c>.</remarks>
    public TileQueryTask(Query query, int tilePage, int northwestFlags, IFilter? filter)
        : base(query)
    {
        _tilePage = tilePage;
        bboxFlags = northwestFlags;
        this.filter = filter;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchRTree(int, Matcher, RTreeQueryTask)</c>.</remarks>
    RTreeQueryTask? SearchRTree(int ppTree, Matcher matcher, RTreeQueryTask? task)
    {
        var p = buf!.GetInt(ppTree);
        if (p == 0) return task;
        p = ppTree + p;
        for (; ; )
        {
            var last = buf.GetInt(p) & 1;
            var keyBits = buf.GetInt(p + 4);
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

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchNodeRTree(int, Matcher, RTreeQueryTask)</c>.</remarks>
    RTreeQueryTask? SearchNodeRTree(int ppTree, Matcher matcher, RTreeQueryTask? task)
    {
        var p = buf!.GetInt(ppTree);
        if (p == 0) return task;
        p = ppTree + p;
        for (; ; )
        {
            var last = buf.GetInt(p) & 1;
            var keyBits = buf.GetInt(p + 4);
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

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.exec()</c>.</remarks>
    protected override bool Exec()
    {
        try
        {
            var store = query.Store();
            buf = store.BufferOfPage(_tilePage);
            var pTile = store.OffsetOfPage(_tilePage);

            var matcher = query.Matcher();
            RTreeQueryTask? task = null;

            var types = query.Types();
            if ((types & TypeBits.NODES) != 0) task = SearchNodeRTree(pTile + 8, matcher, task);
            if ((types & TypeBits.NONAREA_WAYS) != 0) task = SearchRTree(pTile + 12, matcher, task);
            if ((types & TypeBits.AREAS) != 0) task = SearchRTree(pTile + 16, matcher, task);
            if ((types & TypeBits.NONAREA_RELATIONS) != 0) task = SearchRTree(pTile + 20, matcher, task);

            var res = QueryResults.Empty;
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
            results = QueryResults.Empty;
        }
        _tilesProcessed = 1;
        query.Put(this);
        return true;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.tilesProcessed()</c>.</remarks>
    public int TilesProcessed()
    {
        return _tilesProcessed;
    }

}
