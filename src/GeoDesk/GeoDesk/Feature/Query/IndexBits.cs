/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Numerics;
using System.Text;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.IndexBits</c>.</remarks>
public static class IndexBits
{

    /// <summary>
    /// Turns a category into a value that can be matched against a key-index bitset.
    /// </summary>
    /// <param name="category">the index category (1-based; range 1 to 30)</param>
    /// <returns>an int with a single bit set to 1</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.IndexBits.fromCategory(int)</c>.</remarks>
    public static int FromCategory(int category)
    {
        return category == 0 ? 0 : (1 << (category - 1));
    }

    // Note: category starts with 1, but categories[] is 0-based
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.IndexBits.toString(int, String[], String, String)</c>.</remarks>
    public static string ToString(int bits, string[] categories, string separator, string uncategorized)
    {
        if (bits == 0) return uncategorized;
        var buf = new StringBuilder();
        var cat = -1;
        while (bits != 0)
        {
            var zeroes = BitOperations.TrailingZeroCount((uint)bits);
            cat += zeroes + 1;
            if (buf.Length > 0) buf.Append(separator);
            buf.Append(categories[cat]);
            bits = (int)((uint)bits >> (zeroes + 1));
        }
        return buf.ToString();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.IndexBits.firstCategory(int)</c>.</remarks>
    public static int FirstCategory(int bits)
    {
        return bits == 0 ? 0 : (BitOperations.TrailingZeroCount((uint)bits) + 1);
    }

}
