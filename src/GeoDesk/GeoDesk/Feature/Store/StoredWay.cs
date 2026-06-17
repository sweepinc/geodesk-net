/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Clarisma.Common.Pbf;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Store;

public class StoredWay : StoredFeature, Way
{
    public StoredWay(FeatureStore store, NioBuffer buf, int ptr)
        : base(store, buf, ptr)
    {
    }

    public override FeatureType Type() => FeatureType.Way;

    public bool IsWay() => true;

    public override string ToString()
    {
        return "way/" + Id();
    }

    // Iterates the way's coordinates as packed long x/y values.
    public class XYIterator : PbfDecoder
    {
        private int x;
        private int y;
        internal int remaining;
        private readonly int firstX;
        private readonly int firstY;
        private int duplicatedLastCoord;
        private readonly int flags;

        public XYIterator(NioBuffer buf, int pos, int prevX, int prevY, int flags)
            : base(buf, pos)
        {
            x = prevX;
            y = prevY;
            this.flags = flags;
            remaining = (int)ReadVarint();
            if ((flags & IFeatureFlags.AREA_FLAG) != 0)
            {
                remaining++;
                duplicatedLastCoord = 0;
            }
            else
            {
                duplicatedLastCoord = -1;
            }
            ReadNext();
            firstX = x;
            firstY = y;
        }

        private void ReadNext()
        {
            remaining--;
            if (remaining == duplicatedLastCoord)
            {
                x = firstX;
                y = firstY;
                duplicatedLastCoord--;
                return;
            }
            x += (int)ReadSignedVarint();
            y += (int)ReadSignedVarint();
        }

        public bool HasNext()
        {
            return remaining >= 0;
        }

        public long NextXY()
        {
            long c = XY.Of(x, y);
            ReadNext();
            return c;
        }
    }

    public override int[] ToXY()
    {
        int flags = buf.GetInt(ptr);
        XYIterator iter = IterXY(flags);
        int[] coords = new int[(iter.remaining + 1) * 2];
        for (int i = 0; i < coords.Length; i += 2)
        {
            long xy = iter.NextXY();
            coords[i] = XY.X(xy);
            coords[i + 1] = XY.Y(xy);
        }
        return coords;
    }

    public override Geometry ToGeometry()
    {
        GeometryFactory factory = store.GeometryFactory();
        WayCoordinateSequence coords = new WayCoordinateSequence(ToXY());
        if (IsArea()) return factory.CreatePolygon(coords);
        return factory.CreateLineString(coords);
    }

    /// <summary>
    /// Returns an iterator over this Way's coordinates. If AREA_FLAG is set, the starting
    /// coordinate is returned again as the last coordinate.
    /// </summary>
    public XYIterator IterXY(int flags)
    {
        int ppBody = ptr + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        int minX = buf.GetInt(ptr - 16);
        int minY = buf.GetInt(ptr - 12);
        return new XYIterator(buf, pBody, minX, minY, flags);
    }

    public XYIterator IterXY()
    {
        return IterXY(buf.GetInt(ptr));
    }

    public double Length()
    {
        if (IsArea()) return 0;
        XYIterator iter = IterXY(0);
        double total = 0;
        long xy = iter.NextXY();
        int prevX = XY.X(xy);
        int prevY = XY.Y(xy);
        while (iter.HasNext())
        {
            xy = iter.NextXY();
            int x = XY.X(xy);
            int y = XY.Y(xy);
            total += Mercator.Distance(prevX, prevY, x, y);
            prevX = x;
            prevY = y;
        }
        return total;
    }

    public Features Nodes()
    {
        return new WayNodeView(store, buf, ptr);
    }

    public Features Nodes(string query)
    {
        if ((buf.Get(ptr) & IFeatureFlags.WAYNODE_FLAG) == 0) return EmptyView.ANY;
        Matcher matcher = store.GetMatcher(query);
        if ((matcher.AcceptedTypes() & TypeBits.NODES) == 0) return EmptyView.ANY;
        return new WayNodeView(store, buf, ptr, TypeBits.NODES, matcher, null);
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        int flags = buf.GetInt(ptr);
        if ((flags & IFeatureFlags.WAYNODE_FLAG) == 0) return Enumerable.Empty<Feature>().GetEnumerator();
        int ppBody = ptr + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        return new Iter(store, buf, pBody - 4 -
            (flags & IFeatureFlags.RELATION_MEMBER_FLAG), Matcher.ALL);
    }

