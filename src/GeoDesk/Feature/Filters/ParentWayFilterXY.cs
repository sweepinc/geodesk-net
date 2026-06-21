/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Feature.Store;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A filter that accepts a way only if one of its nodes lies at a specific encoded
/// coordinate. Used to find the parent ways that pass through a given node location.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.ParentWayFilterXY</c>.</remarks>
internal class ParentWayFilterXY : IFilter
{

    readonly long _xy;

    /// <summary>
    /// Creates a filter that matches ways containing a node at the given encoded
    /// coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ParentWayFilterXY(long)</c>.</remarks>
    public ParentWayFilterXY(long xy)
    {
        _xy = xy;
    }

    /// <summary>
    /// Returns true if the given feature is a way that has a node at the target
    /// encoded coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ParentWayFilterXY.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        var way = (StoredWay)feature;
        var iter = way.IterXY(0);
            // pass 0 as the area flag, because we don't want the start node returned twice
            // TODO: make sure iterator does not depend on other flags
        while (iter.HasNext())
        {
            if (iter.NextXY() == _xy) return true;
        }
        return false;
    }

}
