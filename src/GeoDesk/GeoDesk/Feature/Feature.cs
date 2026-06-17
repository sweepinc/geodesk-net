/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using GeoDesk.Feature.Query;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature;

/// <summary>
/// A geographic feature.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Feature</c>.</remarks>
public interface Feature : IEnumerable<Feature>
{
    /// <summary>Returns the OSM ID of the feature.</summary>
    long Id();

    /// <summary>Returns the feature's type (<c>Node</c>, <c>Way</c> or <c>Relation</c>).</summary>
    FeatureType Type();

    /// <summary>Checks if this Feature is an OSM node.</summary>
    bool IsNode() => false;

    /// <summary>Checks if this Feature is an OSM way.</summary>
    bool IsWay() => false;

    /// <summary>Checks if this Feature is an OSM relation.</summary>
    bool IsRelation() => false;

    /// <summary>Returns the X coordinate of this feature (Mercator projection).</summary>
    int X();

    /// <summary>Returns the Y coordinate of this feature (Mercator projection).</summary>
    int Y();

    /// <summary>Returns the longitude of this feature (degrees).</summary>
    double Lon() => Mercator.LonFromX(X());

    /// <summary>Returns the latitude of this feature (degrees).</summary>
    double Lat() => Mercator.LatFromY(Y());

    /// <summary>Retrieves the bounding box of the feature.</summary>
    Box Bounds();

    /// <summary>
    /// Returns the way's coordinates as an array of integers. X at even indexes, Y at odd.
    /// </summary>
    int[] ToXY();

    /// <summary>Returns the tags of this feature.</summary>
    Tags Tags();

    /// <summary>Returns the string value of the given key (or an empty string).</summary>
    string Tag(string key);

    /// <summary>Checks whether this feature has a tag with the given key.</summary>
    bool HasTag(string key);

    /// <summary>Checks whether this feature has a tag with the given key and value.</summary>
    bool HasTag(string key, string value);

    /// <summary>
    /// Checks whether this feature is a member of the given Relation, or a node in the given Way.
    /// </summary>
    bool BelongsTo(Feature parent);

    /// <summary>
    /// If this Feature was returned by a call to members() of a Relation, returns this
    /// Feature's role in that Relation.
    /// </summary>
    string? Role();

    /// <summary>Returns the value of a tag as a string.</summary>
    string StringValue(string key);

    /// <summary>Returns the value of a tag as an int.</summary>
    int IntValue(string key);

    /// <summary>Returns the value of a tag as a long.</summary>
    long LongValue(string key);

    /// <summary>Returns the value of the given key as a double.</summary>
    double DoubleValue(string key);

    bool BooleanValue(string key);

    /// <summary>Checks whether this Feature is a member of a Relation.</summary>
    bool BelongsToRelation();

    /// <summary>Checks whether this Feature represents an area.</summary>
    bool IsArea();

    /// <summary>Measures the length of this feature (in meters), or 0 if not lineal.</summary>
    double Length() => 0;

    /// <summary>Measures the area of a feature (in square meters), or 0 if not polygonal.</summary>
    double Area() => 0;

    /// <summary>Creates a JTS Geometry object for this feature.</summary>
    Geometry ToGeometry();

    /// <summary>Returns the way's nodes.</summary>
    Features Nodes()
    {
        return EmptyView.Any;
    }

    /// <summary>Returns the way's nodes that match the given query.</summary>
    Features Nodes(string query)
    {
        return EmptyView.Any;
    }

    /// <summary>Returns the members of this Relation.</summary>
    Features Members()
    {
        return EmptyView.Any;
    }

    /// <summary>Returns the members of this Relation that match the given query.</summary>
    Features Members(string query)
    {
        return EmptyView.Any;
    }

    /// <summary>Returns all ways and relations to which this Feature belongs.</summary>
    Features Parents();

    /// <summary>
    /// Returns all ways and relations to which this Feature belongs that match the given query.
    /// </summary>
    Features Parents(string query);
}
