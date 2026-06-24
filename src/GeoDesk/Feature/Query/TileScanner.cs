/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using GeoDesk.Common.Store;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

// PORT: replaces the Java fork/join pair TileQueryTask + RTreeQueryTask. A tile is scanned by
// forking one Task per accepted index bucket (Task.Run), then awaiting them in order and merging —
// the .NET-idiomatic equivalent of the original fork()/join()/merge loop. The recursive R-tree
// descent (SearchTrunk/SearchLeaf) is folded in from RTreeQueryTask and runs synchronously inside
// each bucket task.
/// <summary>
/// Scans a single tile for features matching a query, descending the per-category
/// spatial R-trees and collecting matching feature pointers. Index buckets are scanned
/// concurrently and their results merged.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask</c> + <c>RTreeQueryTask</c>.</remarks>
internal sealed class TileScanner
{

    readonly FeatureStore _store;
    readonly Segment _segment;
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
    /// Creates a scanner for the given tile page within a query, capturing the query's
    /// bounding box, type, matcher, and filter constraints.
    /// </summary>
    /// <param name="query">the owning query supplying the search parameters</param>
    /// <param name="tilePage">the first page of the tile to scan</param>
    /// <param name="bboxFlags">flags describing how the tile relates to the query bounding box</param>
    /// <param name="filter">an optional per-tile filter</param>
    public TileScanner(Query query, int tilePage, int bboxFlags, IFilter? filter)
    {
        _store = query.Store;
        _segment = _store.SegmentOfPage(tilePage);
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

    // PORT: the Java original (TileQueryTask) forks one fork/join task per accepted index bucket, then
    // joins and merges them. We keep that bucket-level parallelism but apply the standard fork/join trick
    // of running one branch on the current thread: every accepted bucket EXCEPT the last is forked to the
    // thread pool, the last is scanned inline here, then the forked ones are awaited and merged. So a tile
    // with a single accepted bucket (the common case) forks nothing — and because this is an async
    // ValueTask method that completes synchronously on the zero- and single-bucket paths, no Task and no
    // async-state-machine heap allocation occur there either. Multi-bucket tiles suspend to await the forks
    // (releasing the thread rather than blocking it). Tile-level parallelism comes from the producer
    // (one task per tile via Parallel.ForEachAsync).
    /// <summary>
    /// Scans the tile, collecting matching features across the requested type categories. Accepted index
    /// buckets are scanned in parallel via the fork-all-but-last pattern; the single-bucket case runs
    /// entirely inline.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.exec()</c>.</remarks>
    public async ValueTask<QueryResults> ScanAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Walk the type indexes, forking all accepted buckets but the last (returned via pendingP/Nodes).
        var forked = ForkBuckets(ct, out var pendingP, out var pendingNodes);
        if (pendingP < 0)
            return QueryResults.Empty;

        // Scan the last accepted bucket inline (concurrently with the forked ones). Then merge the forked
        // buckets first — in walk order — and append the inline one last, so the result order matches a
        // plain sequential scan (the order isn't contractual, but keeping it avoids surprising callers).
        var lastRes = SearchBranch(pendingP, pendingNodes, ct);
        var res = QueryResults.Empty;
        if (forked != null)
        {
            foreach (var t in forked)
            {
                ct.ThrowIfCancellationRequested();
                res = QueryResults.Merge(res, await t.ConfigureAwait(false));
            }
        }
        return QueryResults.Merge(res, lastRes);
    }

    /// <summary>
    /// Walks the four type indexes, forking a task to search each accepted bucket's R-tree except the most
    /// recently seen one, which is returned in <paramref name="pendingP"/>/<paramref name="pendingNodes"/>
    /// (or <paramref name="pendingP"/> &lt; 0 if none was accepted) so the caller can scan it inline.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchRTree</c> / <c>searchNodeRTree</c>.</remarks>
    List<Task<QueryResults>>? ForkBuckets(CancellationToken ct, out int pendingP, out bool pendingNodes)
    {
        List<Task<QueryResults>>? forked = null;
        var pP = -1;
        var pN = false;

        // Local function (never turned into a delegate, so no allocation): each accepted bucket becomes the
        // new pending bucket; the previously pending one is forked. This yields fork-all-but-last across all
        // four indexes in one pass.
        void WalkTree(int ppTree, bool nodes)
        {
            var buf = new NioBuffer(_segment.Memory);
            var p = buf.GetInt(ppTree);
            if (p == 0)
                return;

            p = ppTree + p;
            for (; ; )
            {
                var last = buf.GetInt(p) & 1;
                var keyBits = buf.GetInt(p + 4);
                if (_matcher.AcceptIndex(keyBits))
                {
                    if (pP >= 0)
                    {
                        var branchP = pP;       // capture copies — pP/pN advance below
                        var branchNodes = pN;
                        (forked ??= new List<Task<QueryResults>>()).Add(
                            Task.Run(() => SearchBranch(branchP, branchNodes, ct), ct));
                    }
                    pP = p;
                    pN = nodes;
                }
                if (last != 0)
                    break;

                p += 8;
            }
        }

        if ((_types & TypeBits.NODES) != 0)
            WalkTree(_pTile + 8, nodes: true);
        if ((_types & TypeBits.NONAREA_WAYS) != 0)
            WalkTree(_pTile + 12, nodes: false);
        if ((_types & TypeBits.AREAS) != 0)
            WalkTree(_pTile + 16, nodes: false);
        if ((_types & TypeBits.NONAREA_RELATIONS) != 0)
            WalkTree(_pTile + 20, nodes: false);

        pendingP = pP;
        pendingNodes = pN;
        return forked;
    }

