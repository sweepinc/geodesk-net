/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Polygons;

/// <remarks>Ported from Java <c>com.geodesk.feature.polygon.RingAssigner</c>.</remarks>
public class RingAssigner
{

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
