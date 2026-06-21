/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Polygons;

/// <summary>
/// Assigns inner rings (holes) to their containing outer rings when assembling a
/// multipolygon from an OSM area relation. Determines, for each inner ring, which
/// outer ring encloses it so that the resulting polygon hierarchy is well formed.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingAssigner</c>.</remarks>
internal class RingAssigner
{

    /// <summary>
    /// Assigns a single inner ring to the smallest outer ring that contains it,
    /// falling back to the largest outer ring when no tighter container is found.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingAssigner.assignRing(Ring[], Ring)</c>.</remarks>
    static void AssignRing(Ring[] outerRings, Ring inner)
    {
        Ring? tentativeOuter = null;
        for (var i = outerRings.Length - 1; i > 0; i--)
        {
            var tryOuter = outerRings[i];
            if (tryOuter.bbox!.Contains(inner.bbox!))
            {
                if (tentativeOuter != null && tentativeOuter.Contains(inner))
                {
                    tentativeOuter.AddInner(inner);
                    return;
                }
                tentativeOuter = tryOuter;
            }
        }
        if (tentativeOuter != null && tentativeOuter.Contains(inner))
        {
            tentativeOuter.AddInner(inner);
            return;
        }
        outerRings[0].AddInner(inner);
    }

    /// <summary>
    /// Assigns every inner ring in a linked list of holes to its containing outer
    /// ring and returns the array of outer rings, each populated with the inner
    /// rings it encloses. The largest outer ring is placed at index zero and serves
    /// as the catch-all container.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingAssigner.assignRings(Ring, Ring)</c>.</remarks>
    public static Ring[] AssignRings(Ring firstOuter, Ring firstInner)
    {
        // count rings and determine the largest ring

        var outerCount = 1;
        var maxCoordinates = 0;
        Ring? biggestOuter = null;
        var outer = firstOuter;
        for (; ; )
        {
            if (outer.coordinateCount > maxCoordinates)
            {
                maxCoordinates = outer.coordinateCount;
                biggestOuter = outer;
            }
            outer = outer.next!;
            if (outer == null) break;
            outerCount++;
        }

        // Calculate bboxes of all rings, except for the largest

        var outerRings = new Ring[outerCount];
        outer = firstOuter;
        for (var i = outerCount - 1; ; )
        {
            if (outer != biggestOuter)
            {
                outer.CalculateBounds();
                outerRings[i] = outer;
                i--;
            }
            outer = outer.next!;
            if (outer == null) break;
        }
        outerRings[0] = biggestOuter!;

        var inner = firstInner;
        for (; ; )
        {
            inner.CalculateBounds();
            var next = inner.next;
                // assignRing may change next because it re-chains the rings
            AssignRing(outerRings, inner);
            if (next == null) break;
            inner = next;
        }
        return outerRings;
    }

}
