/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Threading.Tasks;

using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

// PORT: replaces the Java fork/join pair TileQueryTask + RTreeQueryTask. A tile is scanned by
// forking one Task per accepted index bucket (Task.Run), then awaiting them in order and merging —
// the .NET-idiomatic equivalent of the original fork()/join()/merge loop. The recursive R-tree
// descent (SearchTrunk/SearchLeaf) is folded in from RTreeQueryTask and runs synchronously inside
// each bucket task.
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask</c> + <c>RTreeQueryTask</c>.</remarks>
internal sealed class TileScanner
{

    readonly FeatureStore _store;
    readonly NioBuffer _buf;
    readonly int _pTile;
    readonly int _bboxFlags;
    readonly int _types;
    readonly Matcher _matcher;
    readonly IFilter? _filter;
    readonly int _minX;
    readonly int _minY;
    readonly int _maxX;
    readonly int _maxY;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="query"></param>
    /// <param name="tilePage"></param>
    /// <param name="bboxFlags"></param>
    /// <param name="filter"></param>
    public TileScanner(Query query, int tilePage, int bboxFlags, IFilter? filter)
    {
        _store = query.Store;
        _buf = new NioBuffer(_store.SegmentOfPage(tilePage).Memory);
        _pTile = _store.OffsetOfPage(tilePage);
        _bboxFlags = bboxFlags;
        _types = query.Types;
        _matcher = query.Matcher;
        _filter = filter;
        _minX = query.MinX;
        _minY = query.MinY;
        _maxX = query.MaxX;
        _maxY = query.MaxY;
    }

    // Returns ValueTask: a tile with no accepted buckets completes synchronously (returns
    // QueryResults.Empty without awaiting), so ValueTask avoids allocating a Task on that path. The
    // single caller awaits the result exactly once, which is the required ValueTask usage.
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.exec()</c>.</remarks>
    public async ValueTask<QueryResults> ScanAsync()
    {
        // Fork one task per accepted index bucket across the requested type categories...
        var branches = new List<Task<QueryResults>>();
        if ((_types & TypeBits.NODES) != 0)
            ForkBuckets(branches, _pTile + 8, nodes: true);
        if ((_types & TypeBits.NONAREA_WAYS) != 0)
            ForkBuckets(branches, _pTile + 12, nodes: false);
        if ((_types & TypeBits.AREAS) != 0)
            ForkBuckets(branches, _pTile + 16, nodes: false);
        if ((_types & TypeBits.NONAREA_RELATIONS) != 0)
            ForkBuckets(branches, _pTile + 20, nodes: false);

        // ...then await them in order, merging (each branch produced its own QueryResults).
        var res = QueryResults.Empty;
        foreach (var t in branches)
            res = QueryResults.Merge(res, await t);
        return res;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchRTree(int, Matcher, RTreeQueryTask)</c>.</remarks>
    void ForkBuckets(List<Task<QueryResults>> branches, int ppTree, bool nodes)
    {
        var p = _buf.GetInt(ppTree);
        if (p == 0)
            return;

        p = ppTree + p;
        for (; ; )
        {
            var last = _buf.GetInt(p) & 1;
            var keyBits = _buf.GetInt(p + 4);
            if (_matcher.AcceptIndex(keyBits))
            {
                var branch = p; // capture a copy — p advances in this loop
                branches.Add(Task.Run(() => SearchBranch(branch, nodes)));
            }
            if (last != 0)
                break;

            p += 8;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.exec()</c>.</remarks>
    QueryResults SearchBranch(int p, bool nodes)
    {
        var results = new QueryResults(_buf);
        var ptr = _buf.GetInt(p);
        SearchTrunk(results, p + (int)((uint)ptr & 0xffff_fffc), nodes);
        return results;
    }

    /// <summary>
    /// Returns true if the bounding box stored at <paramref name="pBounds"/> (minX, minY, maxX, maxY
    /// as four consecutive ints) intersects the query bounding box.
    /// </summary>
    bool IntersectsQueryBounds(int pBounds)
    {
        return (_buf.GetInt(pBounds) > _maxX || _buf.GetInt(pBounds + 4) > _maxY || _buf.GetInt(pBounds + 8) < _minX || _buf.GetInt(pBounds + 12) < _minY) == false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchTrunk(int)</c>.</remarks>
    void SearchTrunk(QueryResults results, int p, bool nodes)
    {
        for (; ; )
        {
            var ptr = _buf.GetInt(p);
            var last = ptr & 1;

            if (IntersectsQueryBounds(p + 4))
            {
                if ((ptr & 2) != 0)
                {
                    if (nodes)
                        SearchLeafNodes(results, p + (ptr ^ 2 ^ last));
                    else
                        SearchLeaf(results, p + (ptr ^ 2 ^ last));
                }
                else
                {
                    SearchTrunk(results, p + (ptr ^ last), nodes);
                }
            }
            if (last != 0)
                break;

            p += 20;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchLeaf(int)</c>.</remarks>
    void SearchLeaf(QueryResults results, int p)
    {
        for (; ; )
        {
            var flags = _buf.GetInt(p + 16);
            if ((flags & _bboxFlags) == 0)
            {
                if (IntersectsQueryBounds(p))
                {
                    if (((1 << (flags >> 1)) & _types) != 0)
                    {
                        var pFeature = p + 16;
                        if (_matcher.Accept(_buf, pFeature))
                            if (_filter == null || _filter.Accept(_store.GetFeature(_buf, pFeature)))
                                results.Add(pFeature | (int)(((uint)flags >> 3) & 3));
                    }
                }
            }
            if ((flags & 1) != 0)
                break;

            p += 32;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.Nodes.searchLeaf(int)</c>.</remarks>
    void SearchLeafNodes(QueryResults results, int p)
    {
        for (; ; )
        {
            var flags = _buf.GetInt(p + 8);
            var x = _buf.GetInt(p);
            var y = _buf.GetInt(p + 4);
            if (!(x > _maxX || y > _maxY || x < _minX || y < _minY))
            {
                var pFeature = p + 8;
                if (_matcher.Accept(_buf, pFeature))
                {
                    if (_filter == null || _filter.Accept(new StoredNode(_store, _buf, pFeature)))
                        results.Add(pFeature);
                }
            }
            if ((flags & 1) != 0)
                break;

            p += 20 + (flags & 4);
        }
    }

}
