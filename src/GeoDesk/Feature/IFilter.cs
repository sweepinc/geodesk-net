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
/// <remarks>Ported from Java <c>com.geodesk.feature.Filter</c>.</remarks>
public interface IFilter
{

    /// <summary>
    /// Returns zero or more bit flags specified in <c>FilterStrategy</c> that help the Query Engine
    /// optimize the performance of this Filter.
    /// </summary>
    /// <returns>a bit set of strategy flags</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Filter.strategy()</c>.</remarks>
    int Strategy => 0;

    /// <summary>
    /// Checks whether the given feature should be included in the query results.
    /// </summary>
    /// <param name="feature">the feature to check</param>
    /// <returns><c>true</c> if this feature should be included in the results</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Filter.accept(Feature)</c>.</remarks>
    bool Accept(IFeature feature)
    {
        return Accept(feature, feature.ToGeometry());
    }

    /// <summary>
    /// Checks whether the given feature should be included in the query results. If <c>Strategy()</c>
    /// includes <c>NEEDS_GEOMETRY</c>, <paramref name="geom"/> must not be null.
    /// </summary>
    /// <param name="feature">the feature to check</param>
    /// <param name="geom">the feature's geometry</param>
    /// <returns><c>true</c> if this feature should be included in the results</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Filter.accept(Feature, Geometry)</c>.</remarks>
    bool Accept(IFeature feature, Geometry geom)
    {
        return Accept(feature);
    }

    /// <summary>
    /// Returns the Filter that should be used for the given tile. This allows a Filter to accept all
    /// features within a certain tile, reject a tile entirely, or substitute itself with a cheaper
    /// filter. This method will only be called if <c>Strategy()</c> includes <c>FAST_TILE_FILTER</c>.
    /// To signal that all features should be accepted, this method returns <c>null</c>; to reject the
    /// tile entirely, it must return <c>FalseFilter.Instance</c>. It can return <c>this</c> to indicate
    /// that no shortcut filter is available for the given tile.
    /// </summary>
    /// <param name="tileNumber">the tile number</param>
    /// <param name="tileGeometry">the tile polygon (an axis-aligned square)</param>
    /// <returns>the filter to use for this tile</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Filter.filterForTile(int, Polygon)</c>.</remarks>
    IFilter? FilterForTile(int tileNumber, Polygon tileGeometry) => this;

    /// <summary>
    /// The maximum bounding box in which acceptable candidates can be found.
    /// </summary>
    /// <returns>a bounding box, or null if the filter does not use the spatial index</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.Filter.bounds()</c>.</remarks>
    IBounds Bounds => Box.OfWorld(); // TODO: use singleton

    /// <remarks>Ported from Java <c>com.geodesk.feature.Filter.acceptedTypes()</c>.</remarks>
    int AcceptedTypes => TypeBits.ALL;

}