    internal FeatureIterator FastFeatureNodeIterator(Matcher matcher)
    {
        int flags = buf.GetInt(ptr);
        System.Diagnostics.Debug.Assert((flags & IFeatureFlags.WAYNODE_FLAG) != 0);
        int ppBody = ptr + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        return new Iter(store, buf, pBody - 4 -
            (flags & IFeatureFlags.RELATION_MEMBER_FLAG), matcher);
    }

    // TODO: matcher vs filter!
    public class Iter : FeatureIterator
    {
        private readonly FeatureStore store;
        private readonly NioBuffer buf;
        private readonly Matcher filter;
        private int pNext;
        private Feature? featureNode;
        private int tip = FeatureConstants.START_TIP;
        private int tex = FeatureConstants.WAYNODES_START_TEX;
        private NioBuffer? foreignBuf;
        private int pExports;

        // TODO: consolidate these flags
        private const int NF_LAST = 1;
        private const int NF_FOREIGN = 2;
        private const int NF_DIFFERENT_TILE = 4;
        private const int NF_WIDE_TEX = 8;

        public Iter(FeatureStore store, NioBuffer buf, int pFirst, Matcher filter)
        {
            this.store = store;
            this.buf = buf;
            this.pNext = pFirst;
            this.filter = filter;
            FetchNext();
        }

        private void FetchNext()
        {
            while (pNext != 0)
            {
                NioBuffer nodeBuf;
                int pNode;
                int node = buf.GetInt(pNext);
                if ((node & (NF_FOREIGN << 16)) != 0)
                {
                    if ((node & (NF_WIDE_TEX << 16)) == 0)
                    {
                        node >>= 16;    // signed
                        pNext += 2;
                    }
                    else
                    {
                        node = (int)BitOperations.RotateLeft((uint)node, 16);
                    }
                    tex += (node >> 4);
                    if ((node & NF_DIFFERENT_TILE) != 0)
                    {
                        // TODO: test wide tip delta
                        pNext -= 2;
                        int tipDelta = buf.GetShort(pNext);
                        if ((tipDelta & 1) != 0)
                        {
                            // wide TIP delta
                            pNext -= 2;
                            tipDelta = (buf.GetShort(pNext) << 15) |
                                ((tipDelta >> 1) & 0x7fff);
                        }
                        else
                        {
                            tipDelta >>= 1;     // signed
                        }
                        tip += tipDelta;
                        int entry = store.TileIndexEntry(tip);
                        if (!FeatureStore.IsTileLoadedAndCurrent(entry))
                        {
                            throw new MissingTileException(tip);
                        }
                        int tilePage = FeatureStore.PageFromEntry(entry);
                        foreignBuf = store.BufferOfPage(tilePage);
                        int ppExports = store.OffsetOfPage(tilePage) + 24;
                        pExports = ppExports + foreignBuf.GetInt(ppExports);
                    }
                    nodeBuf = foreignBuf!;
                    int ppExported = pExports + (tex << 2);
                    pNode = ppExported + foreignBuf!.GetInt(ppExported);
                }
                else
                {
                    node = (int)BitOperations.RotateLeft((uint)node, 16);
                    nodeBuf = buf;
                    pNode = pNext + (node >> 1) + 2;
                }
                pNext -= 4;
                pNext &= -1 + (node & NF_LAST);     // set pNext to 0 if this is the last node
                if (filter.Accept(nodeBuf, pNode))
                {
                    featureNode = new StoredNode(store, nodeBuf, pNode);
                    return;
                }
            }
            featureNode = null;
        }

        public override bool HasNext()
        {
            return featureNode != null;
        }

        public override Feature? Next()
        {
            Feature? next = featureNode;
            FetchNext();
            return next;
        }
    }
}
