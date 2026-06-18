/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using System.Numerics;

namespace GeoDesk.Feature.Store;

/// <summary>
/// Methods for dealing with zoom levels.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels</c>.</remarks>
public static class ZoomLevels
{

    public const string DEFAULT = "4,6,8,10,12";

    public const int MAX_LEVEL = 12;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels.fromString(String)</c>.</remarks>
    public static int FromString(string s)
    {
        var zoomLevels = 0;
        var a = s.Split(',');
        for (var i = 0; i < a.Length; i++)
        {
            int zoom;
            try
            {
                zoom = int.Parse(a[i], CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                zoom = -1;
            }
            if (zoom < 0 || zoom > 12)
            {
                throw new ArgumentException("Zoom level must be between 0 and 12, inclusive");
            }
            var bit = 1 << zoom;
            if ((zoomLevels & bit) != 0)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Zoom level {0} specified more than once", zoom));
            }
            zoomLevels |= bit;
        }

        if (zoomLevels == 0)
        {
            throw new ArgumentException("No zoom level specified");
        }
        return zoomLevels;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels.isValidZoomLevel(int, int)</c>.</remarks>
    public static bool IsValidZoomLevel(int zoomLevels, int zoom)
    {
        return (zoomLevels & (1 << zoom)) != 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels.minZoom(int)</c>.</remarks>
    public static int MinZoom(int zoomLevels)
    {
        return BitOperations.TrailingZeroCount((uint)zoomLevels);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels.maxZoom(int)</c>.</remarks>
    public static int MaxZoom(int zoomLevels)
    {
        return 31 - BitOperations.LeadingZeroCount((uint)zoomLevels);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels.numberOfLevels(int)</c>.</remarks>
    public static int NumberOfLevels(int zoomLevels)
    {
        return BitOperations.PopCount((uint)zoomLevels);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels.toArray(int)</c>.</remarks>
    public static int[] ToArray(int zoomLevels)
    {
        var a = new int[NumberOfLevels(zoomLevels)];
        var pos = 0;
        for (var level = 0; level <= MAX_LEVEL; level++)
        {
            if (IsValidZoomLevel(zoomLevels, level)) a[pos++] = level;
        }
        return a;
    }

    /// <summary>
    /// Returns the steps between zoom levels, encoded in an int. Each step, starting from the minimum
    /// zoom level to the next-higher zoom level, is encoded using 2 bits (e.g. for zoom levels 4, 6,
    /// and 9 the pattern would be 0b1110, to signify 2 steps between 4 and 6, and 3 steps between 6 and
    /// 9). If there are more than 3 steps between any level, this method returns -1 to indicate that
    /// the zoom levels are not valid.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.ZoomLevels.zoomSteps(int)</c>.</remarks>
    public static int ZoomSteps(int zoomLevels)
    {
        var zoomSteps = 0;
        var pos = 0;
        var step = BitOperations.TrailingZeroCount((uint)zoomLevels) + 1;
        for (; ; )
        {
            zoomLevels = (int)((uint)zoomLevels >> step);
            step = BitOperations.TrailingZeroCount((uint)zoomLevels) + 1;
            if (step == 33) return zoomSteps;
                // once zoomLevels is zero, we count 32 bits (+1), so are done
            if (step > 3) return -1;
            zoomSteps |= step << pos;
            pos += 2;
        }
    }

}
