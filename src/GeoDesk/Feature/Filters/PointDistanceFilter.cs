/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using GeoDesk.Feature;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// Spatial predicate that accepts only features that are within a given distance from a point.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.PointDistanceFilter</c>.</remarks>
// TODO: check if we need to increase bbox size to account for distortion introduced by the
//  Mercator projection
internal class PointDistanceFilter : IFilter
{

    readonly IBounds _bounds;
    readonly int _px;
    readonly int _py;
    readonly double _distanceSquared;

    /// <summary>
    /// Creates a filter accepting features within the given distance (in meters) of the
    /// point at (x, y), converting the distance into projected units at that latitude.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.PointDistanceFilter(double, int, int)</c>.</remarks>
    public PointDistanceFilter(double distance, int x, int y)
    {
        _px = x;
        _py = y;
        var d = Mercator.DeltaFromMeters(distance, y);
        _bounds = Box.ImpsAroundXY((int)Math.Ceiling(d), x, y);
        _distanceSquared = d * d;
    }

    /// <summary>The bounding box enclosing the search radius.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.PointDistanceFilter.bounds()</c>.</remarks>
    public IBounds Bounds => _bounds;

    /// <summary>
    /// Returns true if any segment of the given way comes within the search distance of
    /// the point.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.PointDistanceFilter.segmentsWithinDistance(StoredWay, int)</c>.</remarks>
    bool SegmentsWithinDistance(StoredWay way, int areaFlag)
    {
        var iter = way.IterXY(areaFlag);
        var xy = iter.NextXY();
        double x1 = XY.X(xy);
        double y1 = XY.Y(xy);
        while (iter.HasNext())
        {
            xy = iter.NextXY();
            double x2 = XY.X(xy);
            double y2 = XY.Y(xy);
            if (PtSegDistSq(x1, y1, x2, y2, _px, _py) < _distanceSquared) return true;
            x1 = x2;
            y1 = y2;
        }
        return false;
    }

    /// <summary>
    /// Returns true if the given way is within the search distance: any segment close
    /// enough, or for areas, the point lying inside the polygon.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.PointDistanceFilter.isWithinDistance(StoredWay)</c>.</remarks>
    bool IsWithinDistance(StoredWay way)
    {
        if (way.IsArea)
        {
            if (SegmentsWithinDistance(way, FeatureFlags.AREA_FLAG)) return true;
            // The distance of a point that lies within a polygon is zero; we need to perform p-in-p
            // check because the edges themselves may be far away from the comparison point
            // TODO: check bbox first?
            return PointInPolygon.TestFast(way.IterXY(FeatureFlags.AREA_FLAG), _px, _py) != 0;
        }
        return SegmentsWithinDistance(way, 0);
    }

    /// <summary>
    /// Returns true if the feature is within the search distance of the point, handling
    /// ways, nodes, and area/non-area relations.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.PointDistanceFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        if (feature is IWay)
        {
            return IsWithinDistance((StoredWay)feature);
        }
        if (feature is INode)
        {
            return DistanceSq(feature.X, feature.Y, _px, _py) < _distanceSquared;
        }
        var rel = (IRelation)feature;
        if (rel.IsArea)
        {
            // measure distance to the ways that define shell and holes, and also perform point in
            // polygon test
            var odd = 0;
            foreach (var member in rel.Members().Ways())   // TODO: use role filter
            {
                var role = member.Role;
                if (role == "outer" || role == "inner")
                {
                    var way = (StoredWay)member;
                    var flags = way.Flags();
                    if (SegmentsWithinDistance(way, flags)) return true;
                    odd ^= PointInPolygon.TestFast(((StoredWay)member).IterXY(flags), _px, _py);
                }
            }
            return odd != 0;
        }
        else
        {
            foreach (var member in rel)
            {
                if (Accept(member)) return true;
            }
        }
        return false;
    }

    // PORT: java.awt.geom.Line2D.ptSegDistSq(x1,y1,x2,y2,px,py) — squared distance from a point to
    // a line segment (JDK algorithm).
    /// <summary>
    /// Returns the squared distance from the point (px, py) to the line segment from
    /// (x1, y1) to (x2, y2).
    /// </summary>
    static double PtSegDistSq(double x1, double y1, double x2, double y2, double px, double py)
    {
        x2 -= x1;
        y2 -= y1;
        px -= x1;
        py -= y1;
        var dotprod = px * x2 + py * y2;
        double projlenSq;
        if (dotprod <= 0.0)
        {
            projlenSq = 0.0;
        }
        else
        {
            px = x2 - px;
            py = y2 - py;
            dotprod = px * x2 + py * y2;
            if (dotprod <= 0.0)
                projlenSq = 0.0;
            else
                projlenSq = dotprod * dotprod / (x2 * x2 + y2 * y2);
        }
        var lenSq = px * px + py * py - projlenSq;
        if (lenSq < 0) lenSq = 0;
        return lenSq;
    }

    // PORT: java.awt.geom.Point2D.distanceSq(x1,y1,x2,y2) — squared distance between two points.
    /// <summary>Returns the squared distance between the points (x1, y1) and (x2, y2).</summary>
    static double DistanceSq(double x1, double y1, double x2, double y2)
    {
        x1 -= x2;
        y1 -= y2;
        return x1 * x1 + y1 * y1;
    }

}
