/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;

using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature;

/// <summary>
/// Shared implementations of <see cref="IFeatureQuery"/>' convenience operations. Both the interface's
/// default methods and <see cref="GeoDesk.Feature.Query.View"/>'s concrete members delegate here, so
/// the logic lives once. (C# default interface methods are only callable through the interface; a
/// concrete method cannot delegate back to the default — so the body is hoisted here instead.)
/// </summary>
/// <remarks>Port-only helper (no Java counterpart): in Java these are interface default methods,
/// which are inherited as callable members of the implementing class; this helper reproduces that
/// for C#.</remarks>
internal static class FeaturesSupport
{

    // --- terminal operations ---

    /// <summary>
    /// Returns the first feature produced by the query, or <c>null</c> if it yields none.
    /// </summary>
    internal static IFeature? First(IFeatureQuery self)
    {
        using var iter = self.GetEnumerator();
        return iter.MoveNext() ? iter.Current : null;
    }

    /// <summary>
    /// Counts the features produced by the query by fully enumerating it.
    /// </summary>
    internal static long Count(IFeatureQuery self)
    {
        long count = 0;
        using var iter = self.GetEnumerator();
        while (iter.MoveNext())
            count++;
        return count;
    }

    /// <summary>
    /// Returns true if the query yields no features (checks only the first element).
    /// </summary>
    internal static bool IsEmpty(IFeatureQuery self) => First(self) == null;

    /// <summary>
    /// Materializes the query into a new <see cref="List{T}"/>.
    /// </summary>
    internal static List<IFeature> ToList(IFeatureQuery self) => [.. self];

    /// <summary>
    /// Materializes the query into a new array.
    /// </summary>
    internal static IFeature[] ToArray(IFeatureQuery self) => ToList(self).ToArray();

    /// <summary>
    /// Returns true if the query yields a feature equal to the given object.
    /// </summary>
    internal static bool Contains(IFeatureQuery self, object f)
    {
        using var iter = self.GetEnumerator();
        while (iter.MoveNext())
            if (f.Equals(iter.Current))
                return true;

        return false;
    }

    /// <summary>
    /// Adds every feature produced by the query to the given target collection.
    /// </summary>
    internal static void AddTo(IFeatureQuery self, ICollection<IFeature> collection)
    {
        foreach (var f in self)
            collection.Add(f);
    }

    // --- by-id lookups ---

    /// <summary>
    /// Returns the node with the given OSM id from the query, or <c>null</c> if not found.
    /// </summary>
    internal static INode? GetNode(IFeatureQuery self, long id) => (INode?)First(self.Select(new IdFilter(TypeBits.NODES, id)));

    /// <summary>
    /// Returns the way with the given OSM id from the query, or <c>null</c> if not found.
    /// </summary>
    internal static IWay? GetWay(IFeatureQuery self, long id) => (IWay?)First(self.Select(new IdFilter(TypeBits.WAYS, id)));

    /// <summary>
    /// Returns the relation with the given OSM id from the query, or <c>null</c> if not found.
    /// </summary>
    internal static IRelation? GetRelation(IFeatureQuery self, long id) => (IRelation?)First(self.Select(new IdFilter(TypeBits.RELATIONS, id)));

    // --- spatial-predicate filters ---

    /// <summary>
    /// Narrows the query to features connected to (sharing a vertex with) the given feature.
    /// </summary>
    internal static IFeatureQuery ConnectedTo(IFeatureQuery self, IFeature f) => self.Select(new ConnectedFilter(f));

    /// <summary>
    /// Narrows the query to features connected to (sharing a vertex with) the given geometry.
    /// </summary>
    internal static IFeatureQuery ConnectedTo(IFeatureQuery self, Geometry geom) => self.Select(new ConnectedFilter(geom));

    /// <summary>
    /// Narrows the query to features that contain the given Mercator-projected point.
    /// </summary>
    internal static IFeatureQuery ContainingXY(IFeatureQuery self, int x, int y) => self.Select(new ContainsPointFilter(x, y));

    /// <summary>
    /// Narrows the query to features that contain the given longitude/latitude point, projecting it
    /// to Mercator coordinates first.
    /// </summary>
    internal static IFeatureQuery ContainingLonLat(IFeatureQuery self, double lon, double lat)
    {
        var x = Mercator.XFromLon(lon);
        var y = Mercator.YFromLat(lat);
        return self.Select(new ContainsPointFilter(x, y));
    }

    /// <summary>
    /// Narrows the query to features that spatially contain the given feature. Nodes are handled via
    /// a cheaper point-in-feature test.
    /// </summary>
    internal static IFeatureQuery Containing(IFeatureQuery self, IFeature feature)
    {
        if (feature is INode)
            return self.Select(new ContainsPointFilter(feature.X, feature.Y));
        else
            return self.Select(new ContainsFilter(feature));
    }

    /// <summary>
    /// Narrows the query to features that spatially contain the given geometry.
    /// </summary>
    internal static IFeatureQuery Containing(IFeatureQuery self, Geometry geom) => self.Select(new ContainsFilter(geom));

    /// <summary>
    /// Narrows the query to features that spatially contain the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery Containing(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new ContainsFilter(prepared));

    /// <summary>
    /// Narrows the query to features covered by the given feature's geometry.
    /// </summary>
    internal static IFeatureQuery CoveredBy(IFeatureQuery self, IFeature feature) => self.Select(new CoveredByFilter(feature));

