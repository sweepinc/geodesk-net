/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
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

    protected internal readonly Bounds bounds;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView(FeatureStore)</c>.</remarks>
    internal WorldView(FeatureStore store)
        : base(store, TypeBits.ALL, Matcher.ALL, null)
    {
        bounds = World;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView(FeatureStore, int, Bounds, Matcher, Filter)</c>.</remarks>
    internal WorldView(FeatureStore store, int types, Bounds bounds, Matcher matcher, Filter? filter)
        : base(store, types, matcher, filter)
    {
        this.bounds = bounds;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView(WorldView, Bounds)</c>.</remarks>
    WorldView(WorldView other, Bounds bounds)
        : base(other.store, other.types, other.matcher, other.filter)
    {
        this.bounds = bounds;           // TODO: intersect bbox
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override Features NewWith(int types, Matcher matcher, Filter? filter)
    {
        return new WorldView(store, types, bounds, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.in(Bounds)</c>.</remarks>
    public override Features In(Bounds bbox)
    {
        return new WorldView(this, bbox);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.contains(Object)</c>.</remarks>
    public bool Contains(object obj)
    {
        if (obj is StoredFeature feature)
        {
            // Feature must come from this library
            if (feature.Store != store) return false;

            // Feature must fit into the filtered types
            var featureType = 1 << (int)((uint)feature.Flags() >> 1); // shift uses only bottom 5 bits
            if ((types & featureType) == 0) return false;

            // Feature must intersect the view's bbox
            //  TODO: improve bounds(), should return immutable
            if (!feature.Bounds().Intersects(bounds)) return false;

            // Feature must be accepted by matcher
            if (!feature.Matches(matcher)) return false;

            // If this view has a spatial filter, the feature must match that one as well
            if (filter == null) return true;
            return filter.Accept(feature);
        }
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.select(Filter)</c>.</remarks>
    public override Features Select(Filter filter)
    {
        var newTypes = types;
        if (this.filter != null)
        {
            filter = AndFilter.Create(this.filter, filter);
            if (filter == FalseFilter.Instance) return EmptyView.Any;
        }
        var strategy = filter.Strategy();
        if ((strategy & FilterStrategy.RestrictsTypes) != 0)
        {
            newTypes &= filter.AcceptedTypes();
            if (newTypes == 0) return EmptyView.Any;
        }

        // TODO: review: filter type check

        var filterBounds = filter.Bounds();
        // TODO: proper combining of bboxes
        return new WorldView(store, types, filterBounds != null ? filterBounds : bounds, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WorldView.iterator()</c>.</remarks>
    public override IEnumerator<Feature> GetEnumerator()
    {
        return new Query(this);
    }

}
