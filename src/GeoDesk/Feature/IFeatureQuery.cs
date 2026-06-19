/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using GeoDesk.Feature.Match;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature;

/// <summary>
/// A collection of features.
/// </summary>
public interface IFeatureQuery : IEnumerable<IFeature>
{

    /// <summary>Returns a view containing only features matching the given query.</summary>
    IFeatureQuery Select(string query);

    /// <summary>Returns a view that contains only nodes.</summary>
    IFeatureQuery Nodes();

    /// <summary>Returns a view that contains only nodes matching the given query.</summary>
    IFeatureQuery Nodes(string query);

    /// <summary>Returns a view that contains only ways.</summary>
    IFeatureQuery Ways();

    /// <summary>Returns a view that contains only ways matching the given query.</summary>
    IFeatureQuery Ways(string query);

    /// <summary>Returns a view that contains only relations.</summary>
    IFeatureQuery Relations();

    /// <summary>Returns a view that contains only relations matching the given query.</summary>
    IFeatureQuery Relations(string query);

    /// <summary>Returns a sub-view of features that are nodes of the given way.</summary>
    IFeatureQuery NodesOf(IFeature parent)
    {
        throw new QueryException("Not implemented for this query.");
    }

    /// <summary>
    /// Returns the features that are nodes of the given way, or members of the given relation.
    /// </summary>
    IFeatureQuery MembersOf(IFeature parent)
    {
        throw new QueryException("Not implemented for this query.");
    }

    /// <summary>Returns the features that are parent elements of the given feature.</summary>
    IFeatureQuery ParentsOf(IFeature child)
    {
        throw new QueryException("Not implemented for this query.");
    }

    /// <summary>Returns a view of features whose bounding box intersects the given bounds.</summary>
    IFeatureQuery In(IBounds bbox);

    /// <summary>Returns the first feature in the collection, or null if empty.</summary>
    IFeature? First() => FeaturesSupport.First(this);

    /// <summary>Returns the number of features in this collection.</summary>
    long Count() => FeaturesSupport.Count(this);

    /// <summary>Returns true if this collection contains no features.</summary>
    bool IsEmpty() => FeaturesSupport.IsEmpty(this);

    /// <summary>Creates a list containing all features in this collection.</summary>
    List<IFeature> ToList() => FeaturesSupport.ToList(this);

    /// <summary>Creates an array containing all features in this collection.</summary>
    IFeature[] ToArray() => FeaturesSupport.ToArray(this);

    /// <summary>Checks whether this collection contains the given object.</summary>
    bool Contains(object f) => FeaturesSupport.Contains(this, f);

    /// <summary>Returns a view filtered by the given <see cref="IFilter"/>.</summary>
    IFeatureQuery Select(IFilter filter);

    /// <summary>Returns the features present in both this collection and <paramref name="other"/>.</summary>
    IFeatureQuery Select(IFeatureQuery other);

    // --- Spatial-predicate filters (ported from com.geodesk.feature.Features default methods) ---

    /// <summary>Returns all features that have at least one common vertex with the given feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.connectedTo(Feature)</c>.</remarks>
    IFeatureQuery ConnectedTo(IFeature f) => FeaturesSupport.ConnectedTo(this, f);

    /// <summary>
    /// Returns all features that have at least one common vertex with the given Geometry.
    /// Coordinates of the Geometry are rounded to integers.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.connectedTo(Geometry)</c>.</remarks>
    IFeatureQuery ConnectedTo(Geometry geom) => FeaturesSupport.ConnectedTo(this, geom);

    /// <summary>Returns all features that contain the given Mercator-projected coordinate.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containingXY(int, int)</c>.</remarks>
    IFeatureQuery ContainingXY(int x, int y) => FeaturesSupport.ContainingXY(this, x, y);

    /// <summary>Returns all features that contain the given longitude/latitude coordinate.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containingLonLat(double, double)</c>.</remarks>
    IFeatureQuery ContainingLonLat(double lon, double lat) => FeaturesSupport.ContainingLonLat(this, lon, lat);

    /// <summary>Returns all features that contain the given feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containing(Feature)</c>.</remarks>
    IFeatureQuery Containing(IFeature feature) => FeaturesSupport.Containing(this, feature);

    /// <summary>Returns all features that contain the given Geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containing(Geometry)</c>.</remarks>
    IFeatureQuery Containing(Geometry geom) => FeaturesSupport.Containing(this, geom);

