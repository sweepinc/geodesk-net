/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Feature;

/// <summary>
/// Methods for creating IDs that are unique across feature types.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId</c>.</remarks>
internal static class FeatureId
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.of(FeatureType, long)</c>.</remarks>
    public static long Of(FeatureType type, long id)
    {
        return (id << 2) | (int)type;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.of(int, long)</c>.</remarks>
    public static long Of(int type, long id)
    {
        System.Diagnostics.Debug.Assert(type >= 0 && type <= 2);
        return (id << 2) | type;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.ofNode(long)</c>.</remarks>
    public static long OfNode(long id)
    {
        return id << 2;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.ofWay(long)</c>.</remarks>
    public static long OfWay(long id)
    {
        return (id << 2) | 1;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.ofRelation(long)</c>.</remarks>
    public static long OfRelation(long id)
    {
        return (id << 2) | 2;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.id(long)</c>.</remarks>
    public static long Id(long id)
    {
        return (long)((ulong)id >> 2);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.type(long)</c>.</remarks>
    public static FeatureType Type(long id)
    {
        return (FeatureType)((int)id & 3);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.typeCode(long)</c>.</remarks>
    public static int TypeCode(long id)
    {
        return (int)id & 3;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.fromString(String)</c>.</remarks>
    public static long FromString(string s)
    {
        var n = s.IndexOf('/');
        if (n < 0) return 0;
        var type = FeatureTypes.From(s.Substring(0, n));
        return Of(type, long.Parse(s.Substring(n + 1), CultureInfo.InvariantCulture));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.compare(long, long)</c>.</remarks>
    public static int Compare(long fid1, long fid2)
    {
        var t1 = (int)fid1 & 3;
        var t2 = (int)fid2 & 3;
        if (t1 < t2) return -1;
        if (t1 > t2) return 1;
        return fid1.CompareTo(fid2);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.sort(long[])</c>.</remarks>
    public static void Sort(long[] fids)
    {
        for (var i = 0; i < fids.Length; i++)
        {
            var fid = fids[i];
            fids[i] = (fid >> 2) | ((fid & 3) << 61);
        }
        Array.Sort(fids);
        for (var i = 0; i < fids.Length; i++)
        {
            var fid = fids[i];
            fids[i] = ((fid << 2) & 0x7fff_ffff_ffff_ffffL) | (long)((ulong)fid >> 61);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.typeToString(long)</c>.</remarks>
    public static string TypeToString(long fid)
    {
        switch ((int)fid & 3)
        {
            case 0: return "node";
            case 1: return "way";
            case 2: return "relation";
        }
        return "";
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.isNode(long)</c>.</remarks>
    public static bool IsNode(long fid)
    {
        return ((int)fid & 3) == 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.isWay(long)</c>.</remarks>
    public static bool IsWay(long fid)
    {
        return ((int)fid & 3) == 1;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.isRelation(long)</c>.</remarks>
    public static bool IsRelation(long fid)
    {
        return ((int)fid & 3) == 2;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.toString(long)</c>.</remarks>
    public static string ToString(long fid)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", TypeToString(fid), (long)((ulong)fid >> 2));
    }

}
