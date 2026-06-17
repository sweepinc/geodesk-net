/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using Clarisma.Common.Math;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.TagMatcher</c>.</remarks>
public abstract class TagMatcher : Matcher
{
    protected readonly string[] globalStrings;
    protected readonly int keyMask;
    protected readonly int keyMin;

    // TODO: take FeatureStore, resources
    protected TagMatcher(int types, string[] globalStrings, int keyMask, int keyMin)
        : base(types)
    {
        this.globalStrings = globalStrings;
        this.keyMask = keyMask;
        this.keyMin = keyMin;
    }

    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        types &= 1 << ((sbyte)buf.Get(pos) >> 1);
        if ((types & acceptedTypes) == 0) return false;
        return Accept(buf, pos);
    }

    public override bool AcceptIndex(int keys)
    {
        return (keys & keyMask) >= keyMin;
    }

    protected static string DoubleToString(double d)
    {
        if (d == (long)d) return ((long)d).ToString(CultureInfo.InvariantCulture);
        return d.ToString("R", CultureInfo.InvariantCulture);
    }

    protected static double StringToDouble(string s)
    {
        return MathUtils.DoubleFromString(s);
    }

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