    /// <summary>
    /// Narrows the query to features covered by the given geometry.
    /// </summary>
    internal static IFeatureQuery CoveredBy(IFeatureQuery self, Geometry geom) => self.Select(new CoveredByFilter(geom));

    /// <summary>
    /// Narrows the query to features covered by the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery CoveredBy(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new CoveredByFilter(prepared));

    /// <summary>
    /// Narrows the query to features that cross the given feature's geometry.
    /// </summary>
    internal static IFeatureQuery Crossing(IFeatureQuery self, IFeature feature) => self.Select(new CrossesFilter(feature));

    /// <summary>
    /// Narrows the query to features that cross the given geometry.
    /// </summary>
    internal static IFeatureQuery Crossing(IFeatureQuery self, Geometry geom) => self.Select(new CrossesFilter(geom));

    /// <summary>
    /// Narrows the query to features that cross the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery Crossing(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new CrossesFilter(prepared));

    /// <summary>
    /// Narrows the query to features disjoint from the given feature's geometry.
    /// </summary>
    internal static IFeatureQuery Disjoint(IFeatureQuery self, IFeature feature) => self.Select(new DisjointFilter(feature));

    /// <summary>
    /// Narrows the query to features disjoint from the given geometry.
    /// </summary>
    internal static IFeatureQuery Disjoint(IFeatureQuery self, Geometry geom) => self.Select(new DisjointFilter(geom));

    /// <summary>
    /// Narrows the query to features disjoint from the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery Disjoint(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new DisjointFilter(prepared));

    /// <summary>
    /// Narrows the query to features that intersect the given feature's geometry.
    /// </summary>
    internal static IFeatureQuery Intersecting(IFeatureQuery self, IFeature feature) => self.Select(new IntersectsFilter(feature));

    /// <summary>
    /// Narrows the query to features that intersect the given geometry.
    /// </summary>
    internal static IFeatureQuery Intersecting(IFeatureQuery self, Geometry geom) => self.Select(new IntersectsFilter(geom));

    /// <summary>
    /// Narrows the query to features that intersect the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery Intersecting(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new IntersectsFilter(prepared));

    /// <summary>
    /// Narrows the query to features whose closest point is within the given distance (meters) of the
    /// given Mercator-projected point.
    /// </summary>
    internal static IFeatureQuery MaxMetersFromXY(IFeatureQuery self, double distance, int x, int y) => self.Select(new PointDistanceFilter(distance, x, y));

    /// <summary>
    /// Narrows the query to features whose closest point is within the given distance (meters) of the
    /// given longitude/latitude point, projecting it to Mercator coordinates first.
    /// </summary>
    internal static IFeatureQuery MaxMetersFromLonLat(IFeatureQuery self, double distance, double lon, double lat)
    {
        var x = Mercator.XFromLon(lon);
        var y = Mercator.YFromLat(lat);
        return self.Select(new PointDistanceFilter(distance, x, y));
    }

    /// <summary>
    /// Intended to narrow the query to features within the given distance of a geometry. Not yet
    /// implemented; throws <see cref="NotImplementedException"/>.
    /// </summary>
    internal static IFeatureQuery MaxMetersFrom(IFeatureQuery self, double distance, Geometry geom) => throw new NotImplementedException("todo");

    /// <summary>
    /// Intended to narrow the query to features within the given distance of another feature. Not yet
    /// implemented; throws <see cref="NotImplementedException"/>.
    /// </summary>
    internal static IFeatureQuery MaxMetersFrom(IFeatureQuery self, double distance, IFeature feature) => throw new NotImplementedException("todo");

    /// <summary>
    /// Narrows the query to features that overlap the given feature's geometry.
    /// </summary>
    internal static IFeatureQuery Overlapping(IFeatureQuery self, IFeature feature) => self.Select(new OverlapsFilter(feature));

    /// <summary>
    /// Narrows the query to features that overlap the given geometry.
    /// </summary>
    internal static IFeatureQuery Overlapping(IFeatureQuery self, Geometry geom) => self.Select(new OverlapsFilter(geom));

    /// <summary>
    /// Narrows the query to features that overlap the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery Overlapping(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new OverlapsFilter(prepared));

    /// <summary>
    /// Narrows the query to features that touch the given feature's geometry.
    /// </summary>
    internal static IFeatureQuery Touching(IFeatureQuery self, IFeature feature) => self.Select(new TouchesFilter(feature));

    /// <summary>
    /// Narrows the query to features that touch the given geometry.
    /// </summary>
    internal static IFeatureQuery Touching(IFeatureQuery self, Geometry geom) => self.Select(new TouchesFilter(geom));

    /// <summary>
    /// Narrows the query to features that touch the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery Touching(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new TouchesFilter(prepared));

    /// <summary>
    /// Narrows the query to features within the given feature's geometry.
    /// </summary>
    internal static IFeatureQuery Within(IFeatureQuery self, IFeature feature) => self.Select(new WithinFilter(feature));

    /// <summary>
    /// Narrows the query to features within the given geometry.
    /// </summary>
    internal static IFeatureQuery Within(IFeatureQuery self, Geometry geom) => self.Select(new WithinFilter(geom));

    /// <summary>
    /// Narrows the query to features within the given prepared geometry.
    /// </summary>
    internal static IFeatureQuery Within(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new WithinFilter(prepared));

}
