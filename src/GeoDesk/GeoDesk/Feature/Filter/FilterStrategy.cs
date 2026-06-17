/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Filters;

public class FilterStrategy
{
    /// <summary>
    /// Filter uses spatial index. If set, <c>Filter</c> must implement <c>Bounds()</c>.
    /// </summary>
    public const int USES_BBOX = 1;

    /// <summary>
    /// Features accepted by the Filter must be fully contained within the
    /// bounding box (USES_BBOX must be set as well).
    /// </summary>
    public const int STRICT_BBOX = 2;

    /// <summary>
    /// Given a specific tile, the Filter is able to accept all features within this
    /// tile, reject the tile entirely, or at least offer a simplified filter.
    /// If set, <c>Filter</c> must implement <c>FilterForTile()</c>.
    /// </summary>
    public const int FAST_TILE_FILTER = 4;

    /// <summary>
    /// Indicates that this Filter expects the Geometry of the feature passed to
    /// <c>Accept()</c>. If not set, the Geometry argument may be null.
    /// </summary>
    public const int NEEDS_GEOMETRY = 8;

    /// <summary>
    /// The filter accepts only a subset of types. If set, <c>Filter</c> must
    /// implement <c>AcceptedTypes()</c>.
    /// </summary>
    public const int RESTRICTS_TYPES = 16;
}