    /// <summary>
    /// Searches the R-tree rooted at one index bucket, descending from its root trunk and collecting
    /// matching features into a fresh <see cref="QueryResults"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.exec()</c>.</remarks>
    QueryResults SearchBranch(int p, bool nodes, CancellationToken ct)
    {
        var results = new QueryResults(_segment);
        var ptr = new NioBuffer(_segment.Memory).GetInt(p);
        SearchTrunk(results, p + (int)((uint)ptr & 0xffff_fffc), nodes, ct);
        return results;
    }

    /// <summary>
    /// Returns true if the bounding box stored at <paramref name="pBounds"/> (minX, minY, maxX, maxY
    /// as four consecutive ints) intersects the query bounding box.
    /// </summary>
    bool IntersectsQueryBounds(int pBounds)
    {
        var buf = new NioBuffer(_segment.Memory);
        return (buf.GetInt(pBounds) > _maxX || buf.GetInt(pBounds + 4) > _maxY || buf.GetInt(pBounds + 8) < _minX || buf.GetInt(pBounds + 12) < _minY) == false;
    }

    /// <summary>
    /// Recursively descends an R-tree trunk node, pruning children whose bounding box does not
    /// intersect the query box and recursing into child trunks or scanning leaf nodes for the rest.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchTrunk(int)</c>.</remarks>
    void SearchTrunk(QueryResults results, int p, bool nodes, CancellationToken ct)
    {
        var buf = new NioBuffer(_segment.Memory);
        for (; ; )
        {
            ct.ThrowIfCancellationRequested();
            var ptr = buf.GetInt(p);
            var last = ptr & 1;

            if (IntersectsQueryBounds(p + 4))
            {
                if ((ptr & 2) != 0)
                {
                    if (nodes)
                        SearchLeafNodes(results, p + (ptr ^ 2 ^ last), ct);
                    else
                        SearchLeaf(results, p + (ptr ^ 2 ^ last), ct);
                }
                else
                {
                    SearchTrunk(results, p + (ptr ^ last), nodes, ct);
                }
            }
            if (last != 0)
                break;

            p += 20;
        }
    }

    /// <summary>
    /// Scans a leaf node of way/area/relation entries, testing each against the bbox flags, query box,
    /// type mask, matcher, and optional filter, and adding the survivors (with their type bits) to the
    /// results.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchLeaf(int)</c>.</remarks>
    void SearchLeaf(QueryResults results, int p, CancellationToken ct)
    {
        var buf = new NioBuffer(_segment.Memory);
        for (; ; )
        {
            ct.ThrowIfCancellationRequested();

            var flags = buf.GetInt(p + 16);
            if ((flags & _bboxFlags) == 0)
            {
                if (IntersectsQueryBounds(p))
                {
                    if (((1 << (flags >> 1)) & _types) != 0)
                    {
                        var pFeature = p + 16;
                        if (_matcher.Accept(_segment, pFeature))
                            if (_filter == null || _filter.Accept(_store.GetFeature(_segment, pFeature)))
                                results.Add(pFeature | (int)(((uint)flags >> 3) & 3));
                    }
                }
            }

            if ((flags & 1) != 0)
                break;

            p += 32;
        }
    }

    /// <summary>
    /// Scans a leaf node of node-feature entries, testing each node's coordinates against the query box
    /// and then the matcher and optional filter, and adding the survivors to the results.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.Nodes.searchLeaf(int)</c>.</remarks>
    void SearchLeafNodes(QueryResults results, int p, CancellationToken ct)
    {
        var buf = new NioBuffer(_segment.Memory);
        for (; ; )
        {
            ct.ThrowIfCancellationRequested();

            var flags = buf.GetInt(p + 8);
            var x = buf.GetInt(p);
            var y = buf.GetInt(p + 4);
            if (!(x > _maxX || y > _maxY || x < _minX || y < _minY))
            {
                var pFeature = p + 8;
                if (_matcher.Accept(_segment, pFeature))
                {
                    if (_filter == null || _filter.Accept(new StoredNode(_store, _segment, pFeature)))
                        results.Add(pFeature);
                }
            }
            if ((flags & 1) != 0)
                break;

            p += 20 + (flags & 4);
        }
    }

}
