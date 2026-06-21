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
/// Helpers for encoding and decoding feature IDs that are unique across all feature types. A
/// type-prefixed ID packs the feature's numeric OSM id together with its 2-bit type code (node,
/// way, or relation) so that a single <c>long</c> uniquely identifies any feature in a library.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId</c>.</remarks>
internal static class FeatureId
{

    /// <summary>
    /// Encodes the given OSM id and <see cref="FeatureType"/> into a single type-prefixed feature ID.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.of(FeatureType, long)</c>.</remarks>
    public static long Of(FeatureType type, long id)
    {
        return (id << 2) | (int)type;
    }

    /// <summary>
    /// Encodes the given OSM id and numeric type code (0=node, 1=way, 2=relation) into a single
    /// type-prefixed feature ID.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.of(int, long)</c>.</remarks>
    public static long Of(int type, long id)
    {
        System.Diagnostics.Debug.Assert(type >= 0 && type <= 2);
        return (id << 2) | type;
    }

    /// <summary>
    /// Encodes the given OSM id as a node feature ID.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.ofNode(long)</c>.</remarks>
    public static long OfNode(long id)
    {
        return id << 2;
    }

    /// <summary>
    /// Encodes the given OSM id as a way feature ID.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.ofWay(long)</c>.</remarks>
    public static long OfWay(long id)
    {
        return (id << 2) | 1;
    }

    /// <summary>
    /// Encodes the given OSM id as a relation feature ID.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.ofRelation(long)</c>.</remarks>
    public static long OfRelation(long id)
    {
        return (id << 2) | 2;
    }

    /// <summary>
    /// Extracts the numeric OSM id from a type-prefixed feature ID, discarding the type code.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.id(long)</c>.</remarks>
    public static long Id(long id)
    {
        return (long)((ulong)id >> 2);
    }

    /// <summary>
    /// Extracts the <see cref="FeatureType"/> from a type-prefixed feature ID.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.type(long)</c>.</remarks>
    public static FeatureType Type(long id)
    {
        return (FeatureType)((int)id & 3);
    }

    /// <summary>
    /// Extracts the raw 2-bit type code (0=node, 1=way, 2=relation) from a type-prefixed feature ID.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.typeCode(long)</c>.</remarks>
    public static int TypeCode(long id)
    {
        return (int)id & 3;
    }

    /// <summary>
    /// Parses a feature ID from its canonical string form (<c>type/number</c>, e.g. <c>way/123</c>),
    /// returning 0 if the string contains no <c>/</c> separator.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.fromString(String)</c>.</remarks>
    public static long FromString(string s)
    {
        var n = s.IndexOf('/');
        if (n < 0) return 0;
        var type = FeatureTypes.From(s.Substring(0, n));
        return Of(type, long.Parse(s.Substring(n + 1), CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Compares two type-prefixed feature IDs, ordering first by type code (node, then way, then
    /// relation) and then by the full encoded value. Returns a negative, zero, or positive result.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.compare(long, long)</c>.</remarks>
    public static int Compare(long fid1, long fid2)
    {
        var t1 = (int)fid1 & 3;
        var t2 = (int)fid2 & 3;
        if (t1 < t2) return -1;
        if (t1 > t2) return 1;
        return fid1.CompareTo(fid2);
    }

    /// <summary>
    /// Sorts an array of type-prefixed feature IDs in place into type-then-id order, by temporarily
    /// rotating the type bits to the top so a plain numeric sort yields the desired ordering.
    /// </summary>
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

    /// <summary>
    /// Returns the lowercase type name (<c>node</c>, <c>way</c>, or <c>relation</c>) for the given
    /// type-prefixed feature ID, or an empty string if the type code is not recognized.
    /// </summary>
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

    /// <summary>
    /// Returns true if the given type-prefixed feature ID identifies a node.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.isNode(long)</c>.</remarks>
    public static bool IsNode(long fid)
    {
        return ((int)fid & 3) == 0;
    }

    /// <summary>
    /// Returns true if the given type-prefixed feature ID identifies a way.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.isWay(long)</c>.</remarks>
    public static bool IsWay(long fid)
    {
        return ((int)fid & 3) == 1;
    }

    /// <summary>
    /// Returns true if the given type-prefixed feature ID identifies a relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.isRelation(long)</c>.</remarks>
    public static bool IsRelation(long fid)
    {
        return ((int)fid & 3) == 2;
    }

    /// <summary>
    /// Formats the given type-prefixed feature ID as its canonical string form <c>type/number</c>
    /// (e.g. <c>way/123</c>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.FeatureId.toString(long)</c>.</remarks>
    public static string ToString(long fid)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", TypeToString(fid), (long)((ulong)fid >> 2));
    }

}
