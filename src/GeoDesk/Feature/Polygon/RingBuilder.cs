/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Numerics;

using GeoDesk.Geom;

namespace GeoDesk.Feature.Polygons;

/// <summary>
/// Assembles way segments into closed rings by matching shared endpoints (via an endpoint hash table),
/// producing the rings from which polygon and multipolygon geometry is built.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder</c>.</remarks>
internal class RingBuilder
{

    /// <summary>
    /// Turns a series of Segments into a series of Rings, which works only if the Segments are
    /// ordered such that neighboring segments connect and form valid linear rings.
    /// </summary>
    /// <param name="segment">one or more Segments</param>
    /// <returns>one or more Rings, or null if the segments are not ordered properly</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.buildFast(Segment)</c>.</remarks>
    public static Ring? BuildFast(Segment? segment)
    {
        var ringCount = 0;
        Ring? rings = null;
        Ring? currentRing = null;
        var firstX = 0;
        var firstY = 0;
        var lastX = 0;
        var lastY = 0;
        while (segment != null)
        {
            var coords = segment.coords;
            var segmentCoordinateCount = coords.Length;
            var segmentFirstX = coords[0];
            var segmentFirstY = coords[1];
            var segmentLastX = coords[segmentCoordinateCount - 2];
            var segmentLastY = coords[segmentCoordinateCount - 1];
            if (currentRing == null)
            {
                rings = new Ring(++ringCount, segment, segmentCoordinateCount, rings);
                currentRing = rings;
                firstX = segmentFirstX;
                firstY = segmentFirstY;
            }
            else
            {
                currentRing.coordinateCount += segmentCoordinateCount - 2;
                if (segmentLastX == lastX && segmentLastY == lastY)
                {
                    segment.backward = true;
                    segmentLastX = segmentFirstX;
                    segmentLastY = segmentFirstY;
                }
                else if (segmentFirstX != lastX || segmentFirstY != lastY)
                {
                    break;
                }
            }
            var nextSegment = segment.next;
            if (segmentLastX == firstX && segmentLastY == firstY)
            {
                // Any ring with less than 4 points is defective
                if (currentRing.coordinateCount < 8) break;
                segment.next = null;
                currentRing = null;
            }
            else
            {
                lastX = segmentLastX;
                lastY = segmentLastY;
            }
            segment = nextSegment;
        }
        if (currentRing == null) return rings;
        RelinkSegments(rings!);
        return null;
    }

    /// <summary>
    /// Re-links the segments of a completed ring into a properly ordered, closed chain.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.relinkSegments(Ring)</c>.</remarks>
    static void RelinkSegments(Ring ring)
    {
        var firstSegment = ring.firstSegment;
        var r = ring.next;
        while (r != null)
        {
            var segment = r.firstSegment;
            for (; ; )
            {
                if (segment.next == null)
                {
                    segment.next = firstSegment;
                    firstSegment = r.firstSegment;
                    break;
                }
                segment = segment.next;
            }
            r = r.next;
        }
    }

    /// <summary>
    /// Finds the first segment which: has not been assigned; is not the current segment; and whose
    /// start or end point matches the given coordinates, which are the start point of the current
    /// segment.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.findNeighbor(Candidate[], Segment)</c>.</remarks>
    static Segment? FindNeighbor(Candidate?[] table, Segment current)
    {
        var coords = current.coords;
        var x = coords[current.backward ? coords.Length - 2 : 0];
        var y = coords[current.backward ? coords.Length - 1 : 1];
        var c = table[(x ^ y) & (table.Length - 1)];
        while (c != null)
        {
            var segment = c.segment;
            if (segment.status < Segment.Assigned && segment != current)
            {
                coords = segment.coords;
                if (coords[0] == x && coords[1] == y)
                {
                    segment.backward = true;
                    return segment;
                }
                if (coords[coords.Length - 2] == x && coords[coords.Length - 1] == y)
                {
                    segment.backward = false;
                    return segment;
                }
            }
            c = c.next;
        }
        return null;
    }

