/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections;
using System.Collections.Generic;

using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.View</c>.</remarks>
public abstract class View : IFeatures
{

    internal readonly FeatureStore store;
    internal readonly int types;
    internal readonly Matcher matcher;
    internal readonly IFilter? filter;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View(FeatureStore, int, Matcher, Filter)</c>.</remarks>
    internal View(FeatureStore store, int types, Matcher matcher, IFilter? filter)
    {
        this.store = store;
        this.types = types;
        this.matcher = matcher;
        this.filter = filter;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.newWith(int, Matcher, Filter)</c>.</remarks>
    internal abstract IFeatures NewWith(int types, Matcher matcher, IFilter? filter);

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.types()</c>.</remarks>
    public int TypesValue()
    {
        return types;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(int)</c>.</remarks>
    protected IFeatures Select(int newTypes)
    {
        newTypes &= types;
        if (newTypes == 0)
            return EmptyView.Any;

        return NewWith(newTypes, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(int, String)</c>.</remarks>
    protected virtual IFeatures Select(int newTypes, string query)
    {
        var newMatcher = store.GetMatcher(query);
        if (matcher != Matcher.ALL)
            newMatcher = new AndMatcher(matcher, newMatcher);

        newTypes &= types & newMatcher.AcceptedTypes();
        if (newTypes == 0)
            return EmptyView.Any;

        return NewWith(newTypes, newMatcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(String)</c>.</remarks>
    public virtual IFeatures Select(string query)
    {
        return Select(TypeBits.ALL, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodes()</c>.</remarks>
    public IFeatures Nodes()
    {
        return Select(TypeBits.NODES);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodes(String)</c>.</remarks>
    public IFeatures Nodes(string query)
    {
        return Select(TypeBits.NODES, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.ways()</c>.</remarks>
    public IFeatures Ways()
    {
        return Select(TypeBits.WAYS);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.ways(String)</c>.</remarks>
    public IFeatures Ways(string query)
    {
        return Select(TypeBits.WAYS, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.relations()</c>.</remarks>
    public IFeatures Relations()
    {
        return Select(TypeBits.RELATIONS);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.relations(String)</c>.</remarks>
    public IFeatures Relations(string query)
    {
        return Select(TypeBits.RELATIONS, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(Filter)</c>.</remarks>
    public virtual IFeatures Select(IFilter filter)
    {
        var newTypes = types;
        if (this.filter != null)
        {
            filter = AndFilter.Create(this.filter, filter);
            if (filter == FalseFilter.Instance)
                return EmptyView.Any;
        }

        var strategy = filter.Strategy();
        if ((strategy & FilterStrategy.RestrictsTypes) != 0)
        {
            newTypes &= filter.AcceptedTypes();
            if (newTypes == 0)
                return EmptyView.Any;
        }

        return NewWith(newTypes, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.parentsOf(Feature)</c>.</remarks>
    public virtual IFeatures ParentsOf(IFeature child)
    {
        if (child.IsNode())
        {
            // An anonymous node *always* has at least one parent way, and *never* has parent relations
            if (child is AnonymousWayNode wayNode)
                return wayNode.Parents(types, matcher, filter);

            return ((StoredNode)child).Parents(types, matcher, filter);
        }
        else
        {
            // Ways and relations can only have relations as parents
            if ((types & TypeBits.RELATIONS) == 0)
                return EmptyView.Any;

            var f = (StoredFeature)child;
            if (!f.BelongsToRelation())
                return EmptyView.Any;

            return new ParentRelationView(store, f.Buffer(), f.GetRelationTablePtr(), types, matcher, filter);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.membersOf(Feature)</c>.</remarks>
    public virtual IFeatures MembersOf(IFeature parent)
    {
        if (parent.IsRelation())
            return ((StoredRelation)parent).Members(types, matcher, filter);

        // TODO: membersOf() for ways
        return EmptyView.Any;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodesOf(Feature)</c>.</remarks>
    public virtual IFeatures NodesOf(IFeature parent)
    {
        if ((types & TypeBits.NODES) == 0)
            return EmptyView.Any;

        if (parent.IsWay())
        {
            var way = (StoredWay)parent;
            if (matcher != Matcher.ALL && (way.Flags() & IFeatureFlags.WAYNODE_FLAG) == 0)
            {
                // GOQL queries only return feature nodes; if the Way's waynode_flag is cleared, it
                // only contains anonymous nodes, so we return an empty set
                return EmptyView.Any;
            }

            return new WayNodeView(store, way.Buffer(), way.Pointer(), types, matcher, filter);
        }

        // TODO: nodesOf() for relations

        return EmptyView.Any;
    }

    // PORT: Java's View does not declare in(); it is abstract here so View can satisfy the
    // Features.in(Bounds) contract, with each concrete view providing the body.
    /// <remarks>Implements Java <c>com.geodesk.feature.Features.in(Bounds)</c> (abstract in this base).</remarks>
    public abstract IFeatures In(Bounds bbox);

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(Features)</c>.</remarks>
    public IFeatures Select(IFeatures otherFeatures)
    {
        // TODO: This assumes both views are WorldViews (which is wrong); at least one must be a WorldView
        // TODO: Throw exception if the Views have different stores

        var other = (View)otherFeatures;

        // TODO: For now, all Features are implemented as a View, but this may change in the future
        var newTypes = types & other.types;
        if (newTypes == 0)
            return EmptyView.Any;

        var newMatcher = other.matcher;
        if (matcher != Matcher.ALL)
        {
            if (newMatcher != Matcher.ALL)
                newMatcher = new AndMatcher(matcher, newMatcher);
            else
                newMatcher = matcher;
        }

        var newFilter = other.filter;
        if (filter != null)
        {
            if (newFilter != null)
                newFilter = AndFilter.Create(filter, newFilter);
            else
                newFilter = filter;
        }

        return NewWith(newTypes, newMatcher, newFilter);
    }

    // --- IFeatures convenience operations, exposed as concrete members so they are callable on the
    // view type directly (C# default interface methods are reachable only through the interface).
    // Both these and the IFeatures defaults delegate to FeaturesSupport, so the logic lives once.

    public IFeature? First() => FeaturesSupport.First(this);
    public long Count() => FeaturesSupport.Count(this);
    public bool IsEmpty() => FeaturesSupport.IsEmpty(this);
    public List<IFeature> ToList() => FeaturesSupport.ToList(this);
    public IFeature[] ToArray() => FeaturesSupport.ToArray(this);
    public void AddTo(ICollection<IFeature> collection) => FeaturesSupport.AddTo(this, collection);

    public INode? GetNode(long id) => FeaturesSupport.GetNode(this, id);
    public IWay? GetWay(long id) => FeaturesSupport.GetWay(this, id);
    public IRelation? GetRelation(long id) => FeaturesSupport.GetRelation(this, id);

    public IFeatures ConnectedTo(IFeature f) => FeaturesSupport.ConnectedTo(this, f);
    public IFeatures ConnectedTo(Geometry geom) => FeaturesSupport.ConnectedTo(this, geom);
    public IFeatures ContainingXY(int x, int y) => FeaturesSupport.ContainingXY(this, x, y);
    public IFeatures ContainingLonLat(double lon, double lat) => FeaturesSupport.ContainingLonLat(this, lon, lat);
    public IFeatures Containing(IFeature feature) => FeaturesSupport.Containing(this, feature);
    public IFeatures Containing(Geometry geom) => FeaturesSupport.Containing(this, geom);
    public IFeatures Containing(IPreparedGeometry prepared) => FeaturesSupport.Containing(this, prepared);
    public IFeatures CoveredBy(IFeature feature) => FeaturesSupport.CoveredBy(this, feature);
    public IFeatures CoveredBy(Geometry geom) => FeaturesSupport.CoveredBy(this, geom);
    public IFeatures CoveredBy(IPreparedGeometry prepared) => FeaturesSupport.CoveredBy(this, prepared);
    public IFeatures Crossing(IFeature feature) => FeaturesSupport.Crossing(this, feature);
    public IFeatures Crossing(Geometry geom) => FeaturesSupport.Crossing(this, geom);
    public IFeatures Crossing(IPreparedGeometry prepared) => FeaturesSupport.Crossing(this, prepared);
    public IFeatures Disjoint(IFeature feature) => FeaturesSupport.Disjoint(this, feature);
    public IFeatures Disjoint(Geometry geom) => FeaturesSupport.Disjoint(this, geom);
    public IFeatures Disjoint(IPreparedGeometry prepared) => FeaturesSupport.Disjoint(this, prepared);
    public IFeatures Intersecting(IFeature feature) => FeaturesSupport.Intersecting(this, feature);
    public IFeatures Intersecting(Geometry geom) => FeaturesSupport.Intersecting(this, geom);
    public IFeatures Intersecting(IPreparedGeometry prepared) => FeaturesSupport.Intersecting(this, prepared);
    public IFeatures MaxMetersFromXY(double distance, int x, int y) => FeaturesSupport.MaxMetersFromXY(this, distance, x, y);
    public IFeatures MaxMetersFromLonLat(double distance, double lon, double lat) => FeaturesSupport.MaxMetersFromLonLat(this, distance, lon, lat);
    public IFeatures MaxMetersFrom(double distance, Geometry geom) => FeaturesSupport.MaxMetersFrom(this, distance, geom);
    public IFeatures MaxMetersFrom(double distance, IFeature feature) => FeaturesSupport.MaxMetersFrom(this, distance, feature);
    public IFeatures Overlapping(IFeature feature) => FeaturesSupport.Overlapping(this, feature);
    public IFeatures Overlapping(Geometry geom) => FeaturesSupport.Overlapping(this, geom);
    public IFeatures Overlapping(IPreparedGeometry prepared) => FeaturesSupport.Overlapping(this, prepared);
    public IFeatures Touching(IFeature feature) => FeaturesSupport.Touching(this, feature);
    public IFeatures Touching(Geometry geom) => FeaturesSupport.Touching(this, geom);
    public IFeatures Touching(IPreparedGeometry prepared) => FeaturesSupport.Touching(this, prepared);
    public IFeatures Within(IFeature feature) => FeaturesSupport.Within(this, feature);
    public IFeatures Within(Geometry geom) => FeaturesSupport.Within(this, geom);
    public IFeatures Within(IPreparedGeometry prepared) => FeaturesSupport.Within(this, prepared);

    /// <remarks>Implements Java <c>java.lang.Iterable.iterator()</c> (abstract in this base).</remarks>
    public abstract IEnumerator<IFeature> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
