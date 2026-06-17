/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Feature.Store;

namespace GeoDesk.Feature.Filters;

/// <remarks>Ported from Java <c>com.geodesk.feature.filter.ParentWayFilterXY</c>.</remarks>
public class ParentWayFilterXY : Filter
{

    readonly long _xy;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ParentWayFilterXY(long)</c>.</remarks>
    public ParentWayFilterXY(long xy)
    {
        _xy = xy;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ParentWayFilterXY.accept(Feature)</c>.</remarks>
    public bool Accept(Feature feature)
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
