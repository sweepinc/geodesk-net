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
/// A collection of features.
/// </summary>
public interface Features : IEnumerable<Feature>
{
    /// <summary>Returns a view containing only features matching the given query.</summary>
    Features Select(string query);

    /// <summary>Returns a view that contains only nodes.</summary>
    Features Nodes();

    /// <summary>Returns a view that contains only nodes matching the given query.</summary>
    Features Nodes(string query);

    /// <summary>Returns a view that contains only ways.</summary>
    Features Ways();

    /// <summary>Returns a view that contains only ways matching the given query.</summary>
    Features Ways(string query);

    /// <summary>Returns a view that contains only relations.</summary>
    Features Relations();

    /// <summary>Returns a view that contains only relations matching the given query.</summary>
    Features Relations(string query);

    /// <summary>Returns a sub-view of features that are nodes of the given way.</summary>
    Features NodesOf(Feature parent)
    {
        throw new QueryException("Not implemented for this query.");
    }

    /// <summary>
    /// Returns the features that are nodes of the given way, or members of the given relation.
    /// </summary>
    Features MembersOf(Feature parent)
    {
        throw new QueryException("Not implemented for this query.");
    }

    /// <summary>Returns the features that are parent elements of the given feature.</summary>
    Features ParentsOf(Feature child)
    {
        throw new QueryException("Not implemented for this query.");
    }

    /// <summary>Returns a view of features whose bounding box intersects the given bounds.</summary>
    Features In(Bounds bbox);

    /// <summary>Returns the first feature in the collection, or null if empty.</summary>
    Feature? First()
    {
        using IEnumerator<Feature> iter = GetEnumerator();
        return iter.MoveNext() ? iter.Current : null;
    }

    /// <summary>Returns the number of features in this collection.</summary>
    long Count()
    {
        long count = 0;
        using IEnumerator<Feature> iter = GetEnumerator();
        while (iter.MoveNext())
        {
            count++;
        }
        return count;
    }

    /// <summary>Returns true if this collection contains no features.</summary>
    bool IsEmpty()
    {
        return First() == null;
    }

    /// <summary>Creates a list containing all features in this collection.</summary>
    List<Feature> ToList()
    {
        List<Feature> list = new List<Feature>();
        foreach (Feature f in this) list.Add(f);
        return list;
    }

    /// <summary>Creates an array containing all features in this collection.</summary>
    Feature[] ToArray()
    {
        return ToList().ToArray();
    }

    /// <summary>Checks whether this collection contains the given object.</summary>
    bool Contains(object f)
    {
        using IEnumerator<Feature> iter = GetEnumerator();
        while (iter.MoveNext())
        {
            if (f.Equals(iter.Current)) return true;
        }
        return false;
    }

    /// <summary>Returns a view filtered by the given <see cref="Filter"/>.</summary>
    Features Select(Filter filter);

    /// <summary>Returns the features present in both this collection and <paramref name="other"/>.</summary>
    Features Select(Features other);

    // --- Spatial-predicate filters (ported from com.geodesk.feature.Features default methods) ---

    /// <summary>Returns all features that have at least one common vertex with the given feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.connectedTo(Feature)</c>.</remarks>
    Features ConnectedTo(Feature f) => Select(new ConnectedFilter(f));

    /// <summary>
    /// Returns all features that have at least one common vertex with the given Geometry.
    /// Coordinates of the Geometry are rounded to integers.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.connectedTo(Geometry)</c>.</remarks>
    Features ConnectedTo(Geometry geom) => Select(new ConnectedFilter(geom));

    /// <summary>Returns all features that contain the given Mercator-projected coordinate.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containingXY(int, int)</c>.</remarks>
    Features ContainingXY(int x, int y) => Select(new ContainsPointFilter(x, y));

    /// <summary>Returns all features that contain the given longitude/latitude coordinate.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containingLonLat(double, double)</c>.</remarks>
    Features ContainingLonLat(double lon, double lat)
    {
        var x = (int)Mercator.XFromLon(lon);
        var y = (int)Mercator.YFromLat(lat);
        return Select(new ContainsPointFilter(x, y));
    }

    /// <summary>Returns all features that contain the given feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containing(Feature)</c>.</remarks>
    Features Containing(Feature feature)
    {
        if (feature is Node) return Select(new ContainsPointFilter(feature.X(), feature.Y()));
        return Select(new ContainsFilter(feature));
    }

    /// <summary>Returns all features that contain the given Geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containing(Geometry)</c>.</remarks>
    Features Containing(Geometry geom) => Select(new ContainsFilter(geom));

