/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections;
using System.Collections.Generic;
using System.Threading;

using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Query;

/// <summary>
/// Abstract base for the query views that implement <see cref="IFeatureQuery"/>. A view captures a
/// feature store, an accepted-type bitmask, a tag matcher, and an optional spatial filter, and
/// exposes the fluent refinement operations (by type, by query string, by filter, by relationship)
/// plus the terminal collection operations. Concrete subclasses (world, member, way-node, etc.)
/// supply iteration and the factory <see cref="NewWith"/>.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.View</c>.</remarks>
public abstract class View : IFeatureQuery
{

    internal readonly FeatureStore store;
    internal readonly int types;
    internal readonly Matcher matcher;
    internal readonly IFilter? filter;

    /// <summary>
    /// Initializes the view with its store, accepted feature types, matcher, and optional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View(FeatureStore, int, Matcher, Filter)</c>.</remarks>
    internal View(FeatureStore store, int types, Matcher matcher, IFilter? filter)
    {
        this.store = store;
        this.types = types;
        this.matcher = matcher;
        this.filter = filter;
    }

    /// <summary>
    /// Factory that produces a new view of the same concrete kind with the given types, matcher, and
    /// filter; the basis for all refinement operations.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.newWith(int, Matcher, Filter)</c>.</remarks>
    internal abstract IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter);

    /// <summary>
    /// Returns the bitmask of feature types this view accepts.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.types()</c>.</remarks>
    public int TypesValue()
    {
        return types;
    }

    /// <summary>
    /// Narrows the view to the intersection of its types with <paramref name="newTypes"/>, returning
    /// the empty view if nothing remains.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(int)</c>.</remarks>
    protected IFeatureQuery Select(int newTypes)
    {
        newTypes &= types;
        if (newTypes == 0)
            return EmptyView.Any;

        return NewWith(newTypes, matcher, filter);
    }

    /// <summary>
    /// Narrows the view by both a type mask and a GOQL query string, ANDing the compiled matcher with
    /// the current one and intersecting the accepted types.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(int, String)</c>.</remarks>
    protected virtual IFeatureQuery Select(int newTypes, string query)
    {
        var newMatcher = store.GetMatcher(query);
        if (matcher != Matcher.ALL)
            newMatcher = new AndMatcher(matcher, newMatcher);

        newTypes &= types & newMatcher.AcceptedTypes;
        if (newTypes == 0)
            return EmptyView.Any;

        return NewWith(newTypes, newMatcher, filter);
    }

    /// <summary>
    /// Narrows the view by a GOQL query string across all feature types.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(String)</c>.</remarks>
    public virtual IFeatureQuery Select(string query)
    {
        return Select(TypeBits.ALL, query);
    }

    /// <summary>
    /// Restricts the view to node features.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodes()</c>.</remarks>
    public IFeatureQuery Nodes()
    {
        return Select(TypeBits.NODES);
    }

    /// <summary>
    /// Restricts the view to node features matching the given query.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodes(String)</c>.</remarks>
    public IFeatureQuery Nodes(string query)
    {
        return Select(TypeBits.NODES, query);
    }

    /// <summary>
    /// Restricts the view to way features.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.ways()</c>.</remarks>
    public IFeatureQuery Ways()
    {
        return Select(TypeBits.WAYS);
    }

    /// <summary>
    /// Restricts the view to way features matching the given query.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.ways(String)</c>.</remarks>
    public IFeatureQuery Ways(string query)
    {
        return Select(TypeBits.WAYS, query);
    }

    /// <summary>
    /// Restricts the view to relation features.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.relations()</c>.</remarks>
    public IFeatureQuery Relations()
    {
        return Select(TypeBits.RELATIONS);
    }

    /// <summary>
    /// Restricts the view to relation features matching the given query.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.relations(String)</c>.</remarks>
    public IFeatureQuery Relations(string query)
    {
        return Select(TypeBits.RELATIONS, query);
    }

    /// <summary>
    /// Applies a spatial filter to the view, ANDing it with any existing filter and narrowing the
    /// accepted types when the filter restricts them; returns the empty view if nothing can match.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(Filter)</c>.</remarks>
    public virtual IFeatureQuery Select(IFilter filter)
    {
        var newTypes = types;
        if (this.filter != null)
        {
            filter = AndFilter.Create(this.filter, filter);
            if (filter == FalseFilter.Instance)
                return EmptyView.Any;
        }

        var strategy = filter.Strategy;
        if ((strategy & FilterStrategy.RestrictsTypes) != 0)
        {
            newTypes &= filter.AcceptedTypes;
            if (newTypes == 0)
                return EmptyView.Any;
        }

        return NewWith(newTypes, matcher, filter);
    }

    /// <summary>
    /// Returns a view over the parent features of the given child: parent ways and relations for a
    /// node, or parent relations for a way or relation, filtered by this view's types, matcher, and filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.parentsOf(Feature)</c>.</remarks>
    public virtual IFeatureQuery ParentsOf(IFeature child)
    {
        if (child.IsNode)
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
            if (!f.BelongsToRelation)
                return EmptyView.Any;

            return new ParentRelationView(store, f.Segment, f.GetRelationTablePtr(), types, matcher, filter);
        }
    }

    /// <summary>
    /// Returns a view over the members of the given relation, filtered by this view's types, matcher,
    /// and filter. Other parent types yield an empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.membersOf(Feature)</c>.</remarks>
    public virtual IFeatureQuery MembersOf(IFeature parent)
    {
        if (parent.IsRelation)
            return ((StoredRelation)parent).Members(types, matcher, filter);

        // TODO: membersOf() for ways
        return EmptyView.Any;
    }

    /// <summary>
    /// Returns a view over the feature-nodes of the given way, filtered by this view's types, matcher,
    /// and filter. Ways carrying only anonymous nodes (under a matcher query) and non-way parents yield
    /// an empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodesOf(Feature)</c>.</remarks>
    public virtual IFeatureQuery NodesOf(IFeature parent)
    {
        if ((types & TypeBits.NODES) == 0)
            return EmptyView.Any;

        if (parent.IsWay)
        {
            var way = (StoredWay)parent;
            if (matcher != Matcher.ALL && (way.Flags() & FeatureFlags.WAYNODE_FLAG) == 0)
            {
                // GOQL queries only return feature nodes; if the Way's waynode_flag is cleared, it
                // only contains anonymous nodes, so we return an empty set
                return EmptyView.Any;
            }

            return new WayNodeView(store, way.Segment, way.Pointer(), types, matcher, filter);
        }

        // TODO: nodesOf() for relations

        return EmptyView.Any;
    }

    // PORT: Java's View does not declare in(); it is abstract here so View can satisfy the
    // Features.in(Bounds) contract, with each concrete view providing the body.
    /// <summary>
    /// Restricts the view to features within the given bounding box; implemented by each concrete view.
    /// </summary>
    /// <remarks>Implements Java <c>com.geodesk.feature.Features.in(Bounds)</c> (abstract in this base).</remarks>
    public abstract IFeatureQuery In(IBounds bbox);

    /// <summary>
    /// Intersects this view with another feature query, combining their accepted types, matchers, and
    /// filters; returns the empty view if no type can satisfy both.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(Features)</c>.</remarks>
    public IFeatureQuery Select(IFeatureQuery otherFeatures)
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

    /// <inheritdoc />
    public virtual IFeature? First() => FeaturesSupport.First(this);

    /// <inheritdoc />
    public virtual long Count() => FeaturesSupport.Count(this);

    /// <inheritdoc />
    public virtual bool IsEmpty() => FeaturesSupport.IsEmpty(this);

    /// <inheritdoc />
    public virtual List<IFeature> ToList() => FeaturesSupport.ToList(this);

    /// <inheritdoc />
    public virtual IFeature[] ToArray() => FeaturesSupport.ToArray(this);

    /// <inheritdoc />
    public virtual bool Contains(IFeature f) => FeaturesSupport.Contains(this, f);

    /// <inheritdoc />
    public virtual void AddTo(ICollection<IFeature> collection) => FeaturesSupport.AddTo(this, collection);

    /// <inheritdoc />
    public virtual INode? GetNode(long id) => FeaturesSupport.GetNode(this, id);

    /// <inheritdoc />
    public virtual IWay? GetWay(long id) => FeaturesSupport.GetWay(this, id);

    /// <inheritdoc />
    public virtual IRelation? GetRelation(long id) => FeaturesSupport.GetRelation(this, id);

    /// <inheritdoc />
    public virtual IFeatureQuery ConnectedTo(IFeature f) => FeaturesSupport.ConnectedTo(this, f);

    /// <inheritdoc />
    public virtual IFeatureQuery ConnectedTo(Geometry geom) => FeaturesSupport.ConnectedTo(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery ContainingXY(int x, int y) => FeaturesSupport.ContainingXY(this, x, y);

    /// <inheritdoc />
    public virtual IFeatureQuery ContainingLonLat(double lon, double lat) => FeaturesSupport.ContainingLonLat(this, lon, lat);

    /// <inheritdoc />
    public virtual IFeatureQuery Containing(IFeature feature) => FeaturesSupport.Containing(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Containing(Geometry geom) => FeaturesSupport.Containing(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery Containing(IPreparedGeometry prepared) => FeaturesSupport.Containing(this, prepared);

    /// <inheritdoc />
    public virtual IFeatureQuery CoveredBy(IFeature feature) => FeaturesSupport.CoveredBy(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery CoveredBy(Geometry geom) => FeaturesSupport.CoveredBy(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery CoveredBy(IPreparedGeometry prepared) => FeaturesSupport.CoveredBy(this, prepared);

    /// <inheritdoc />
    public virtual IFeatureQuery Crossing(IFeature feature) => FeaturesSupport.Crossing(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Crossing(Geometry geom) => FeaturesSupport.Crossing(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery Crossing(IPreparedGeometry prepared) => FeaturesSupport.Crossing(this, prepared);

    /// <inheritdoc />
    public virtual IFeatureQuery Disjoint(IFeature feature) => FeaturesSupport.Disjoint(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Disjoint(Geometry geom) => FeaturesSupport.Disjoint(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery Disjoint(IPreparedGeometry prepared) => FeaturesSupport.Disjoint(this, prepared);

    /// <inheritdoc />
    public virtual IFeatureQuery Intersecting(IFeature feature) => FeaturesSupport.Intersecting(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Intersecting(Geometry geom) => FeaturesSupport.Intersecting(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery Intersecting(IPreparedGeometry prepared) => FeaturesSupport.Intersecting(this, prepared);

    /// <inheritdoc />
    public virtual IFeatureQuery MaxMetersFromXY(double distance, int x, int y) => FeaturesSupport.MaxMetersFromXY(this, distance, x, y);

    /// <inheritdoc />
    public virtual IFeatureQuery MaxMetersFromLonLat(double distance, double lon, double lat) => FeaturesSupport.MaxMetersFromLonLat(this, distance, lon, lat);

    /// <inheritdoc />
    public virtual IFeatureQuery MaxMetersFrom(double distance, Geometry geom) => FeaturesSupport.MaxMetersFrom(this, distance, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery MaxMetersFrom(double distance, IFeature feature) => FeaturesSupport.MaxMetersFrom(this, distance, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Overlapping(IFeature feature) => FeaturesSupport.Overlapping(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Overlapping(Geometry geom) => FeaturesSupport.Overlapping(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery Overlapping(IPreparedGeometry prepared) => FeaturesSupport.Overlapping(this, prepared);

    /// <inheritdoc />
    public virtual IFeatureQuery Touching(IFeature feature) => FeaturesSupport.Touching(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Touching(Geometry geom) => FeaturesSupport.Touching(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery Touching(IPreparedGeometry prepared) => FeaturesSupport.Touching(this, prepared);

    /// <inheritdoc />
    public virtual IFeatureQuery Within(IFeature feature) => FeaturesSupport.Within(this, feature);

    /// <inheritdoc />
    public virtual IFeatureQuery Within(Geometry geom) => FeaturesSupport.Within(this, geom);

    /// <inheritdoc />
    public virtual IFeatureQuery Within(IPreparedGeometry prepared) => FeaturesSupport.Within(this, prepared);

    /// <summary>
    /// Returns an enumerator over the features in this view; implemented by each concrete view.
    /// </summary>
    /// <remarks>Implements Java <c>java.lang.Iterable.iterator()</c> (abstract in this base).</remarks>
    public abstract IEnumerator<IFeature> GetEnumerator();

    /// <summary>
    /// Non-generic enumerator, forwarding to the typed <see cref="GetEnumerator"/>.
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    // The base adapts the synchronous enumerator — correct for views whose results are already in
    // memory and never block on a results channel. WorldView overrides this with a non-blocking,
    // tile-streaming enumerator for the spatial query path.
    /// <summary>
    /// Returns an async enumerator over the view's features. The base wraps the synchronous
    /// enumerator; spatial views override it with a non-blocking, tile-streaming implementation.
    /// </summary>
    public virtual async IAsyncEnumerator<IFeature> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        foreach (var feature in this)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return feature;
        }

        await System.Threading.Tasks.Task.CompletedTask.ConfigureAwait(false);
    }

}
