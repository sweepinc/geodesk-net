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
/// A geographic feature.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Feature</c>.</remarks>
public interface IFeature : IEnumerable<IFeature>
{

    /// <summary>Returns the OSM ID of the feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.id()</c>.</remarks>
    long Id { get; }

    /// <summary>Returns the feature's type (<c>Node</c>, <c>Way</c> or <c>Relation</c>).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.type()</c>.</remarks>
    FeatureType Type { get; }

    /// <summary>Checks if this Feature is an OSM node.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isNode()</c>.</remarks>
    bool IsNode => false;

    /// <summary>Checks if this Feature is an OSM way.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isWay()</c>.</remarks>
    bool IsWay => false;

    /// <summary>Checks if this Feature is an OSM relation.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isRelation()</c>.</remarks>
    bool IsRelation => false;

    /// <summary>Returns the X coordinate of this feature (Mercator projection).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.x()</c>.</remarks>
    int X { get; }

    /// <summary>Returns the Y coordinate of this feature (Mercator projection).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.y()</c>.</remarks>
    int Y { get; }

    /// <summary>Returns the longitude of this feature (degrees).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.lon()</c>.</remarks>
    double Lon => Mercator.LonFromX(X);

    /// <summary>Returns the latitude of this feature (degrees).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.lat()</c>.</remarks>
    double Lat => Mercator.LatFromY(Y);

    /// <summary>Retrieves the bounding box of the feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.bounds()</c>.</remarks>
    Box Bounds { get; }

    /// <summary>
    /// Returns the way's coordinates as an array of integers. X at even indexes, Y at odd.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.toXY()</c>.</remarks>
    int[] ToXY();

    /// <summary>Returns the tags of this feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.tags()</c>.</remarks>
    ITags Tags { get; }

    /// <summary>Returns the string value of the given key (or an empty string).</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.tag(String)</c>.</remarks>
    string Tag(string key);

    /// <summary>Checks whether this feature has a tag with the given key.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.hasTag(String)</c>.</remarks>
    bool HasTag(string key);

    /// <summary>Checks whether this feature has a tag with the given key and value.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.hasTag(String, String)</c>.</remarks>
    bool HasTag(string key, string value);

    /// <summary>
    /// Checks whether this feature is a member of the given Relation, or a node in the given Way.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.belongsTo(Feature)</c>.</remarks>
    bool BelongsTo(IFeature parent);

    /// <summary>
    /// If this Feature was returned by a call to members() of a Relation, returns this
    /// Feature's role in that Relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.role()</c>.</remarks>
    string? Role { get; }

    /// <summary>Returns the value of a tag as a string.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.stringValue(String)</c>.</remarks>
    string StringValue(string key);

    /// <summary>Returns the value of a tag as an int.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.intValue(String)</c>.</remarks>
    int IntValue(string key);

    /// <summary>Returns the value of a tag as a long.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.longValue(String)</c>.</remarks>
    long LongValue(string key);

    /// <summary>Returns the value of the given key as a double.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.doubleValue(String)</c>.</remarks>
    double DoubleValue(string key);

    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.booleanValue(String)</c>.</remarks>
    bool BooleanValue(string key);

    /// <summary>Checks whether this Feature is a member of a Relation.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.belongsToRelation()</c>.</remarks>
    bool BelongsToRelation { get; }

    /// <summary>Checks whether this Feature represents an area.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.isArea()</c>.</remarks>
    bool IsArea { get; }

    /// <summary>Measures the length of this feature (in meters), or 0 if not lineal.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.length()</c>.</remarks>
    double Length => 0;

    /// <summary>Measures the area of a feature (in square meters), or 0 if not polygonal.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.area()</c>.</remarks>
    double Area => 0;

    /// <summary>Creates a JTS Geometry object for this feature.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.toGeometry()</c>.</remarks>
    Geometry ToGeometry();

    /// <summary>Returns the way's nodes.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.nodes()</c>.</remarks>
    IFeatures Nodes()
    {
        return EmptyView.Any;
    }

    /// <summary>Returns the way's nodes that match the given query.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.nodes(String)</c>.</remarks>
    IFeatures Nodes(string query)
    {
        return EmptyView.Any;
    }

    /// <summary>Returns the members of this Relation.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.members()</c>.</remarks>
    IFeatures Members()
    {
        return EmptyView.Any;
    }

    /// <summary>Returns the members of this Relation that match the given query.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.members(String)</c>.</remarks>
    IFeatures Members(string query)
    {
        return EmptyView.Any;
    }

    /// <summary>Returns all ways and relations to which this Feature belongs.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.parents()</c>.</remarks>
    IFeatures Parents();

    /// <summary>
    /// Returns all ways and relations to which this Feature belongs that match the given query.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Feature.parents(String)</c>.</remarks>
    IFeatures Parents(string query);

}
