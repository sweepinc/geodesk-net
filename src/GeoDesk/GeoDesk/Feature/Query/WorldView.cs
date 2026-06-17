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

/// A Feature Collection that is materialized by running a query against
/// a FeatureStore.
///
/// @hidden
public class WorldView : View
{
    /// @hidden
    protected internal readonly Bounds bounds;

    /// @hidden
    protected static readonly Box WORLD = Box.OfWorld();

    public WorldView(FeatureStore store)
        : base(store, TypeBits.ALL, Matcher.ALL, null)
    {
        this.bounds = WORLD;
    }

    public WorldView(FeatureStore store, int types, Bounds bounds, Matcher matcher, Filter? filter)
        : base(store, types, matcher, filter)
    {
        this.bounds = bounds;
    }

    /// @hidden
    protected override Features NewWith(int types, Matcher matcher, Filter? filter)
    {
        return new WorldView(store, types, bounds, matcher, filter);
    }

    private WorldView(WorldView other, Bounds bounds)
        : base(other.store, other.types, other.matcher, other.filter)
    {
        this.bounds = bounds;           // TODO: intersect bbox
    }

    public override Features In(Bounds bbox)
    {
        return new WorldView(this, bbox);
    }

    public bool Contains(object obj)
    {
        if (obj is StoredFeature feature)
        {
            // Feature must come from this library
            if (feature.Store != store) return false;

            // Feature must fit into the filtered types
            int featureType = 1 << (int)((uint)feature.Flags() >> 1); // shift uses only bottom 5 bits
            if ((types & featureType) == 0) return false;

            // Feature must intersect the view's bbox
            //  TODO: improve bounds(), should return immutable
            if (!feature.Bounds().Intersects(bounds)) return false;

            // Feature must be accepted by matcher
            if (!feature.Matches(matcher)) return false;

            // If this view has a spatial filter, the feature must match
            // that one as well
            if (filter == null) return true;
            return filter.Accept(feature);
        }
        return false;
    }

    public override Features Select(Filter filter)
    {
        int newTypes = types;
        if (this.filter != null)
        {
            filter = AndFilter.Create(this.filter, filter);
            if (filter == FalseFilter.INSTANCE) return EmptyView.ANY;
        }
        int strategy = filter.Strategy();
        if ((strategy & FilterStrategy.RESTRICTS_TYPES) != 0)
        {
            newTypes &= filter.AcceptedTypes();
            if (newTypes == 0) return EmptyView.ANY;
        }

        // TODO: review: filter type check

        Bounds? filterBounds = filter.Bounds();
        // TODO: proper combining of bboxes
        return new WorldView(store, types, filterBounds != null ? filterBounds : bounds,
            matcher, filter);
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        return new Query(this);
    }
}
