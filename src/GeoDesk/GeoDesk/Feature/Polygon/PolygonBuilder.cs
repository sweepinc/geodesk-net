/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Clarisma.Common.Store;
using GeoDesk.Feature.Store;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Polygons;

/// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder</c>.</remarks>
public class PolygonBuilder
{

    /// <summary>
    /// Creates an int array with X/Y coordinate pairs that represent the given Ring. The Ring must
    /// consist of properly ordered Segments which are closed.
    /// </summary>
    /// <param name="ring">the Ring for which to create the coordinate array</param>
    /// <returns>coordinates representing the linear ring</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.getRingCoordinates(Ring)</c>.</remarks>
    static int[] GetRingCoordinates(Ring ring)
    {
        var coords = new int[ring.coordinateCount];
        var segment = ring.firstSegment;
        var segmentCoords = segment.coords;
        var i = segment.backward ? segmentCoords.Length - 2 : 0;
        coords[0] = segmentCoords[i];
        coords[1] = segmentCoords[i + 1];
        var pos = 2;
        while (true)
        {
            segmentCoords = segment.coords;
            var segmentCoordinateCount = segmentCoords.Length;
            if (segment.backward)
            {
                for (i = segmentCoordinateCount - 4; i >= 0; i -= 2)
                {
                    coords[pos++] = segmentCoords[i];
                    coords[pos++] = segmentCoords[i + 1];
                }
            }
            else
            {
                Array.Copy(segmentCoords, 2, coords, pos, segmentCoordinateCount - 2);
                pos += segmentCoordinateCount - 2;
            }
            var next = segment.next;
            if (next == null) break;
            segment = next;
        }
        System.Diagnostics.Debug.Assert(pos == coords.Length);
        return coords;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.createLinearRing(GeometryFactory, Ring)</c>.</remarks>
    static LinearRing CreateLinearRing(GeometryFactory factory, Ring ring)
    {
        return factory.CreateLinearRing(new WayCoordinateSequence(GetRingCoordinates(ring)));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.overlapsFollowing(Ring)</c>.</remarks>
    static bool OverlapsFollowing(Ring inner)
    {
        var other = inner.next;
        while (other != null)
        {
            if (inner.bbox!.Intersects(other.bbox!)) return true;
            other = other.next;
        }
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.ringsOverlap(Ring)</c>.</remarks>
    static bool RingsOverlap(Ring firstInner)
    {
        for (var inner = firstInner; inner != null; inner = inner.next)
        {
            if (OverlapsFollowing(inner)) return true;
        }
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.createHoles(GeometryFactory, Ring)</c>.</remarks>
    static LinearRing[]? CreateHoles(GeometryFactory factory, Ring outer)
    {
        var inner = outer.firstInner;
        if (inner == null) return null;

        LinearRing[] holes;
        var innerCount = 0;
        for (; inner != null; inner = inner.next)
        {
            if (inner.bbox == null) inner.CalculateBounds();
            innerCount++;
        }
        if (innerCount > 4 || RingsOverlap(outer.firstInner!))
        {
            // TODO: no need to merge rings that don't overlap; create a list, add
            var holePolygons = new Polygon[innerCount];
            var i = 0;
            for (inner = outer.firstInner; inner != null; inner = inner.next)
            {
                holePolygons[i++] = factory.CreatePolygon(CreateLinearRing(factory, inner));
            }
            Geometry g = factory.CreateGeometryCollection(holePolygons);
            g = g.Buffer(0);
            var mergedCount = g.NumGeometries;
            holes = new LinearRing[mergedCount];
            for (i = 0; i < mergedCount; i++)
            {
                holes[i] = (LinearRing)((Polygon)g.GetGeometryN(i)).ExteriorRing;
            }
        }
        else
        {
            holes = new LinearRing[innerCount];
            var i = 0;
            for (inner = outer.firstInner; inner != null; inner = inner.next)
            {
                holes[i++] = CreateLinearRing(factory, inner);
            }
        }
        return holes;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.createPolygon(GeometryFactory, Ring)</c>.</remarks>
    static Polygon CreatePolygon(GeometryFactory factory, Ring outer)
    {
        return factory.CreatePolygon(CreateLinearRing(factory, outer), CreateHoles(factory, outer));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.createPolygonals(GeometryFactory, Ring)</c>.</remarks>
    static Geometry CreatePolygonals(GeometryFactory factory, Ring rings)
    {
        if (rings.number == 1) return CreatePolygon(factory, rings);
        var polygons = new Polygon[rings.number];
        var i = 0;
        var current = rings;
        while (true)
        {
            polygons[i++] = CreatePolygon(factory, current);
            var next = current.next;
            if (next == null) break;
            current = next;
        }
        System.Diagnostics.Debug.Assert(i == polygons.Length);
        return factory.CreateMultiPolygon(polygons);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.PolygonBuilder.build(GeometryFactory, Relation)</c>.</remarks>
    public static Geometry Build(GeometryFactory factory, Relation rel)
    {
        var outerSegmentCount = 0;
        var innerSegmentCount = 0;
        Segment? outerSegments = null;
        Segment? innerSegments = null;

        // TODO: use proper member filtering

        // segments are ordered in reverse

        try
        {
            foreach (var member in rel)
            {
                if (member is Way way)
                {
                    if (way.Role() == "outer")
                        outerSegments = new Segment(++outerSegmentCount, way, outerSegments);
                    else if (way.Role() == "inner")
                        innerSegments = new Segment(++innerSegmentCount, way, innerSegments);
                }
            }
        }
        catch (StoreException)
        {
            // TODO: we should distinguish between
            //  - can't load tile because no repository specified
            //  - problems with the repository (connection lost, bad tile file, etc.)
            // suppress exception and continue, try to assemble polygon from the ways we've been
            // able to fetch
        }

        if (outerSegments == null) return factory.CreatePolygon();
        Ring? outerRings = null; // RingBuilder.buildFast(outerSegments);
        if (outerRings == null) // TODO: useless check (Used for fast-path in the past)
        {
            outerRings = RingBuilder.Build(outerSegments);
            if (outerRings == null) return factory.CreatePolygon();
        }
        if (innerSegments != null)
        {
            Ring? innerRings = null; // RingBuilder.buildFast(innerSegments);
            if (innerRings == null) // TODO: useless check (Used for fast-path in the past)
            {
                innerRings = RingBuilder.Build(innerSegments);
            }
            if (innerRings != null)
            {
                if (outerRings.next == null)
                    outerRings.firstInner = innerRings;
                else
                    RingAssigner.AssignRings(outerRings, innerRings);
            }
        }

        return CreatePolygonals(factory, outerRings);
    }

}
