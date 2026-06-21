/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using GeoDesk.Feature.Query;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

namespace GeoDesk.Feature;

/// <summary>
/// A geographic feature read from a GeoDesk feature library — an OSM node, way, or relation,
/// together with its tags, geometry, and topological relationships.
/// <para>A feature is also an <see cref="IEnumerable{T}"/> of <see cref="IFeature"/> over its
/// members: the feature-nodes of a way or the members of a relation (a node yields nothing).</para>
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Feature</c>.</remarks>
public interface IFeature : IEnumerable<IFeature>
{

    /// <summary>
    /// The OSM identifier of this feature. IDs are unique only within a feature type, so the same
    /// numeric value may name a different node, way, and relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.id()</c>.</remarks>
    long Id { get; }

    /// <summary>
    /// The kind of feature this is: <see cref="FeatureType.Node"/>, <see cref="FeatureType.Way"/>,
    /// or <see cref="FeatureType.Relation"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.type()</c>.</remarks>
    FeatureType Type { get; }

    /// <summary>
    /// True if this feature is an OSM node (a single point). False for ways and relations.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isNode()</c>.</remarks>
    bool IsNode => false;

    /// <summary>
    /// True if this feature is an OSM way (an ordered sequence of nodes). False otherwise.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isWay()</c>.</remarks>
    bool IsWay => false;

    /// <summary>
    /// True if this feature is an OSM relation (a collection of member features). False otherwise.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isRelation()</c>.</remarks>
    bool IsRelation => false;

    /// <summary>
    /// The X coordinate of this feature in the library's Web Mercator projection. For a node this is
    /// its location; for a way or relation it is a representative anchor point.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.x()</c>.</remarks>
    int X { get; }

    /// <summary>
    /// The Y coordinate of this feature in the library's Web Mercator projection. For a node this is
    /// its location; for a way or relation it is a representative anchor point.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.y()</c>.</remarks>
    int Y { get; }

    /// <summary>
    /// The longitude of this feature in degrees (WGS-84), derived from <see cref="X"/> by inverse
    /// Mercator projection.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.lon()</c>.</remarks>
    double Lon => Mercator.LonFromX(X);

    /// <summary>
    /// The latitude of this feature in degrees (WGS-84), derived from <see cref="Y"/> by inverse
    /// Mercator projection.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.lat()</c>.</remarks>
    double Lat => Mercator.LatFromY(Y);

    /// <summary>
    /// The bounding box of this feature, in Web Mercator coordinates. For a node the box is the
    /// single point; for a way or relation it encloses all of its geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.bounds()</c>.</remarks>
    Box Bounds { get; }

    /// <summary>
    /// Returns this feature's coordinates as a flat integer array of Web Mercator values, with X at
    /// even indexes and Y at odd indexes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.toXY()</c>.</remarks>
    int[] ToXY();

    /// <summary>
    /// The tags (key/value pairs) attached to this feature, as a re-enumerable read-only collection.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.tags()</c>.</remarks>
    TagCollection Tags { get; }

    /// <summary>
    /// Returns the value of the tag with the given key as a string, or an empty string if the
    /// feature has no such tag.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.tag(String)</c>.</remarks>
    string Tag(string key);

    /// <summary>
    /// Checks whether this feature has a tag with the given key, regardless of its value.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.hasTag(String)</c>.</remarks>
    bool HasTag(string key);

    /// <summary>
    /// Checks whether this feature has a tag with the given key whose value equals the given value.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.hasTag(String, String)</c>.</remarks>
    bool HasTag(string key, string value);

    /// <summary>
    /// Checks whether this feature belongs to the given parent — i.e. it is a member of the given
    /// relation, or a node of the given way.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.belongsTo(Feature)</c>.</remarks>
    bool BelongsTo(IFeature parent);

    /// <summary>
    /// If this feature was obtained by enumerating the members of a relation, its role within that
    /// relation; otherwise <c>null</c>. An empty string denotes a member with no explicit role.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.role()</c>.</remarks>
    string? Role { get; }

    /// <summary>
    /// Returns the value of the tag with the given key as a string (an empty string if absent). This
    /// is the canonical string form of the tag value.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.stringValue(String)</c>.</remarks>
    string StringValue(string key);

    /// <summary>
    /// Returns the value of the tag with the given key parsed as a 32-bit integer, or 0 if the tag
    /// is absent or not numeric.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.intValue(String)</c>.</remarks>
    int IntValue(string key);

    /// <summary>
    /// Returns the value of the tag with the given key parsed as a 64-bit integer, or 0 if the tag
    /// is absent or not numeric.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.longValue(String)</c>.</remarks>
    long LongValue(string key);

    /// <summary>
    /// Returns the value of the tag with the given key parsed as a double, or 0 if the tag is absent
    /// or not numeric.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.doubleValue(String)</c>.</remarks>
    double DoubleValue(string key);

    /// <summary>
    /// Returns the value of the tag with the given key interpreted as a boolean (e.g. <c>yes</c>/
    /// <c>true</c> versus <c>no</c>/absent).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.booleanValue(String)</c>.</remarks>
    bool BooleanValue(string key);

    /// <summary>
    /// Checks whether this feature is a member of at least one relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.belongsToRelation()</c>.</remarks>
    bool BelongsToRelation { get; }

    /// <summary>
    /// Checks whether this feature represents an area (a closed, filled polygon) rather than a point
    /// or open line.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isArea()</c>.</remarks>
    bool IsArea { get; }

    /// <summary>
    /// The length of this feature in meters, measured along its geometry. Returns 0 for features
    /// that are not lineal (e.g. nodes and areas).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.length()</c>.</remarks>
    double Length => 0;

    /// <summary>
    /// The area of this feature in square meters. Returns 0 for features that are not polygonal.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.area()</c>.</remarks>
    double Area => 0;

    /// <summary>
    /// Builds a JTS (NetTopologySuite) <see cref="Geometry"/> for this feature: a <c>Point</c> for a
    /// node, a <c>LineString</c> or <c>Polygon</c> for a way, or a geometry collection for a relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.toGeometry()</c>.</remarks>
    Geometry ToGeometry();

    /// <summary>
    /// Returns this way's feature-nodes as a queryable collection. The default implementation returns
    /// an empty view; only ways override it with their actual nodes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.nodes()</c>.</remarks>
    IFeatureQuery Nodes()
    {
        return EmptyView.Any;
    }

    /// <summary>
    /// Returns this way's feature-nodes that match the given GOQL query. The default implementation
    /// returns an empty view; only ways override it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.nodes(String)</c>.</remarks>
    IFeatureQuery Nodes(string query)
    {
        return EmptyView.Any;
    }

    /// <summary>
    /// Returns the members of this relation as a queryable collection. The default implementation
    /// returns an empty view; only relations override it with their actual members.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.members()</c>.</remarks>
    IFeatureQuery Members()
    {
        return EmptyView.Any;
    }

    /// <summary>
    /// Returns the members of this relation that match the given GOQL query. The default
    /// implementation returns an empty view; only relations override it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.members(String)</c>.</remarks>
    IFeatureQuery Members(string query)
    {
        return EmptyView.Any;
    }

    /// <summary>
    /// Returns all parent features to which this feature belongs: the ways that contain it as a node
    /// and the relations that contain it as a member.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.parents()</c>.</remarks>
    IFeatureQuery Parents();

    /// <summary>
    /// Returns the parent ways and relations to which this feature belongs that match the given GOQL
    /// query.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.parents(String)</c>.</remarks>
    IFeatureQuery Parents(string query);

}
