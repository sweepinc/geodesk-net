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
using GeoDesk.Common.Store;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A way feature read directly from a feature library tile. Decodes its coordinate
/// sequence (delta-encoded in the body) on demand and exposes its nodes, geometry,
/// and length.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay</c>.</remarks>
internal class StoredWay : StoredFeature, IWay
{

    /// <summary>
    /// Creates a stored way backed by the given store, buffer, and pointer to the
    /// way's record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public StoredWay(FeatureStore store, Segment segment, int pFeature) :
        base(store, segment, pFeature)
    {

    }

    /// <summary>
    /// The feature type, always <see cref="FeatureType.Way"/>.
    /// </summary>
    public override FeatureType Type => FeatureType.Way;

    /// <summary>
    /// Always true; this feature is a way.
    /// </summary>
    public bool IsWay => true;

    /// <summary>
    /// Returns a debug string of the form <c>way/{id}</c>.
    /// </summary>
    public override string ToString()
    {
        return "way/" + Id;
    }

    // Iterates the way's coordinates as packed long x/y values.
    /// <summary>
    /// Iterates a way's coordinates as packed X/Y longs, decoding the delta-encoded
    /// body. For area ways the first coordinate is repeated as the closing point.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator</c>.</remarks>
    public class XYIterator
    {

        PbfDecoder _dec;
        int _x;
        int _y;
        internal int remaining;
        readonly int _firstX;
        readonly int _firstY;
        int _duplicatedLastCoord;
        readonly int _flags;

        /// <summary>
        /// Creates the coordinate iterator over the way body at the given position,
        /// seeded with the preceding X/Y and flags, and reads the first coordinate.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator(ByteBuffer, int, int, int, int)</c>.</remarks>
        public XYIterator(NioBuffer buf, int pos, int prevX, int prevY, int flags)
        {
            _dec = new PbfDecoder(buf.Memory, pos);
            _x = prevX;
            _y = prevY;
            _flags = flags;
            _firstX = 0;
            _firstY = 0;
            remaining = (int)_dec.ReadVarint();
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

        /// <summary>
        /// Advances to the next coordinate, decoding the next signed delta pair or
        /// re-emitting the first coordinate to close an area ring.
        /// </summary>
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
            _x += (int)_dec.ReadSignedVarint();
            _y += (int)_dec.ReadSignedVarint();
        }

        /// <summary>
        /// Returns true while more coordinates remain to be produced.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator.hasNext()</c>.</remarks>
        public bool HasNext()
        {
            return remaining >= 0;
        }

        /// <summary>
        /// Returns the current packed X/Y coordinate and advances to the next.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.XYIterator.nextXY()</c>.</remarks>
        public long NextXY()
        {
            var c = XY.Of(_x, _y);
            ReadNext();
            return c;
        }

    }

    /// <summary>
    /// Returns the way's full coordinate sequence as a flat X/Y array, decoded from the
    /// delta-encoded body.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.toXY()</c>.</remarks>
    public override int[] ToXY()
    {
        int flags = buf.GetInt(pFeature);
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

    /// <summary>
    /// Builds the way's geometry: a polygon when it is an area, otherwise a line string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.toGeometry()</c>.</remarks>
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
        int ppBody = pFeature + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        int minX = buf.GetInt(pFeature - 16);
        int minY = buf.GetInt(pFeature - 12);
        return new XYIterator(buf, pBody, minX, minY, flags);
    }

    /// <summary>
    /// Returns an iterator over this way's coordinates using the way's own flags.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.iterXY()</c>.</remarks>
    public XYIterator IterXY()
    {
        return IterXY(buf.GetInt(pFeature));
    }

    /// <summary>
    /// The length of the way in meters (0 for areas), summed over its segments and
    /// corrected for Mercator distortion.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.length()</c>.</remarks>
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

    /// <summary>
    /// Returns a query over all of this way's nodes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.nodes()</c>.</remarks>
    public IFeatureQuery Nodes()
    {
        return new WayNodeView(store, segment, pFeature);
    }

    /// <summary>
    /// Returns a query over this way's nodes that match the given GOQL query string;
    /// empty when the way has no feature-nodes or the query excludes nodes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.nodes(String)</c>.</remarks>
    public IFeatureQuery Nodes(string query)
    {
        if ((buf.Get(pFeature) & FeatureFlags.WAYNODE_FLAG) == 0)
            return EmptyView.Any;
        Matcher matcher = store.GetMatcher(query);
        if ((matcher.AcceptedTypes & TypeBits.NODES) == 0)
            return EmptyView.Any;
        return new WayNodeView(store, segment, pFeature, TypeBits.NODES, matcher, null);
    }