    /// <summary>Returns all features that contain the given PreparedGeometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.containing(PreparedGeometry)</c>.</remarks>
    IFeatureQuery Containing(IPreparedGeometry prepared) => FeaturesSupport.Containing(this, prepared);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.coveredBy(Feature)</c>.</remarks>
    IFeatureQuery CoveredBy(IFeature feature) => FeaturesSupport.CoveredBy(this, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.coveredBy(Geometry)</c>.</remarks>
    IFeatureQuery CoveredBy(Geometry geom) => FeaturesSupport.CoveredBy(this, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.coveredBy(PreparedGeometry)</c>.</remarks>
    IFeatureQuery CoveredBy(IPreparedGeometry prepared) => FeaturesSupport.CoveredBy(this, prepared);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.crossing(Feature)</c>.</remarks>
    IFeatureQuery Crossing(IFeature feature) => FeaturesSupport.Crossing(this, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.crossing(Geometry)</c>.</remarks>
    IFeatureQuery Crossing(Geometry geom) => FeaturesSupport.Crossing(this, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.crossing(PreparedGeometry)</c>.</remarks>
    IFeatureQuery Crossing(IPreparedGeometry prepared) => FeaturesSupport.Crossing(this, prepared);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.disjoint(Feature)</c>.</remarks>
    IFeatureQuery Disjoint(IFeature feature) => FeaturesSupport.Disjoint(this, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.disjoint(Geometry)</c>.</remarks>
    IFeatureQuery Disjoint(Geometry geom) => FeaturesSupport.Disjoint(this, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.disjoint(PreparedGeometry)</c>.</remarks>
    IFeatureQuery Disjoint(IPreparedGeometry prepared) => FeaturesSupport.Disjoint(this, prepared);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.intersecting(Feature)</c>.</remarks>
    IFeatureQuery Intersecting(IFeature feature) => FeaturesSupport.Intersecting(this, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.intersecting(Geometry)</c>.</remarks>
    IFeatureQuery Intersecting(Geometry geom) => FeaturesSupport.Intersecting(this, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.intersecting(PreparedGeometry)</c>.</remarks>
    IFeatureQuery Intersecting(IPreparedGeometry prepared) => FeaturesSupport.Intersecting(this, prepared);

    /// <summary>Returns all features whose closest point lies within a given radius (meters).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFromXY(double, int, int)</c>.</remarks>
    IFeatureQuery MaxMetersFromXY(double distance, int x, int y) => FeaturesSupport.MaxMetersFromXY(this, distance, x, y);

    /// <summary>Returns all features whose closest point lies within a given radius (meters).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFromLonLat(double, double, double)</c>.</remarks>
    IFeatureQuery MaxMetersFromLonLat(double distance, double lon, double lat) => FeaturesSupport.MaxMetersFromLonLat(this, distance, lon, lat);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFrom(double, Geometry)</c>.</remarks>
    IFeatureQuery MaxMetersFrom(double distance, Geometry geom) => FeaturesSupport.MaxMetersFrom(this, distance, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.maxMetersFrom(double, Feature)</c>.</remarks>
    IFeatureQuery MaxMetersFrom(double distance, IFeature feature) => FeaturesSupport.MaxMetersFrom(this, distance, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.overlapping(Feature)</c>.</remarks>
    IFeatureQuery Overlapping(IFeature feature) => FeaturesSupport.Overlapping(this, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.overlapping(Geometry)</c>.</remarks>
    IFeatureQuery Overlapping(Geometry geom) => FeaturesSupport.Overlapping(this, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.overlapping(PreparedGeometry)</c>.</remarks>
    IFeatureQuery Overlapping(IPreparedGeometry prepared) => FeaturesSupport.Overlapping(this, prepared);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.touching(Feature)</c>.</remarks>
    IFeatureQuery Touching(IFeature feature) => FeaturesSupport.Touching(this, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.touching(Geometry)</c>.</remarks>
    IFeatureQuery Touching(Geometry geom) => FeaturesSupport.Touching(this, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.touching(PreparedGeometry)</c>.</remarks>
    IFeatureQuery Touching(IPreparedGeometry prepared) => FeaturesSupport.Touching(this, prepared);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.within(Feature)</c>.</remarks>
    IFeatureQuery Within(IFeature feature) => FeaturesSupport.Within(this, feature);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.within(Geometry)</c>.</remarks>
    IFeatureQuery Within(Geometry geom) => FeaturesSupport.Within(this, geom);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.within(PreparedGeometry)</c>.</remarks>
    IFeatureQuery Within(IPreparedGeometry prepared) => FeaturesSupport.Within(this, prepared);

    /// <summary>Returns the node with the given ID (or null).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.node(long)</c>.</remarks>
    INode? GetNode(long id) => FeaturesSupport.GetNode(this, id);

    /// <summary>Returns the way with the given ID (or null).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.way(long)</c>.</remarks>
    IWay? GetWay(long id) => FeaturesSupport.GetWay(this, id);

    /// <summary>Returns the relation with the given ID (or null).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Features.relation(long)</c>.</remarks>
    IRelation? GetRelation(long id) => FeaturesSupport.GetRelation(this, id);

    /// <summary>Adds all features in this collection to the given collection.</summary>
    void AddTo(ICollection<IFeature> collection) => FeaturesSupport.AddTo(this, collection);

}
