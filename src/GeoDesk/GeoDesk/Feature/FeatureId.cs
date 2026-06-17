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
public static class FeatureId
{
    public static long Of(FeatureType type, long id)
    {
        return (id << 2) | (int)type;
    }

    public static long Of(int type, long id)
    {
        System.Diagnostics.Debug.Assert(type >= 0 && type <= 2);
        return (id << 2) | type;
    }

    public static long OfNode(long id)
    {
        return id << 2;
    }

    public static long OfWay(long id)
    {
        return (id << 2) | 1;
    }

    public static long OfRelation(long id)
    {
        return (id << 2) | 2;
    }

    public static long Id(long id)
    {
        return (long)((ulong)id >> 2);
    }

    public static FeatureType Type(long id)
    {
        return (FeatureType)((int)id & 3);
    }

    public static int TypeCode(long id)
    {
        return (int)id & 3;
    }

    public static long FromString(string s)
    {
        int n = s.IndexOf('/');
        if (n < 0) return 0;
        FeatureType type = FeatureTypes.From(s.Substring(0, n));
        return Of(type, long.Parse(s.Substring(n + 1), CultureInfo.InvariantCulture));
    }

    public static int Compare(long fid1, long fid2)
    {
        int t1 = (int)fid1 & 3;
        int t2 = (int)fid2 & 3;
        if (t1 < t2) return -1;
        if (t1 > t2) return 1;
        return fid1.CompareTo(fid2);
    }

    public static void Sort(long[] fids)
    {
        for (int i = 0; i < fids.Length; i++)
        {
            long fid = fids[i];
            fids[i] = (fid >> 2) | ((fid & 3) << 61);
        }
        Array.Sort(fids);
        for (int i = 0; i < fids.Length; i++)
        {
            long fid = fids[i];
            fids[i] = ((fid << 2) & 0x7fff_ffff_ffff_ffffL) | (long)((ulong)fid >> 61);
        }
    }

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

    public static bool IsNode(long fid)
    {
        return ((int)fid & 3) == 0;
    }

    public static bool IsWay(long fid)
    {
        return ((int)fid & 3) == 1;
    }

    public static bool IsRelation(long fid)
    {
        return ((int)fid & 3) == 2;
    }

    public static string ToString(long fid)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", TypeToString(fid), (long)((ulong)fid >> 2));
    }
}
