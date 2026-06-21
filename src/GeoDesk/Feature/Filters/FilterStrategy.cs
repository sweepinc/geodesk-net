/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Filters;

/// <summary>
/// Bit-flag constants describing how a filter behaves, letting the query engine
/// optimize: whether it uses a bounding box (and whether strictly), needs geometry,
/// restricts feature types, or supports per-tile acceleration.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.FilterStrategy</c>.</remarks>
internal class FilterStrategy
{

    /// <summary>
    /// Filter uses spatial index. If set, <c>Filter</c> must implement <c>Bounds()</c>.
    /// </summary>
    public const int UsesBbox = 1;

    /// <summary>
    /// Features accepted by the Filter must be fully contained within the bounding box (UsesBbox
    /// must be set as well).
    /// </summary>
    public const int StrictBbox = 2;

    /// <summary>
    /// Given a specific tile, the Filter is able to accept all features within this tile, reject
    /// the tile entirely, or at least offer a simplified filter. If set, <c>Filter</c> must
    /// implement <c>FilterForTile()</c>.
    /// </summary>
    public const int FastTileFilter = 4;

    /// <summary>
    /// A flag to indicate that this Filter expects the Geometry of the feature passed to
    /// <c>Accept()</c>. If this flag is not set, the <c>Geometry</c> argument may be null (in which
    /// case the Filter has to obtain the feature's geometry explicitly).
    /// </summary>
    public const int NeedsGeometry = 8;

    /// <summary>
    /// The filter accepts only a subset of types (e.g. only areas, only relation members). If set,
    /// <c>Filter</c> must implement <c>AcceptedTypes()</c>.
    /// </summary>
    public const int RestrictsTypes = 16;

}
