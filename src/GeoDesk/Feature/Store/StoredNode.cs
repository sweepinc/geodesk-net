/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;

using GeoDesk.Common.Store;
using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A node feature read directly from a feature library tile. Exposes the node's
/// coordinate, geometry, and its parent ways and relations by decoding the stored
/// representation on demand.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode</c>.</remarks>
internal class StoredNode : StoredFeature, INode
{
    /// <summary>
    /// Creates a stored node backed by the given store, buffer, and pointer to the
    /// node's record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public StoredNode(FeatureStore store, Segment segment, int pFeature)
        : base(store, segment, pFeature)
    {
    }

    /// <summary>
    /// Returns an empty enumerator; a node has no members.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return Enumerable.Empty<IFeature>().GetEnumerator();
    }

    /// <summary>
    /// The feature type, always <see cref="FeatureType.Node"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.type()</c>.</remarks>
    public override FeatureType Type => FeatureType.Node;

    /// <summary>
    /// Always true; this feature is a node.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.isNode()</c>.</remarks>
    public bool IsNode => true;

    /// <summary>
    /// The node's X coordinate in the library's projection, read from the record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.x()</c>.</remarks>
    public override int X => buf.GetInt(pFeature - 8);

    /// <summary>
    /// The node's Y coordinate in the library's projection, read from the record.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.y()</c>.</remarks>
    public override int Y => buf.GetInt(pFeature - 4);

    /// <summary>
    /// The bounding box of the node: a degenerate box at its coordinate, or an empty
    /// box when the coordinate is 0/0 (used to mark missing nodes).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.bounds()</c>.</remarks>
    public override Box Bounds
    {
        get
        {
            int x = X;
            int y = Y;
            if ((x | y) == 0)
                return new Box(); // If coordinates are 0/0, return an empty bbox (missing nodes)

            return new Box(x, y);
        }
    }

    /// <summary>
    /// Returns the node's coordinate as a two-element X/Y array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.toXY()</c>.</remarks>
    public override int[] ToXY()
    {
        return [X, Y];
    }

    /// <summary>
    /// Returns the node as a JTS/NTS point geometry at its coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.toGeometry()</c>.</remarks>
    public override Geometry ToGeometry()
    {
        return store.GeometryFactory().CreatePoint(new Coordinate(X, Y));
    }

    /// <summary>
    /// Returns a debug string of the form <c>node/{id}</c>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.toString()</c>.</remarks>
    public override string ToString()
    {
        return "node/" + Id;
    }

    /// <summary>
    /// Returns the absolute buffer pointer to the node's relation table. For a node
    /// the body pointer is itself the pointer to the reltable.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.getRelationTablePtr()</c>.</remarks>
    public override int GetRelationTablePtr()
    {
        // A Node's body pointer is the pointer to its reltable
        int ppBody = pFeature + 12;
        return buf.GetInt(ppBody) + ppBody;
    }

    /// <summary>
    /// Returns a world view over the ways that include this node, combining a
    /// parent-way filter (matching by this node's id) with any supplied filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parentWays(int, Matcher, Filter)</c>.</remarks>
    public WorldView ParentWays(int types, Matcher matcher, IFilter? filter)
    {
        IFilter newFilter = new ParentWayFilter(Id);
        if (filter != null)
            newFilter = AndFilter.Create(newFilter, filter);

        return new WorldView(store, types & TypeBits.WAYS & TypeBits.WAYNODE_FLAGGED, Bounds, matcher, newFilter);
    }

    /// <summary>
    /// Returns a query over this node's parents (ways, relations, or both) selected by
    /// the requested types. The node's flags determine which parent kinds exist and
    /// the appropriate view is constructed; an empty view is returned when none apply.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parents(int, Matcher, Filter)</c>.</remarks>
    public IFeatureQuery Parents(int types, Matcher matcher, IFilter? filter)
    {
        int acceptedFlags = ((types & TypeBits.RELATIONS) != 0) ? FeatureFlags.RELATION_MEMBER_FLAG : 0;
        acceptedFlags |= ((types & TypeBits.WAYS) != 0) ? FeatureFlags.WAYNODE_FLAG : 0;
        int flags = buf.GetInt(pFeature) & acceptedFlags;

        if (flags == FeatureFlags.WAYNODE_FLAG)
            return ParentWays(types, matcher, filter);

        if (flags == FeatureFlags.RELATION_MEMBER_FLAG)
            return new ParentRelationView(store, segment, GetRelationTablePtr(), types & TypeBits.RELATIONS, matcher, filter);

        if (flags == (FeatureFlags.WAYNODE_FLAG | FeatureFlags.RELATION_MEMBER_FLAG))
            return new NodeParentView(store, segment, this, GetRelationTablePtr(), types, matcher, filter);

        return EmptyView.Any;
    }

    /// <summary>
    /// Returns a query over all of this node's parent ways and relations.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parents()</c>.</remarks>
    public override IFeatureQuery Parents()
    {
        return Parents(TypeBits.RELATIONS | TypeBits.WAYS, Matcher.ALL, null);
    }

    /// <summary>
    /// Returns a query over the parents of this node that satisfy the given GOQL
    /// query string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.parents(String)</c>.</remarks>
    public override IFeatureQuery Parents(string query)
    {
        var matcher = store.GetMatcher(query);
        return Parents(matcher.AcceptedTypes, matcher, null);
    }

    // TODO: No need to dereference the nodes in a way; we could simply check for
    //  same buffer and pointer (Nodes always live in one tile only)
    /// <summary>
    /// A filter that accepts a way only if it has this node among its feature-nodes,
    /// matching by the node's id rather than by coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.ParentWayFilter</c>.</remarks>
    class ParentWayFilter : IdMatcher, IFilter
    {

        /// <summary>
        /// Creates a filter that matches ways containing the node with the given id.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.ParentWayFilter(long)</c>.</remarks>
        public ParentWayFilter(long nodeId) :
            base(0, nodeId)
        {

        }

        /// <summary>
        /// Returns true if the given way contains the target node among its
        /// feature-nodes.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode.ParentWayFilter.accept(Feature)</c>.</remarks>
        public bool Accept(IFeature feature)
        {
            StoredWay way = (StoredWay)feature;
            return way.FastFeatureNodeIterator(this).HasNext();
        }

    }

}
