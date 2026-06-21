/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Geom;

namespace GeoDesk.Feature.Polygons;

/// <summary>
/// One ring of a (multi)polygon being assembled from way segments: a closed loop holding its segment
/// chain, coordinate count, bounding box, and the inner rings (holes) it encloses.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Ring</c>.</remarks>
internal class Ring
{

    internal readonly int number;       // TODO: remove? No!
    internal readonly Segment firstSegment;
    internal int coordinateCount;
    internal Box? bbox;
    internal Ring? firstInner;
    internal Ring? next;

    /// <summary>
    /// Creates a ring with the given number within its polygon, built from the given segment chain and
    /// coordinate count, and linked ahead of the given next ring.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Ring(int, Segment, int, Ring)</c>.</remarks>
    public Ring(int number, Segment firstSegment, int coordinateCount, Ring? next)
    {
        this.number = number;
        this.firstSegment = firstSegment;
        this.coordinateCount = coordinateCount;
        this.next = next;
    }

    /// <summary>
    /// Computes and caches this ring's bounding box by expanding over the bounds of all its segments.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Ring.calculateBounds()</c>.</remarks>
    public void CalculateBounds()
    {
        var b = new Box();
        for (var segment = firstSegment; segment != null; segment = segment.next)
        {
            b.ExpandToInclude(segment.way.Bounds); // TODO: use way itself as Bounds
        }
        bbox = b;
    }

    /// <summary>
    /// Checks whether the given point is a vertex in this Ring.
    /// </summary>
    /// <returns>true if x/y represent a vertex of this Ring</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Ring.containsVertex(int, int)</c>.</remarks>
    public bool ContainsVertex(int x, int y)
    {
        for (var segment = firstSegment; segment != null; segment = segment.next)
        {
            IBounds b = segment.way.Bounds;
            if (!b.Contains(x, y)) continue;
            // We could skip one of the ending coordinates of each segment, but since the segment
            // may be backwards, we'll just check all for the sake of simplicity
            if (XY.Contains(segment.coords, x, y)) return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether the given point (which must not be a vertex) lies within this Ring.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Ring.containsPoint(int, int)</c>.</remarks>
    bool ContainsPoint(int x, int y)
    {
        var odd = 0;
        for (var segment = firstSegment; segment != null; segment = segment.next)
        {
            IBounds b = segment.way.Bounds;
            if (y >= b.MinY && y <= b.MaxY) odd ^= XY.CastRay(segment.coords, x, y);
        }
        return odd != 0;
    }

    /// <summary>
    /// Checks whether this Ring contains another Ring, using a point-in-polygon test of a
    /// non-vertex point of the other Ring. This method assumes that the caller has already checked
    /// if this Ring's bounding box contains the other Ring's bounding box (a much cheaper test to
    /// rule out whether it is even possible for this Ring to contain the other).
    /// </summary>
    /// <param name="other">the potential inner Ring</param>
    /// <returns>true if this Ring contains the other</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Ring.contains(Ring)</c>.</remarks>
    public bool Contains(Ring other)
    {
        var otherCoords = other.firstSegment.coords;
        var x = otherCoords[0];
        var y = otherCoords[1];
        if (!ContainsVertex(x, y)) return ContainsPoint(x, y);

        // If another ring shares a vertex with this ring, this does not necessarily mean that it is
        // an inner ring. Further, the fast ray-casting point-in-polygon test does not work for
        // vertexes, anyway. That's why we want to perform the test with a non-vertex point.
        // Technically, we should scan all of the potential inner ring's points to find a non-vertex
        // point. However, since two shared vertexes in a row would mean that the geometry created
        // by the two rings is invalid (two rings may touch in one or more points, but may not have
        // a shared edge), we only check whether the first point is a non-vertex, and take the second
        // point even though it may be a vertex as well.

        x = otherCoords[2];
        y = otherCoords[3];
        return ContainsPoint(x, y);
    }

    /// <summary>
    /// Adds the given ring as an inner ring (hole) of this ring, prepending it to the inner-ring list.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.Ring.addInner(Ring)</c>.</remarks>
    public void AddInner(Ring inner)
    {
        inner.next = firstInner;
        firstInner = inner;
    }

}
