/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// Base class for spatial filters that test a feature by materializing its full
/// geometry and delegating to a geometry-level predicate. The "slow" name reflects
/// that these filters construct geometry objects rather than operating on raw
/// coordinates.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowSpatialFilter</c>.</remarks>
internal abstract class SlowSpatialFilter : IFilter
{

    /// <summary>
    /// Tests the given materialized geometry against this filter's spatial predicate.
    /// Subclasses implement the specific relationship (intersects, within, etc.).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowSpatialFilter.acceptGeometry(Geometry)</c>.</remarks>
    protected abstract bool AcceptGeometry(Geometry geom);

    /// <summary>
    /// Converts the feature to its geometry and evaluates the spatial predicate
    /// against it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.SlowSpatialFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        return AcceptGeometry(feature.ToGeometry());
    }

}
