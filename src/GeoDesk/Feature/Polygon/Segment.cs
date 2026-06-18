/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;

// PORT: the Java package is com.geodesk.feature.polygon, but a C# namespace named Polygon would
// collide with the NetTopologySuite Polygon type used throughout this package, so it is namespaced
// as Polygons (plural).
namespace GeoDesk.Feature.Polygons;

/// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Segment</c>.</remarks>
internal class Segment
{

    internal const int Unassigned = 0;
    internal const int Tentative = 1;
    internal const int Assigned = 2;
    internal const int Dangling = 3;

    internal readonly int number;
    internal readonly IWay way;
    internal readonly int[] coords;
    internal Segment? next;
    internal bool backward;
    internal byte status;

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Segment(int, Way, Segment)</c>.</remarks>
    internal Segment(int number, IWay way, Segment? next)
    {
        this.number = number;
        this.way = way;
        coords = way.ToXY();
        this.next = next;
    }

}
