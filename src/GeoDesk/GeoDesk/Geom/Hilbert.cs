/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

/*
 *
 * This class is based on flatbush (https://github.com/mourner/flatbush)
 * by Vladimir Agafonkin. The original work is licensed as follows:
 *
 * ISC License
 *
 * Copyright (c) 2018, Vladimir Agafonkin
 *
 * Permission to use, copy, modify, and/or distribute this software for any purpose
 * with or without fee is hereby granted, provided that the above copyright notice
 * and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
 * REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
 * INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
 * OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
 * TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
 * THIS SOFTWARE.
 *
 * (https://github.com/mourner/flatbush/blob/master/LICENSE)
 *
 * The original work is based on Fast Hilbert curve algorithm by http://threadlocalmutex.com,
 * ported from C++ https://github.com/rawrunprotected/hilbert_curves (public domain).
 */

using System.Diagnostics;
using System.Globalization;

namespace GeoDesk.Geom;

/// <remarks>Ported from Java <c>com.geodesk.geom.Hilbert</c>.</remarks>
public static class Hilbert
{

    /// <summary>
    /// Calculates the distance of a coordinate along the Hilbert Curve.
    ///
    /// The coordinate space is technically 0 &lt;= x/y &lt; 2^16, but this would require treating the
    /// signed result as an unsigned value. Since this is unintuitive and leads to needless
    /// frustrations, the maximum coordinate value should be 2^15-1. Technically, this means we use a
    /// single quadrant of a 16th-order Hilbert curve (i.e. a 15th-order Hilbert curve).
    /// </summary>
    /// <param name="x">must be 0 &lt;= x &lt; 2^15</param>
    /// <param name="y">must be 0 &lt;= y &lt; 2^15</param>
    /// <returns>the Hilbert Curve distance; (0 &lt;= distance &lt; 2^30)</returns>
    /// <remarks>Ported from Java <c>com.geodesk.geom.Hilbert.fromXY(int, int)</c>.</remarks>
    public static int FromXY(int x, int y)
    {
        Debug.Assert(x >= 0 && x < (1 << 15), string.Format(CultureInfo.InvariantCulture, "{0} is out of range", x));
        Debug.Assert(y >= 0 && y < (1 << 15), string.Format(CultureInfo.InvariantCulture, "{0} is out of range", y));

        var a = x ^ y;
        var b = 0xFFFF ^ a;
        var c = 0xFFFF ^ (x | y);
        var d = x & (y ^ 0xFFFF);

        var A = a | (b >> 1);
        var B = (a >> 1) ^ a;
        var C = ((c >> 1) ^ (b & (d >> 1))) ^ c;
        var D = ((a & (c >> 1)) ^ (d >> 1)) ^ d;

        a = A; b = B; c = C; d = D;
        A = ((a & (a >> 2)) ^ (b & (b >> 2)));
        B = ((a & (b >> 2)) ^ (b & ((a ^ b) >> 2)));
        C ^= ((a & (c >> 2)) ^ (b & (d >> 2)));
        D ^= ((b & (c >> 2)) ^ ((a ^ b) & (d >> 2)));

        a = A; b = B; c = C; d = D;
        A = ((a & (a >> 4)) ^ (b & (b >> 4)));
        B = ((a & (b >> 4)) ^ (b & ((a ^ b) >> 4)));
        C ^= ((a & (c >> 4)) ^ (b & (d >> 4)));
        D ^= ((b & (c >> 4)) ^ ((a ^ b) & (d >> 4)));

        a = A; b = B; c = C; d = D;
        C ^= ((a & (c >> 8)) ^ (b & (d >> 8)));
        D ^= ((b & (c >> 8)) ^ ((a ^ b) & (d >> 8)));

        a = C ^ (C >> 1);
        b = D ^ (D >> 1);

        var i0 = x ^ y;
        var i1 = b | (0xFFFF ^ (i0 | a));

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
