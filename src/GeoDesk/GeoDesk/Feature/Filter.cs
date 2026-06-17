/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature.Match;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature;

/// <summary>
/// An interface for classes that select the features to be returned by a query.
/// </summary>
public interface Filter
{
    /// <summary>
    /// Returns zero or more bit flags (see <c>FilterStrategy</c>) that help the Query Engine
    /// optimize the performance of this Filter.
    /// </summary>
    int Strategy() => 0;

    /// <summary>
    /// Checks whether the given feature should be included in the query results.
    /// </summary>
    bool Accept(Feature feature)
    {
        return Accept(feature, feature.ToGeometry());
    }

    /// <summary>
    /// Checks whether the given feature should be included in the query results.
    /// </summary>
    bool Accept(Feature feature, Geometry geom)
    {
        return Accept(feature);
    }

    /// <summary>
    /// Returns the Filter that should be used for the given tile.
    /// </summary>
    Filter? FilterForTile(int tileNumber, Polygon tileGeometry) => this;

    /// <summary>
    /// The maximum bounding box in which acceptable candidates can be found.
    /// </summary>
    Bounds Bounds() => Box.OfWorld(); // TODO: use singleton

    int AcceptedTypes() => TypeBits.ALL;
}
