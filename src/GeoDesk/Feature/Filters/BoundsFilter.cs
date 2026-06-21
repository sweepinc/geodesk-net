/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A Filter that accepts only features that intersect the specified bounds.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.BoundsFilter</c>.</remarks>
internal class BoundsFilter : IFilter
{

    readonly IBounds _bounds;

    /// <summary>Creates a filter that accepts features intersecting the given bounds.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.BoundsFilter(Bounds)</c>.</remarks>
    public BoundsFilter(IBounds bounds)
    {
        _bounds = bounds;
    }

    /// <summary>Returns true if the feature's bounding box intersects the filter's bounds.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.BoundsFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        return _bounds.Intersects(feature.Bounds);
    }

    // TODO: Geometry?

}
