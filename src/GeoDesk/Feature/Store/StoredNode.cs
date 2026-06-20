/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;

using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Store;

internal class StoredNode : StoredFeature, INode
{
    public StoredNode(FeatureStore store, NioBuffer buf, int ptr)
        : base(store, buf, ptr)
    {
    }

    public override IEnumerator<IFeature> GetEnumerator()
    {
        return Enumerable.Empty<IFeature>().GetEnumerator();
    }

    public override FeatureType Type => FeatureType.Node;

    public bool IsNode => true;

    public override int X => buf.GetInt(ptr - 8);

    public override int Y => buf.GetInt(ptr - 4);

    public override Box Bounds
    {
        get
        {
            int x = X;
            int y = Y;
            if ((x | y) == 0)
            {
                // If coordinates are 0/0, return an empty bbox (missing nodes)
                return new Box();
            }
            return new Box(x, y);
        }
    }

    public override int[] ToXY()
    {
        return new int[] { X, Y };
    }

    public override Geometry ToGeometry()
    {
        return store.GeometryFactory().CreatePoint(new Coordinate(X, Y));
    }

    public override string ToString()
    {
        return "node/" + Id;
    }

    public override int GetRelationTablePtr()
    {
        // A Node's body pointer is the pointer to its reltable
        int ppBody = ptr + 12;
        return buf.GetInt(ppBody) + ppBody;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parentWays(int, Matcher, Filter)</c>.</remarks>
    public WorldView ParentWays(int types, Matcher matcher, IFilter? filter)
    {
        IFilter newFilter = new ParentWayFilter(Id);
        if (filter != null)
            newFilter = AndFilter.Create(newFilter, filter);

        return new WorldView(store, types & TypeBits.WAYS & TypeBits.WAYNODE_FLAGGED, Bounds, matcher, newFilter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parents(int, Matcher, Filter)</c>.</remarks>
    public IFeatureQuery Parents(int types, Matcher matcher, IFilter? filter)
    {
        int acceptedFlags = ((types & TypeBits.RELATIONS) != 0) ? FeatureFlags.RELATION_MEMBER_FLAG : 0;
        acceptedFlags |= ((types & TypeBits.WAYS) != 0) ? FeatureFlags.WAYNODE_FLAG : 0;
        int flags = buf.GetInt(ptr) & acceptedFlags;

        if (flags == FeatureFlags.WAYNODE_FLAG)
            return ParentWays(types, matcher, filter);

        if (flags == FeatureFlags.RELATION_MEMBER_FLAG)
            return new ParentRelationView(store, buf, GetRelationTablePtr(), types & TypeBits.RELATIONS, matcher, filter);

        if (flags == (FeatureFlags.WAYNODE_FLAG | FeatureFlags.RELATION_MEMBER_FLAG))
            return new NodeParentView(store, buf, this, GetRelationTablePtr(), types, matcher, filter);

        return EmptyView.Any;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parents()</c>.</remarks>
    public override IFeatureQuery Parents()
    {
        return Parents(TypeBits.RELATIONS | TypeBits.WAYS, Matcher.ALL, null);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parents(String)</c>.</remarks>
    public override IFeatureQuery Parents(string query)
    {
        var matcher = store.GetMatcher(query);
        return Parents(matcher.AcceptedTypes, matcher, null);
    }

    // TODO: No need to dereference the nodes in a way; we could simply check for
    //  same buffer and pointer (Nodes always live in one tile only)
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.ParentWayFilter</c>.</remarks>
    class ParentWayFilter : IdMatcher, IFilter
    {

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.ParentWayFilter(long)</c>.</remarks>
        public ParentWayFilter(long nodeId) :
            base(0, nodeId)
        {

        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.ParentWayFilter.accept(Feature)</c>.</remarks>
        public bool Accept(IFeature feature)
        {
            StoredWay way = (StoredWay)feature;
            return way.FastFeatureNodeIterator(this).HasNext();
        }

    }

}
