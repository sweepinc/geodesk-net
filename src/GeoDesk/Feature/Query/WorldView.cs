/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Threading;

using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A Feature Collection that is materialized by running a query against a FeatureStore.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView</c>.</remarks>
public class WorldView : View
{

    protected static readonly Box World = Box.OfWorld();

    protected internal readonly IBounds bounds;

    /// <summary>Creates a world view over the entire store, accepting all features.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView(FeatureStore)</c>.</remarks>
    internal WorldView(FeatureStore store) :
        base(store, TypeBits.ALL, Matcher.ALL, null)
    {
        bounds = World;
    }

    /// <summary>
    /// Creates a world view constrained by the given types, bounding box, matcher, and
    /// optional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView(FeatureStore, int, Bounds, Matcher, Filter)</c>.</remarks>
    internal WorldView(FeatureStore store, int types, IBounds bounds, Matcher matcher, IFilter? filter) :
        base(store, types, matcher, filter)
    {
        this.bounds = bounds;
    }

    /// <summary>Creates a copy of another world view restricted to the given bounding box.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView(WorldView, Bounds)</c>.</remarks>
    WorldView(WorldView other, IBounds bounds) :
        base(other.store, other.types, other.matcher, other.filter)
    {
        this.bounds = bounds; // TODO: intersect bbox
    }

    /// <summary>
    /// Returns a new world view over the same store and bounds with the given type,
    /// matcher, and filter constraints applied.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return new WorldView(store, types, bounds, matcher, filter);
    }

    /// <summary>Returns a new world view restricted to the given bounding box.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.in(Bounds)</c>.</remarks>
    public override IFeatureQuery In(IBounds bbox)
    {
        return new WorldView(this, bbox);
    }

    /// <summary>
    /// Returns true if the given object is a feature from this store that satisfies all
    /// of this view's constraints (type, bounds, matcher, and filter).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.contains(Object)</c>.</remarks>
    public bool Contains(object obj)
    {
        if (obj is StoredFeature feature)
        {
            // Feature must come from this library
            if (feature.Store != store)
                return false;

            // Feature must fit into the filtered types
            var featureType = 1 << (int)((uint)feature.Flags() >> 1); // shift uses only bottom 5 bits
            if ((types & featureType) == 0)
                return false;

            // Feature must intersect the view's bbox
            //  TODO: improve bounds(), should return immutable
            if (!feature.Bounds.Intersects(bounds))
                return false;

            // Feature must be accepted by matcher
            if (!feature.Matches(matcher))
                return false;

            // If this view has a spatial filter, the feature must match that one as well
            if (filter == null)
                return true;
            return filter.Accept(feature);
        }
        return false;
    }

    /// <summary>
    /// Returns a new view that additionally applies the given filter, combining it with
    /// any existing filter and narrowing the accepted types and bounds. May collapse to
    /// an empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.select(Filter)</c>.</remarks>
    public override IFeatureQuery Select(IFilter filter)
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

        // TODO: review: filter type check

        var filterBounds = filter.Bounds;
        // TODO: proper combining of bboxes
        return new WorldView(store, types, filterBounds != null ? filterBounds : bounds, matcher, filter);
    }

    /// <summary>
    /// Returns a (prefetching) iterator that runs the query and yields matching
    /// features.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return new Query(this);
    }

    // The non-blocking counterpart of GetEnumerator: the Query is built without prefetching, so it
    // streams features as each tile's results arrive on the channel — MoveNextAsync awaits between
    // tiles rather than parking a thread. This is the path a web server should use.
    /// <summary>
    /// Returns an async iterator that streams matching features as each tile's results
    /// arrive, awaiting between tiles rather than blocking a thread.
    /// </summary>
    public override IAsyncEnumerator<IFeature> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new Query(this, prefetch: false, cancellationToken);
    }

}
