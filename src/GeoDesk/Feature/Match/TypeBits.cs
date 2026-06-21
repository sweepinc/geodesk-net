/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Diagnostics;
using System.Text;

namespace GeoDesk.Feature.Match;

/// <summary>
/// Support for bitsets that precisely describe which features to match. See the Java
/// original for the bit layout documentation.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeBits</c>.</remarks>
internal static class TypeBits
{

    public const int NODES = 0b00000000_00000101_00000000_00000101;
    public const int WAYS = 0b00000000_11110000_00000000_11110000;
    public const int RELATIONS = 0b00001111_00000000_00001111_00000000;
    public const int AREAS = 0b00001010_10100000_00001010_10100000;
    public const int WAYNODE_FLAGGED = 0b00000000_11110101_00000000_00000000;
    public const int RELATION_MEMBER = 0b00001100_11000100_00001100_11000100;
    public const int NONAREA_WAYS = WAYS & (~AREAS);
    public const int NONAREA_RELATIONS = RELATIONS & (~AREAS);
    public const int ALL = NODES | WAYS | RELATIONS;

    /// <summary>
    /// Converts a feature's flag word into the single type bit identifying its kind and
    /// area-ness.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeBits.fromFeatureFlags(int)</c>.</remarks>
    public static int FromFeatureFlags(int flags)
    {
        Debug.Assert((1 << 31) == (1 << unchecked((int)0xffff_ffff)));
        return 1 << (flags >> 1); // Don't need & 0x1F, C#'s shift only considers lowest 5 bits
    }

    /// <summary>
    /// Returns a human-readable, newline-separated description of the feature types
    /// selected by the given type-bits mask.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeBits.toString(int)</c>.</remarks>
    public static string ToString(int flags)
    {
        var s = new StringBuilder();
        for (var type = 0; type < 28; type++)
        {
            if ((flags & (1 << type)) != 0)
            {
                if (s.Length != 0) s.Append('\n');
                s.Append(((type >> 2) & 3) switch
                {
                    0 => "  node",
                    1 => "  way",
                    2 => "  relation",
                    _ => "  invalid"
                });
                if ((type & 1) != 0) s.Append(" area");
                if ((type & 2) != 0) s.Append(" relmember");
                if ((type & 16) != 0) s.Append(" waynode");
            }
        }
        return s.ToString();
    }

}
