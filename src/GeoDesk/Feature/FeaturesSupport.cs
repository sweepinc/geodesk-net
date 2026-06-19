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

    internal static IFeature? First(IFeatureQuery self)
    {
        using var iter = self.GetEnumerator();
        return iter.MoveNext() ? iter.Current : null;
    }

    internal static long Count(IFeatureQuery self)
    {
        long count = 0;
        using var iter = self.GetEnumerator();
        while (iter.MoveNext())
            count++;
        return count;
    }

    internal static bool IsEmpty(IFeatureQuery self) => First(self) == null;

    internal static List<IFeature> ToList(IFeatureQuery self) => [.. self];

    internal static IFeature[] ToArray(IFeatureQuery self) => ToList(self).ToArray();

    internal static bool Contains(IFeatureQuery self, object f)
    {
        using var iter = self.GetEnumerator();
        while (iter.MoveNext())
            if (f.Equals(iter.Current))
                return true;

        return false;
    }

    internal static void AddTo(IFeatureQuery self, ICollection<IFeature> collection)
    {
        foreach (var f in self)
            collection.Add(f);
    }

    // --- by-id lookups ---

    internal static INode? GetNode(IFeatureQuery self, long id) => (INode?)First(self.Select(new IdFilter(TypeBits.NODES, id)));

    internal static IWay? GetWay(IFeatureQuery self, long id) => (IWay?)First(self.Select(new IdFilter(TypeBits.WAYS, id)));

    internal static IRelation? GetRelation(IFeatureQuery self, long id) => (IRelation?)First(self.Select(new IdFilter(TypeBits.RELATIONS, id)));

    // --- spatial-predicate filters ---

    internal static IFeatureQuery ConnectedTo(IFeatureQuery self, IFeature f) => self.Select(new ConnectedFilter(f));
    internal static IFeatureQuery ConnectedTo(IFeatureQuery self, Geometry geom) => self.Select(new ConnectedFilter(geom));

    internal static IFeatureQuery ContainingXY(IFeatureQuery self, int x, int y) => self.Select(new ContainsPointFilter(x, y));

    internal static IFeatureQuery ContainingLonLat(IFeatureQuery self, double lon, double lat)
    {
        var x = Mercator.XFromLon(lon);
        var y = Mercator.YFromLat(lat);
        return self.Select(new ContainsPointFilter(x, y));
    }

    internal static IFeatureQuery Containing(IFeatureQuery self, IFeature feature)
    {
        if (feature is INode)
            return self.Select(new ContainsPointFilter(feature.X, feature.Y));
        else
            return self.Select(new ContainsFilter(feature));
    }

    internal static IFeatureQuery Containing(IFeatureQuery self, Geometry geom) => self.Select(new ContainsFilter(geom));

    internal static IFeatureQuery Containing(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new ContainsFilter(prepared));

    internal static IFeatureQuery CoveredBy(IFeatureQuery self, IFeature feature) => self.Select(new CoveredByFilter(feature));

    internal static IFeatureQuery CoveredBy(IFeatureQuery self, Geometry geom) => self.Select(new CoveredByFilter(geom));

    internal static IFeatureQuery CoveredBy(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new CoveredByFilter(prepared));

    internal static IFeatureQuery Crossing(IFeatureQuery self, IFeature feature) => self.Select(new CrossesFilter(feature));

    internal static IFeatureQuery Crossing(IFeatureQuery self, Geometry geom) => self.Select(new CrossesFilter(geom));

    internal static IFeatureQuery Crossing(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new CrossesFilter(prepared));

    internal static IFeatureQuery Disjoint(IFeatureQuery self, IFeature feature) => self.Select(new DisjointFilter(feature));

    internal static IFeatureQuery Disjoint(IFeatureQuery self, Geometry geom) => self.Select(new DisjointFilter(geom));

    internal static IFeatureQuery Disjoint(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new DisjointFilter(prepared));

    internal static IFeatureQuery Intersecting(IFeatureQuery self, IFeature feature) => self.Select(new IntersectsFilter(feature));

    internal static IFeatureQuery Intersecting(IFeatureQuery self, Geometry geom) => self.Select(new IntersectsFilter(geom));

    internal static IFeatureQuery Intersecting(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new IntersectsFilter(prepared));

    internal static IFeatureQuery MaxMetersFromXY(IFeatureQuery self, double distance, int x, int y) => self.Select(new PointDistanceFilter(distance, x, y));

    internal static IFeatureQuery MaxMetersFromLonLat(IFeatureQuery self, double distance, double lon, double lat)
    {
        var x = Mercator.XFromLon(lon);
        var y = Mercator.YFromLat(lat);
        return self.Select(new PointDistanceFilter(distance, x, y));
    }

    internal static IFeatureQuery MaxMetersFrom(IFeatureQuery self, double distance, Geometry geom) => throw new NotImplementedException("todo");

    internal static IFeatureQuery MaxMetersFrom(IFeatureQuery self, double distance, IFeature feature) => throw new NotImplementedException("todo");

    internal static IFeatureQuery Overlapping(IFeatureQuery self, IFeature feature) => self.Select(new OverlapsFilter(feature));

    internal static IFeatureQuery Overlapping(IFeatureQuery self, Geometry geom) => self.Select(new OverlapsFilter(geom));

    internal static IFeatureQuery Overlapping(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new OverlapsFilter(prepared));

    internal static IFeatureQuery Touching(IFeatureQuery self, IFeature feature) => self.Select(new TouchesFilter(feature));

    internal static IFeatureQuery Touching(IFeatureQuery self, Geometry geom) => self.Select(new TouchesFilter(geom));

    internal static IFeatureQuery Touching(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new TouchesFilter(prepared));

    internal static IFeatureQuery Within(IFeatureQuery self, IFeature feature) => self.Select(new WithinFilter(feature));

    internal static IFeatureQuery Within(IFeatureQuery self, Geometry geom) => self.Select(new WithinFilter(geom));

    internal static IFeatureQuery Within(IFeatureQuery self, IPreparedGeometry prepared) => self.Select(new WithinFilter(prepared));

}
