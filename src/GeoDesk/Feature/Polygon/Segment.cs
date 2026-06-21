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

/// <summary>
/// One way segment used while assembling polygon rings from a relation's member ways. Holds the
/// way, its coordinate sequence, a link to the next segment, a direction flag, and a ring-assignment
/// status (unassigned, tentative, assigned, or dangling).
/// </summary>
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

    /// <summary>
    /// Creates a segment for the given way (capturing its coordinates) and links it ahead of
    /// <paramref name="next"/> in the segment chain.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Segment(int, Way, Segment)</c>.</remarks>
    internal Segment(int number, IWay way, Segment? next)
    {
        this.number = number;
        this.way = way;
        coords = way.ToXY();
        this.next = next;
    }

}
