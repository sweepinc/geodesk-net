/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Clarisma.Common.Util;
using GeoDesk.Feature;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.TestUtils</c>.</remarks>
public static class TestUtils
{

    /// <summary>
    /// Reports the difference between two collections of features (both must be typed IDs sorted in
    /// ascending order).
    /// </summary>
    /// <returns>true if both sets contain the same features</returns>
    /// <remarks>Ported from Java <c>com.geodesk.tests.TestUtils.compareSets(String, LongList, String, LongList)</c>.</remarks>
    public static bool CompareSets(string aName, List<long> a, string bName, List<long> b)
    {
        var ia = 0;
        var ib = 0;
        var equal = true;

        for (; ; )
        {
            var fa = a[ia];
            var fb = b[ib];
            if (fa != fb)
            {
                equal = false;
                if (fa < fb)
                {
                    Log.Warn("%s is in %s, but not in %s", FeatureId.ToString(fa), aName, bName);
                    ia++;
                }
                else
                {
                    Log.Warn("%s is in %s, but not in %s", FeatureId.ToString(fb), bName, aName);
                    ib++;
                }
            }
            else
            {
                ia++;
                ib++;
            }
            if (ia == a.Count)
            {
                if (ib == b.Count) break;
                ia--;
            }
            else if (ib == b.Count)
            {
                ib--;
            }
        }
        if (equal)
        {
            Log.Debug("Great! %s and %s contain the same features.", aName, bName);
        }
        return equal;
    }

    /// <summary>
    /// Reports any duplicates in a feature collection (must be typed IDs sorted in ascending order).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.TestUtils.checkNoDupes(String, LongList)</c>.</remarks>
    public static bool CheckNoDupes(string name, List<long> list)
    {
        long prev = 0;
        long lastDupe = 0;
        for (var i = 0; i < list.Count; i++)
        {
            var fid = list[i];
            if (fid == prev)
            {
                if (lastDupe == 0)
                {
                    Log.Warn("Duplicates in %s:", name);
                }
                if (lastDupe != fid)
                {
                    Log.Warn("- %s", FeatureId.ToString(fid));
                    lastDupe = fid;
                }
            }
            prev = fid;
        }
        if (lastDupe == 0)
        {
            Log.Debug("Great! There are no dupes in %s.", name);
            return true;
        }
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.TestUtils.getSet(Features)</c>.</remarks>
    public static List<long> GetSet(Features features)
    {
        var list = new List<long>();
        foreach (var f in features)
        {
            list.Add(FeatureId.Of(f.Type(), f.Id()));
        }
        list.Sort();
        return list;
    }

    static readonly string[] IgnoredKeys = { "name", "ref", "type", "source", "note",
        "admin_level", "wikidata", "description" };

    /// <remarks>Ported from Java <c>com.geodesk.tests.TestUtils.isIgnoredKey(String)</c>.</remarks>
    public static bool IsIgnoredKey(string k)
    {
        foreach (var ignored in IgnoredKeys)
        {
            if (k == ignored) return true;
        }
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.TestUtils.primaryTag(Feature)</c>.</remarks>
    public static string PrimaryTag(GeoDesk.Feature.Feature f)
    {
        var tags = f.Tags();
        while (tags.Next())
        {
            var key = tags.Key();
            if (!IsIgnoredKey(key!))
            {
                return key + "=" + tags.Value();
            }
        }
        return "";
    }

}
