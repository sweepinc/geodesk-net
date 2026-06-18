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
/// Shared implementations of <see cref="IFeatures"/>' convenience operations. Both the interface's
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

    internal static IFeature? First(IFeatures self)
    {
        using var iter = self.GetEnumerator();
        return iter.MoveNext() ? iter.Current : null;
    }

    internal static long Count(IFeatures self)
    {
        long count = 0;
        using var iter = self.GetEnumerator();
        while (iter.MoveNext())
            count++;
        return count;
    }

    internal static bool IsEmpty(IFeatures self) => First(self) == null;

    internal static List<IFeature> ToList(IFeatures self) => [.. self];

    internal static IFeature[] ToArray(IFeatures self) => ToList(self).ToArray();

    internal static bool Contains(IFeatures self, object f)
    {
        using var iter = self.GetEnumerator();
        while (iter.MoveNext())
            if (f.Equals(iter.Current))
                return true;

        return false;
    }

    internal static void AddTo(IFeatures self, ICollection<IFeature> collection)
    {
        foreach (var f in self)
            collection.Add(f);
    }

    // --- by-id lookups ---

    internal static INode? GetNode(IFeatures self, long id) => (INode?)First(self.Select(new IdFilter(TypeBits.NODES, id)));

    internal static IWay? GetWay(IFeatures self, long id) => (IWay?)First(self.Select(new IdFilter(TypeBits.WAYS, id)));

    internal static IRelation? GetRelation(IFeatures self, long id) => (IRelation?)First(self.Select(new IdFilter(TypeBits.RELATIONS, id)));

    // --- spatial-predicate filters ---

    internal static IFeatures ConnectedTo(IFeatures self, IFeature f) => self.Select(new ConnectedFilter(f));
    internal static IFeatures ConnectedTo(IFeatures self, Geometry geom) => self.Select(new ConnectedFilter(geom));

    internal static IFeatures ContainingXY(IFeatures self, int x, int y) => self.Select(new ContainsPointFilter(x, y));

    internal static IFeatures ContainingLonLat(IFeatures self, double lon, double lat)
    {
        var x = Mercator.XFromLon(lon);
        var y = Mercator.YFromLat(lat);
        return self.Select(new ContainsPointFilter(x, y));
    }

    internal static IFeatures Containing(IFeatures self, IFeature feature)
    {
        if (feature is INode)
            return self.Select(new ContainsPointFilter(feature.X(), feature.Y()));
        else
            return self.Select(new ContainsFilter(feature));
    }

    internal static IFeatures Containing(IFeatures self, Geometry geom) => self.Select(new ContainsFilter(geom));

    internal static IFeatures Containing(IFeatures self, IPreparedGeometry prepared) => self.Select(new ContainsFilter(prepared));

    internal static IFeatures CoveredBy(IFeatures self, IFeature feature) => self.Select(new CoveredByFilter(feature));

    internal static IFeatures CoveredBy(IFeatures self, Geometry geom) => self.Select(new CoveredByFilter(geom));

    internal static IFeatures CoveredBy(IFeatures self, IPreparedGeometry prepared) => self.Select(new CoveredByFilter(prepared));

    internal static IFeatures Crossing(IFeatures self, IFeature feature) => self.Select(new CrossesFilter(feature));

    internal static IFeatures Crossing(IFeatures self, Geometry geom) => self.Select(new CrossesFilter(geom));

    internal static IFeatures Crossing(IFeatures self, IPreparedGeometry prepared) => self.Select(new CrossesFilter(prepared));

    internal static IFeatures Disjoint(IFeatures self, IFeature feature) => self.Select(new DisjointFilter(feature));

    internal static IFeatures Disjoint(IFeatures self, Geometry geom) => self.Select(new DisjointFilter(geom));

    internal static IFeatures Disjoint(IFeatures self, IPreparedGeometry prepared) => self.Select(new DisjointFilter(prepared));

    internal static IFeatures Intersecting(IFeatures self, IFeature feature) => self.Select(new IntersectsFilter(feature));

    internal static IFeatures Intersecting(IFeatures self, Geometry geom) => self.Select(new IntersectsFilter(geom));

    internal static IFeatures Intersecting(IFeatures self, IPreparedGeometry prepared) => self.Select(new IntersectsFilter(prepared));

    internal static IFeatures MaxMetersFromXY(IFeatures self, double distance, int x, int y) => self.Select(new PointDistanceFilter(distance, x, y));

    internal static IFeatures MaxMetersFromLonLat(IFeatures self, double distance, double lon, double lat)
    {
        var x = Mercator.XFromLon(lon);
        var y = Mercator.YFromLat(lat);
        return self.Select(new PointDistanceFilter(distance, x, y));
    }

    internal static IFeatures MaxMetersFrom(IFeatures self, double distance, Geometry geom) => throw new NotImplementedException("todo");

    internal static IFeatures MaxMetersFrom(IFeatures self, double distance, IFeature feature) => throw new NotImplementedException("todo");

    internal static IFeatures Overlapping(IFeatures self, IFeature feature) => self.Select(new OverlapsFilter(feature));

    internal static IFeatures Overlapping(IFeatures self, Geometry geom) => self.Select(new OverlapsFilter(geom));

    internal static IFeatures Overlapping(IFeatures self, IPreparedGeometry prepared) => self.Select(new OverlapsFilter(prepared));

    internal static IFeatures Touching(IFeatures self, IFeature feature) => self.Select(new TouchesFilter(feature));

    internal static IFeatures Touching(IFeatures self, Geometry geom) => self.Select(new TouchesFilter(geom));

    internal static IFeatures Touching(IFeatures self, IPreparedGeometry prepared) => self.Select(new TouchesFilter(prepared));

    internal static IFeatures Within(IFeatures self, IFeature feature) => self.Select(new WithinFilter(feature));

    internal static IFeatures Within(IFeatures self, Geometry geom) => self.Select(new WithinFilter(geom));

    internal static IFeatures Within(IFeatures self, IPreparedGeometry prepared) => self.Select(new WithinFilter(prepared));

}
