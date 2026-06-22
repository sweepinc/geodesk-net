/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;

using GeoDesk.Common.Math;
using GeoDesk.Common.Store;
using GeoDesk.Feature.Store;

namespace GeoDesk.Feature.Match;

/// <summary>
/// Abstract base for matchers that test a feature's tags. Holds the global string table for
/// resolving string-coded keys and values and a key range used to quickly reject features whose
/// indexed key set cannot contain the matcher's key. Subclasses implement the actual tag comparison.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher</c>.</remarks>
internal abstract class TagMatcher : Matcher
{

    protected readonly GlobalStringTable globalStrings;
    protected readonly int keyMask;
    protected readonly int keyMin;

    // TODO: take FeatureStore, resources
    /// <summary>
    /// Initializes the tag matcher with its accepted types, the global string table, and the key
    /// index range used for fast index rejection.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher(int, String[], int, int)</c>.</remarks>
    protected TagMatcher(int types, GlobalStringTable globalStrings, int keyMask, int keyMin)
        : base(types)
    {
        this.globalStrings = globalStrings;
        this.keyMask = keyMask;
        this.keyMin = keyMin;
    }

    /// <summary>
    /// The store's global string table. Exposed so a compiled <see cref="ExpressionTagMatcher"/>'s delegate
    /// can read it through its <c>self</c> receiver instead of baking it into the tree as a constant — the
    /// way a real instance method reads <c>this.globalStrings</c>.
    /// </summary>
    /// <remarks>Port-only accessor (no Java counterpart): Java's generated matcher reads the inherited field directly.</remarks>
    public GlobalStringTable GlobalStrings => globalStrings;

    /// <summary>
    /// Rejects the feature if its type is not in the accepted set, otherwise runs the tag test.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.acceptTyped(int, ByteBuffer, int)</c>.</remarks>
    public override bool AcceptTyped(int types, Segment segment, int pFeature)
    {
        types &= 1 << ((sbyte)segment.Memory.Span[pFeature] >> 1);
        if ((types & acceptedTypes) == 0)
            return false;
        return Accept(segment, pFeature);
    }

    /// <summary>
    /// Accepts an index key set only if the masked key value is at least the matcher's minimum,
    /// enabling fast index-based pruning.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.acceptIndex(int)</c>.</remarks>
    public override bool AcceptIndex(int keys)
    {
        return (keys & keyMask) >= keyMin;
    }

    /// <summary>
    /// Renders a double as a string, omitting the decimal portion when the value is integral.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.doubleToString(double)</c>.</remarks>
    protected static string DoubleToString(double d)
    {
        if (d == (long)d)
            return ((long)d).ToString(CultureInfo.InvariantCulture);
        else
            return d.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Leniently parses a string as a double for numeric tag comparisons.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.stringToDouble(String)</c>.</remarks>
    protected static double StringToDouble(string s)
    {
        return MathUtils.DoubleFromString(s);
    }

    /// <summary>
    /// Resolves a global string code to its text, throwing <see cref="QueryException"/> if the code is
    /// out of range (a sign of an invalid store).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.globalString(int)</c>.</remarks>
    protected string GlobalString(int code)
    {
        try
        {
            return globalStrings.Text(code);
        }
        catch (System.IndexOutOfRangeException)
        {
            // TODO: this is a sign of an invalid FeatureStore
            throw new QueryException(string.Format(CultureInfo.InvariantCulture,
                "Invalid global string code: {0}", code));
        }
    }

}
