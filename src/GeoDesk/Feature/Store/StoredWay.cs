/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using GeoDesk.Common.Pbf;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Store;

internal class StoredWay : StoredFeature, IWay
{

    public StoredWay(FeatureStore store, NioBuffer buf, int ptr) :
        base(store, buf, ptr)
    {

    }

    public override FeatureType Type => FeatureType.Way;

    public bool IsWay => true;

    public override string ToString()
    {
        return "way/" + Id;
    }

    // Iterates the way's coordinates as packed long x/y values.
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator</c>.</remarks>
    public class XYIterator : PbfDecoder
    {

        int _x;
        int _y;
        internal int remaining;
        readonly int _firstX;
        readonly int _firstY;
        int _duplicatedLastCoord;
        readonly int _flags;

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator(ByteBuffer, int, int, int, int)</c>.</remarks>
        public XYIterator(NioBuffer buf, int pos, int prevX, int prevY, int flags) :
            base(buf, pos)
        {
            _x = prevX;
            _y = prevY;
            _flags = flags;
            remaining = (int)ReadVarint();
            if ((flags & FeatureFlags.AREA_FLAG) != 0)
            {
                remaining++;
                _duplicatedLastCoord = 0;
            }
            else
            {
                _duplicatedLastCoord = -1;
            }
            ReadNext();
            _firstX = _x;
            _firstY = _y;
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator.readNext()</c>.</remarks>
        void ReadNext()
        {
            remaining--;
            if (remaining == _duplicatedLastCoord)
            {
                _x = _firstX;
                _y = _firstY;
                _duplicatedLastCoord--;
                return;
            }
            _x += (int)ReadSignedVarint();
            _y += (int)ReadSignedVarint();
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator.hasNext()</c>.</remarks>
        public bool HasNext()
        {
            return remaining >= 0;
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator.nextXY()</c>.</remarks>
        public long NextXY()
        {
            var c = XY.Of(_x, _y);
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
        if (IsArea)
            return factory.CreatePolygon(coords);
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

    public double Length
    {
        get
        {
            if (IsArea)
                return 0;
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
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.nodes()</c>.</remarks>
    public IFeatureQuery Nodes()
    {
        return new WayNodeView(store, buf, ptr);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.nodes(String)</c>.</remarks>
    public IFeatureQuery Nodes(string query)
    {
        if ((buf.Get(ptr) & FeatureFlags.WAYNODE_FLAG) == 0)
            return EmptyView.Any;
        Matcher matcher = store.GetMatcher(query);
        if ((matcher.AcceptedTypes & TypeBits.NODES) == 0)
            return EmptyView.Any;
        return new WayNodeView(store, buf, ptr, TypeBits.NODES, matcher, null);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        int flags = buf.GetInt(ptr);
        if ((flags & FeatureFlags.WAYNODE_FLAG) == 0)
            return Enumerable.Empty<IFeature>().GetEnumerator();
        int ppBody = ptr + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        return new Iter(store, buf, pBody - 4 -
            (flags & FeatureFlags.RELATION_MEMBER_FLAG), Matcher.ALL);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.fastFeatureNodeIterator(Matcher)</c>.</remarks>
    internal FeatureIterator FastFeatureNodeIterator(Matcher matcher)
    {
        int flags = buf.GetInt(ptr);
        System.Diagnostics.Debug.Assert((flags & FeatureFlags.WAYNODE_FLAG) != 0);
        int ppBody = ptr + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        return new Iter(store, buf, pBody - 4 -
            (flags & FeatureFlags.RELATION_MEMBER_FLAG), matcher);
    }

    // TODO: matcher vs filter!
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter</c>.</remarks>
    public class Iter : FeatureIterator
    {

        // TODO: consolidate these flags
        const int NfLast = 1;
        const int NfForeign = 2;
        const int NfDifferentTile = 4;
        const int NfWideTex = 8;

        readonly FeatureStore _store;
        readonly NioBuffer _buf;
        readonly Matcher _filter;
        int _pNext;
        IFeature? _featureNode;
        int _tip = FeatureConstants.START_TIP;
        int _tex = FeatureConstants.WAYNODES_START_TEX;
        NioBuffer _foreignBuf;
        int _pExports;

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter(FeatureStore, ByteBuffer, int, Matcher)</c>.</remarks>
        public Iter(FeatureStore store, NioBuffer buf, int pFirst, Matcher filter)
        {
            _store = store;
            _buf = buf;
            _pNext = pFirst;
            _filter = filter;
            FetchNext();
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter.fetchNext()</c>.</remarks>
        void FetchNext()
        {
            while (_pNext != 0)
            {
                NioBuffer nodeBuf;
                int pNode;
                var node = _buf.GetInt(_pNext);
                if ((node & (NfForeign << 16)) != 0)
                {
                    if ((node & (NfWideTex << 16)) == 0)
                    {
                        node >>= 16;    // signed
                        _pNext += 2;
                    }
                    else
                    {
                        node = (int)BitOperations.RotateLeft((uint)node, 16);
                    }

                    _tex += (node >> 4);

                    if ((node & NfDifferentTile) != 0)
                    {
                        // TODO: test wide tip delta
                        _pNext -= 2;
                        int tipDelta = _buf.GetShort(_pNext);
                        if ((tipDelta & 1) != 0)
                        {
                            // wide TIP delta
                            _pNext -= 2;
                            tipDelta = (_buf.GetShort(_pNext) << 15) | ((tipDelta >> 1) & 0x7fff);
                        }
                        else
                        {
                            tipDelta >>= 1;     // signed
                        }
                        _tip += tipDelta;

                        var entry = _store.TileIndexEntry(_tip);
                        if (!FeatureStore.IsTileLoadedAndCurrent(entry))
                            throw new MissingTileException(_tip);

                        var tilePage = FeatureStore.PageFromEntry(entry);
                        _foreignBuf = new NioBuffer(_store.SegmentOfPage(tilePage).Memory);

                        var ppExports = _store.OffsetOfPage(tilePage) + 24;
                        _pExports = ppExports + _foreignBuf.GetInt(ppExports);
                    }

                    nodeBuf = _foreignBuf;
                    var ppExported = _pExports + (_tex << 2);
                    pNode = ppExported + _foreignBuf.GetInt(ppExported);
                }
                else
                {
                    node = (int)BitOperations.RotateLeft((uint)node, 16);
                    nodeBuf = _buf;
                    pNode = _pNext + (node >> 1) + 2;
                }

                _pNext -= 4;
                _pNext &= -1 + (node & NfLast);     // set _pNext to 0 if this is the last node
                if (_filter.Accept(nodeBuf, pNode))
                {
                    _featureNode = new StoredNode(_store, nodeBuf, pNode);
                    return;
                }
            }

            _featureNode = null;
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter.hasNext()</c>.</remarks>
        public override bool HasNext()
        {
            return _featureNode != null;
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter.next()</c>.</remarks>
        public override IFeature? Next()
        {
            var next = _featureNode;
            FetchNext();
            return next;
        }

    }

}
