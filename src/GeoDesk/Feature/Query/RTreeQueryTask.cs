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

// TODO: make Nodes the base class, Ways/Relations the specialization?

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask</c>.</remarks>
internal class RTreeQueryTask : QueryTask
{

    protected readonly NioBuffer buf;
    protected readonly int ppTree;
    protected readonly int bboxFlags;
    protected readonly Matcher matcher;
    protected readonly IFilter? filter;
    internal readonly RTreeQueryTask? next;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask(TileQueryTask, int, Matcher, RTreeQueryTask)</c>.</remarks>
    public RTreeQueryTask(TileQueryTask parent, int ppTree, Matcher matcher, RTreeQueryTask? next)
        : base(parent.query)
    {
        buf = parent.buf!;
        this.ppTree = ppTree;
        bboxFlags = parent.bboxFlags;
        this.matcher = matcher;
        filter = parent.filter;
        this.next = next;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.exec()</c>.</remarks>
    protected override bool Exec()
    {
        try
        {
            results = new QueryResults(buf);
            var ptr = buf.GetInt(ppTree);
            SearchTrunk(ppTree + (int)((uint)ptr & 0xffff_fffc));
        }
        catch (Exception ex)
        {
            query.SetError(ex);
        }
        return true;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchTrunk(int)</c>.</remarks>
    void SearchTrunk(int p)
    {
        var minX = query.MinX;
        var minY = query.MinY;
        var maxX = query.MaxX;
        var maxY = query.MaxY;
        for (; ; )
        {
            var ptr = buf.GetInt(p);
            var last = ptr & 1;

            if (!(buf.GetInt(p + 4) > maxX ||
                buf.GetInt(p + 8) > maxY ||
                buf.GetInt(p + 12) < minX ||
                buf.GetInt(p + 16) < minY))
            {
                if ((ptr & 2) != 0)
                    SearchLeaf(p + (ptr ^ 2 ^ last));
                else
                    SearchTrunk(p + (ptr ^ last));
            }
            if (last != 0) break;
            p += 20;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchLeaf(int)</c>.</remarks>
    protected virtual void SearchLeaf(int p)
    {
        var minX = query.MinX;
        var minY = query.MinY;
        var maxX = query.MaxX;
        var maxY = query.MaxY;
        var acceptedTypes = query.Types();

        for (; ; )
        {
            var flags = buf.GetInt(p + 16);
            if ((flags & bboxFlags) == 0)
            {
                if (!(buf.GetInt(p) > maxX ||
                    buf.GetInt(p + 4) > maxY ||
                    buf.GetInt(p + 8) < minX ||
                    buf.GetInt(p + 12) < minY))
                {
                    // Check for acceptable type (way, relation, member, way-node, etc.)
                    // (No need for AND with 0x1f, as int-shift only considers lower 5 bits)
                    if (((1 << (flags >> 1)) & acceptedTypes) != 0)
                    {
                        var pFeature = p + 16;
                        if (matcher.Accept(buf, pFeature))
                        {
                            // TODO: We should return results as Features rather than pointers, since
                            //  we are creating a Feature anyway in order to apply a filter
                            if (filter == null || filter.Accept(query.Store().GetFeature(buf, pFeature)))
                                results!.Add(pFeature | (int)(((uint)flags >> 3) & 3));
                        }
                    }
                }
            }
            if ((flags & 1) != 0) break;
            p += 32;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.Nodes</c>.</remarks>
    public class Nodes : RTreeQueryTask
    {

        /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.Nodes(TileQueryTask, int, Matcher, RTreeQueryTask)</c>.</remarks>
        public Nodes(TileQueryTask parent, int ppTree, Matcher matcher, RTreeQueryTask? next)
            : base(parent, ppTree, matcher, next)
        {
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.Nodes.searchLeaf(int)</c>.</remarks>
        protected override void SearchLeaf(int p)
        {
            var minX = query.MinX;
            var minY = query.MinY;
            var maxX = query.MaxX;
            var maxY = query.MaxY;
            for (; ; )
            {
                var flags = buf.GetInt(p + 8);

                // TODO: Should do type check for nodes as well to e.g. to recognize WAYNODE_FLAG

                var x = buf.GetInt(p);
                var y = buf.GetInt(p + 4);
                if (!(x > maxX || y > maxY || x < minX || y < minY))
                {
                    var pFeature = p + 8;
                    if (matcher.Accept(buf, pFeature))
                    {
                        // TODO: We should return results as Features rather than pointers, since we
                        //  are creating a Feature anyway in order to apply a filter
                        if (filter == null || filter.Accept(new StoredNode(query.Store(), buf, pFeature)))
                            results!.Add(pFeature);
                    }
                }
                if ((flags & 1) != 0) break;
                p += 20 + (flags & 4);
                    // If Node is member of relation (flag bit 2), add extra 4 bytes for the
                    // relation table pointer
            }
        }

    }

}