    /// <summary>
    /// Returns an iterator over this way's feature-nodes; empty when the way has none.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        int flags = buf.GetInt(pFeature);
        if ((flags & FeatureFlags.WAYNODE_FLAG) == 0)
            return Enumerable.Empty<IFeature>().GetEnumerator();
        int ppBody = pFeature + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        return new Iter(store, segment, pBody - 4 -
            (flags & FeatureFlags.RELATION_MEMBER_FLAG), Matcher.ALL);
    }

    /// <summary>
    /// Returns an iterator over this way's feature-nodes filtered by the given matcher.
    /// The way is assumed to carry feature-nodes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.fastFeatureNodeIterator(Matcher)</c>.</remarks>
    internal FeatureIterator FastFeatureNodeIterator(Matcher matcher)
    {
        int flags = buf.GetInt(pFeature);
        System.Diagnostics.Debug.Assert((flags & FeatureFlags.WAYNODE_FLAG) != 0);
        int ppBody = pFeature + 12;
        int pBody = buf.GetInt(ppBody) + ppBody;
        return new Iter(store, segment, pBody - 4 -
            (flags & FeatureFlags.RELATION_MEMBER_FLAG), matcher);
    }

    // TODO: matcher vs filter!
    /// <summary>
    /// Iterates the feature-nodes of a way, decoding the packed node-reference list to
    /// resolve local and foreign nodes (loading foreign tiles and their exports as
    /// needed) and yielding those accepted by the matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter</c>.</remarks>
    public class Iter : FeatureIterator
    {

        // TODO: consolidate these flags
        const int NfLast = 1;
        const int NfForeign = 2;
        const int NfDifferentTile = 4;
        const int NfWideTex = 8;

        readonly FeatureStore _store;
        readonly Segment _segment;
        readonly Matcher _filter;
        int _pNext;
        IFeature? _featureNode;
        int _tip = FeatureConstants.START_TIP;
        int _tex = FeatureConstants.WAYNODES_START_TEX;
        Segment? _foreignSegment;
        int _pExports;

        /// <summary>
        /// Creates the iterator over the way's node-reference list starting at the given
        /// position and pre-fetches the first matching feature-node.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter(FeatureStore, ByteBuffer, int, Matcher)</c>.</remarks>
        public Iter(FeatureStore store, Segment segment, int pFirst, Matcher filter)
        {
            _store = store;
            _segment = segment;
            _pNext = pFirst;
            _filter = filter;
            FetchNext();
        }

        /// <summary>
        /// Advances to the next feature-node accepted by the matcher, decoding foreign
        /// references and TEX/TIP deltas and resolving the node's buffer location.
        /// Caches the result, or null once the list is exhausted.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter.fetchNext()</c>.</remarks>
        void FetchNext()
        {
            var buf = new NioBuffer(_segment.Memory);
            while (_pNext != 0)
            {
                Segment nodeSegment;
                int pNode;
                var node = buf.GetInt(_pNext);
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
                        int tipDelta = buf.GetShort(_pNext);
                        if ((tipDelta & 1) != 0)
                        {
                            // wide TIP delta
                            _pNext -= 2;
                            tipDelta = (buf.GetShort(_pNext) << 15) | ((tipDelta >> 1) & 0x7fff);
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
                        _foreignSegment = _store.SegmentOfPage(tilePage);

                        var ppExports = _store.OffsetOfPage(tilePage) + 24;
                        _pExports = ppExports + new NioBuffer(_foreignSegment.Memory).GetInt(ppExports);
                    }

                    var foreignBuf = new NioBuffer(_foreignSegment!.Memory);
                    nodeSegment = _foreignSegment!;
                    var ppExported = _pExports + (_tex << 2);
                    pNode = ppExported + foreignBuf.GetInt(ppExported);
                }
                else
                {
                    node = (int)BitOperations.RotateLeft((uint)node, 16);
                    nodeSegment = _segment;
                    pNode = _pNext + (node >> 1) + 2;
                }

                _pNext -= 4;
                _pNext &= -1 + (node & NfLast);     // set _pNext to 0 if this is the last node
                if (_filter.Accept(nodeSegment, pNode))
                {
                    _featureNode = new StoredNode(_store, nodeSegment, pNode);
                    return;
                }
            }

            _featureNode = null;
        }

        /// <summary>
        /// Returns true if a pre-fetched feature-node is available.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter.hasNext()</c>.</remarks>
        public override bool HasNext()
        {
            return _featureNode != null;
        }

        /// <summary>
        /// Returns the current feature-node and pre-fetches the next one.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay.Iter.next()</c>.</remarks>
        public override IFeature? Next()
        {
            var next = _featureNode;
            FetchNext();
            return next;
        }

    }

}
