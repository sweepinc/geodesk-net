/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;

using GeoDesk.Common.Math;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher</c>.</remarks>
internal abstract class TagMatcher : Matcher
{

    protected readonly string[] globalStrings;
    protected readonly int keyMask;
    protected readonly int keyMin;

    // TODO: take FeatureStore, resources
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher(int, String[], int, int)</c>.</remarks>
    protected TagMatcher(int types, string[] globalStrings, int keyMask, int keyMin)
        : base(types)
    {
        this.globalStrings = globalStrings;
        this.keyMask = keyMask;
        this.keyMin = keyMin;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.acceptTyped(int, ByteBuffer, int)</c>.</remarks>
    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        types &= 1 << ((sbyte)buf.Get(pos) >> 1);
        if ((types & acceptedTypes) == 0) return false;
        return Accept(buf, pos);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.acceptIndex(int)</c>.</remarks>
    public override bool AcceptIndex(int keys)
    {
        return (keys & keyMask) >= keyMin;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.doubleToString(double)</c>.</remarks>
    protected static string DoubleToString(double d)
    {
        if (d == (long)d) return ((long)d).ToString(CultureInfo.InvariantCulture);
        return d.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.stringToDouble(String)</c>.</remarks>
    protected static double StringToDouble(string s)
    {
        return MathUtils.DoubleFromString(s);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher.globalString(int)</c>.</remarks>
    protected string GlobalString(int code)
    {
        try
        {
            return globalStrings[code];
        }
        catch (System.IndexOutOfRangeException)
        {
            // TODO: this is a sign of an invalid FeatureStore
            throw new QueryException(string.Format(CultureInfo.InvariantCulture,
                "Invalid global string code: {0}", code));
        }
    }

}
