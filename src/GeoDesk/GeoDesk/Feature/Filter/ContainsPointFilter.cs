/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// Spatial predicate that accepts only features that contain a given point.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsPointFilter</c>.</remarks>
public class ContainsPointFilter : Filter
{

    readonly Bounds _bounds;
    readonly int _px;
    readonly int _py;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsPointFilter(int, int)</c>.</remarks>
    public ContainsPointFilter(int x, int y)
    {
        _px = x;
        _py = y;
        _bounds = Box.AtXY(x, y);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsPointFilter.bounds()</c>.</remarks>
    public Bounds Bounds()
    {
        return _bounds;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsPointFilter.accept(Feature)</c>.</remarks>
    public bool Accept(Feature feature)
    {
        // TODO: ways and nodes can also "contain" a point!
        if (!feature.IsArea()) return false; // TODO: should set as pre-filter
        if (feature is StoredWay way)
            return PointInPolygon.TestFast(way.IterXY(IFeatureFlags.AREA_FLAG), _px, _py) != 0;
        else if (feature is StoredRelation rel)
            return IsInsideRelation(rel);
        return _px == feature.X() && _py == feature.Y();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsPointFilter.isInsideRelation(StoredRelation)</c>.</remarks>
    bool IsInsideRelation(StoredRelation rel)
    {
        var crossings = 0;
        foreach (var member in rel.Members().Ways())
        {
            var role = member.Role();
            if (role != "outer" && role != "inner") continue;
            Box memberBox = member.Bounds();
            if (_py < memberBox.MinY || _py > memberBox.MaxY) continue;
            crossings ^= PointInPolygon.TestFast(((StoredWay)member).IterXY(0), _px, _py);
        }
        return crossings != 0;
    }

}