    /// <summary>
    /// Marks every segment of a ring (starting at the given one) as assigned and returns the ring's
    /// total coordinate count.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.markAndCount(Segment)</c>.</remarks>
    static int MarkAndCount(Segment segment)
    {
        var coordinateCount = segment.coords.Length;
        for (; ; )
        {
            segment.status = Segment.Assigned;
            segment = segment.next!;
            if (segment == null) return coordinateCount;
            coordinateCount += segment.coords.Length - 2;
        }
    }

    /// <summary>
    /// Adds a segment to the endpoint hash table under the slot derived from the given x/y endpoint,
    /// chaining it ahead of any candidate already in that slot.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.addToTable(Candidate[], Segment, int, int)</c>.</remarks>
    static void AddToTable(Candidate?[] table, Segment segment, int x, int y)
    {
        var slot = (x ^ y) & (table.Length - 1);
        table[slot] = new Candidate(segment, table[slot]);
    }

    /// <summary>
    /// Attempts to assemble closed Rings from a linked list of Segments, which can be in any order.
    /// Segments are marked ASSIGNED or DANGLING.
    /// </summary>
    /// <param name="firstSegment">first Segment in a linked list</param>
    /// <returns>a linked list of Ring objects, or null if none were found</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.build(Segment)</c>.</remarks>
    public static Ring? Build(Segment firstSegment)
    {
        if (firstSegment.number == 1)
        {
            var coords1 = firstSegment.coords;
            if (XY.IsClosed(coords1))
            {
                firstSegment.status = Segment.Assigned;
                return new Ring(1, firstSegment, coords1.Length, null);
            }
            firstSegment.status = Segment.Dangling;
            return null;
        }
        var segments = new Segment[firstSegment.number];
        var tableSize = (int)(uint.MaxValue >> BitOperations.LeadingZeroCount((uint)(segments.Length - 1))) + 1;
        var table = new Candidate?[tableSize];
        System.Diagnostics.Debug.Assert(tableSize > 0, "Bad tableSize for " + segments.Length + " segments");
        var i = 0;
        var segment = firstSegment;
        while (segment != null)
        {
            segments[i++] = segment;
            var coords = segment.coords;
            AddToTable(table, segment, coords[0], coords[1]);
            AddToTable(table, segment, coords[coords.Length - 2], coords[coords.Length - 1]);
            segment = segment.next!;
        }
        System.Diagnostics.Debug.Assert(i == segments.Length);

        var ringCount = 0;
        Ring? rings = null;

        for (i = 0; i < segments.Length; i++)
        {
            segment = segments[i];
            if (segment.status != Segment.Unassigned) continue;
            segment.backward = false;
            segment.next = null;
            var coords = segment.coords;
            if (XY.IsClosed(coords))
            {
                segment.status = Segment.Assigned;
                rings = new Ring(++ringCount, segment, coords.Length, rings);
                continue;
            }
            segment.status = Segment.Tentative;
            for (; ; )
            {
                var candidate = FindNeighbor(table, segment);
                if (candidate == null)
                {
                    segment.status = Segment.Dangling;
                    segment = segment.next!;
                }
                else if (candidate.status == Segment.Tentative)
                {
                    var nextSegment = candidate.next;
                    candidate.next = null;
                    rings = new Ring(++ringCount, segment, MarkAndCount(segment), rings);
                    segment = nextSegment!;
                }
                else if (XY.IsClosed(candidate.coords))
                {
                    candidate.status = Segment.Assigned;
                    candidate.next = null;
                    rings = new Ring(++ringCount, candidate, candidate.coords.Length, rings);
                    continue;
                }
                else
                {
                    candidate.status = Segment.Tentative;
                    candidate.next = segment;
                    segment = candidate;
                    continue;
                }
                if (segment == null) break;
            }
        }
        return rings;
    }

    /// <summary>
    /// A hash-table entry that links a segment to others sharing the same endpoint, used to find the
    /// segment that continues a ring.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.Candidate</c>.</remarks>
    class Candidate
    {

        internal readonly Segment segment;
        internal readonly Candidate? next;

        /// <summary>
        /// Creates a candidate wrapping the given segment, chained ahead of the given next candidate.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingBuilder.Candidate(Segment, Candidate)</c>.</remarks>
        internal Candidate(Segment segment, Candidate? next)
        {
            this.segment = segment;
            this.next = next;
        }

    }

}
