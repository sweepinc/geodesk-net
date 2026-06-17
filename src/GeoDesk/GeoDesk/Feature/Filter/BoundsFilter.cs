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
public class BoundsFilter : Filter
{
    private readonly Bounds bounds;

    public BoundsFilter(Bounds bounds)
    {
        this.bounds = bounds;
    }

    public bool Accept(Feature feature)
    {
        return bounds.Intersects(feature.Bounds());
    }

    // TODO: Geometry?
}