    /// <summary>Returns all features that contain the given PreparedGeometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containing(PreparedGeometry)</c>.</remarks>
    Features Containing(IPreparedGeometry prepared) => Select(new ContainsFilter(prepared));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.coveredBy(Feature)</c>.</remarks>
    Features CoveredBy(Feature feature) => Select(new CoveredByFilter(feature));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.coveredBy(Geometry)</c>.</remarks>
    Features CoveredBy(Geometry geom) => Select(new CoveredByFilter(geom));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.coveredBy(PreparedGeometry)</c>.</remarks>
    Features CoveredBy(IPreparedGeometry prepared) => Select(new CoveredByFilter(prepared));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.crossing(Feature)</c>.</remarks>
    Features Crossing(Feature feature) => Select(new CrossesFilter(feature));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.crossing(Geometry)</c>.</remarks>
    Features Crossing(Geometry geom) => Select(new CrossesFilter(geom));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.crossing(PreparedGeometry)</c>.</remarks>
    Features Crossing(IPreparedGeometry prepared) => Select(new CrossesFilter(prepared));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.disjoint(Feature)</c>.</remarks>
    Features Disjoint(Feature feature) => Select(new DisjointFilter(feature));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.disjoint(Geometry)</c>.</remarks>
    Features Disjoint(Geometry geom) => Select(new DisjointFilter(geom));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.disjoint(PreparedGeometry)</c>.</remarks>
    Features Disjoint(IPreparedGeometry prepared) => Select(new DisjointFilter(prepared));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.intersecting(Feature)</c>.</remarks>
    Features Intersecting(Feature feature) => Select(new IntersectsFilter(feature));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.intersecting(Geometry)</c>.</remarks>
    Features Intersecting(Geometry geom) => Select(new IntersectsFilter(geom));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.intersecting(PreparedGeometry)</c>.</remarks>
    Features Intersecting(IPreparedGeometry prepared) => Select(new IntersectsFilter(prepared));

    /// <summary>Returns all features whose closest point lies within a given radius (meters).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFromXY(double, int, int)</c>.</remarks>
    Features MaxMetersFromXY(double distance, int x, int y) => Select(new PointDistanceFilter(distance, x, y));

    /// <summary>Returns all features whose closest point lies within a given radius (meters).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFromLonLat(double, double, double)</c>.</remarks>
    Features MaxMetersFromLonLat(double distance, double lon, double lat)
    {
        var x = (int)Mercator.XFromLon(lon);
        var y = (int)Mercator.YFromLat(lat);
        return Select(new PointDistanceFilter(distance, x, y));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFrom(double, Geometry)</c>.</remarks>
    Features MaxMetersFrom(double distance, Geometry geom)
    {
        throw new NotImplementedException("todo");     // TODO
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFrom(double, Feature)</c>.</remarks>
    Features MaxMetersFrom(double distance, Feature feature)
    {
        throw new NotImplementedException("todo");     // TODO
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.overlapping(Feature)</c>.</remarks>
    Features Overlapping(Feature feature) => Select(new OverlapsFilter(feature));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.overlapping(Geometry)</c>.</remarks>
    Features Overlapping(Geometry geom) => Select(new OverlapsFilter(geom));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.overlapping(PreparedGeometry)</c>.</remarks>
    Features Overlapping(IPreparedGeometry prepared) => Select(new OverlapsFilter(prepared));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.touching(Feature)</c>.</remarks>
    Features Touching(Feature feature) => Select(new TouchesFilter(feature));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.touching(Geometry)</c>.</remarks>
    Features Touching(Geometry geom) => Select(new TouchesFilter(geom));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.touching(PreparedGeometry)</c>.</remarks>
    Features Touching(IPreparedGeometry prepared) => Select(new TouchesFilter(prepared));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.within(Feature)</c>.</remarks>
    Features Within(Feature feature) => Select(new WithinFilter(feature));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.within(Geometry)</c>.</remarks>
    Features Within(Geometry geom) => Select(new WithinFilter(geom));

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.within(PreparedGeometry)</c>.</remarks>
    Features Within(IPreparedGeometry prepared) => Select(new WithinFilter(prepared));

    // PORT: Java's node()/way()/relation() are renamed GetNode()/GetWay()/GetRelation() to avoid
    // colliding with the Node/Way/Relation types (C# is case-insensitive across method/type names).

    /// <summary>Returns the node with the given ID (or null).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.node(long)</c>.</remarks>
    Node? GetNode(long id) => (Node?)Select(new IdFilter(TypeBits.NODES, id)).First();

    /// <summary>Returns the way with the given ID (or null).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.way(long)</c>.</remarks>
    Way? GetWay(long id) => (Way?)Select(new IdFilter(TypeBits.WAYS, id)).First();

    /// <summary>Returns the relation with the given ID (or null).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.relation(long)</c>.</remarks>
    Relation? GetRelation(long id) => (Relation?)Select(new IdFilter(TypeBits.RELATIONS, id)).First();

    /// <summary>Adds all features in this collection to the given collection.</summary>
    void AddTo(ICollection<Feature> collection)
    {
        foreach (Feature f in this) collection.Add(f);
    }

    /// <summary>Opens the given Geographic Object Library and returns all of its features.</summary>
    static Features Open(string path)
    {
        return new FeatureLibrary(path);
    }
}
