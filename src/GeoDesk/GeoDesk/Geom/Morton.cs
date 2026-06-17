/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Diagnostics;

namespace GeoDesk.Geom;

/// <remarks>Ported from Java <c>com.geodesk.geom.Morton</c>.</remarks>
public static class Morton
{

    /// <remarks>Ported from Java <c>com.geodesk.geom.Morton.mortonFromXY(int, int)</c>.</remarks>
    public static int MortonFromXY(int x, int y)
    {
        Debug.Assert((x & 0xffff_0000) == 0);
        Debug.Assert((y & 0xffff_0000) == 0);

        var i0 = x;
        var i1 = y;

        i0 = (i0 | (i0 << 8)) & 0x00FF00FF;
        i0 = (i0 | (i0 << 4)) & 0x0F0F0F0F;
        i0 = (i0 | (i0 << 2)) & 0x33333333;
        i0 = (i0 | (i0 << 1)) & 0x55555555;

        i1 = (i1 | (i1 << 8)) & 0x00FF00FF;
        i1 = (i1 | (i1 << 4)) & 0x0F0F0F0F;
        i1 = (i1 | (i1 << 2)) & 0x33333333;
        i1 = (i1 | (i1 << 1)) & 0x55555555;

        return (i1 << 1) | i0;
    }

}
