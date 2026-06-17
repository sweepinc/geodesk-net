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
public static class ZoomLevels
{
    public const string DEFAULT = "4,6,8,10,12";

    public const int MAX_LEVEL = 12;

    public static int FromString(string s)
    {
        int zoomLevels = 0;
        string[] a = s.Split(',');
        for (int i = 0; i < a.Length; i++)
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
            int bit = 1 << zoom;
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

    public static bool IsValidZoomLevel(int zoomLevels, int zoom)
    {
        return (zoomLevels & (1 << zoom)) != 0;
    }

    public static int MinZoom(int zoomLevels)
    {
        return BitOperations.TrailingZeroCount((uint)zoomLevels);
    }

    public static int MaxZoom(int zoomLevels)
    {
        return 31 - BitOperations.LeadingZeroCount((uint)zoomLevels);
    }

    public static int NumberOfLevels(int zoomLevels)
    {
        return BitOperations.PopCount((uint)zoomLevels);
    }

    public static int[] ToArray(int zoomLevels)
    {
        int[] a = new int[NumberOfLevels(zoomLevels)];
        int pos = 0;
        for (int level = 0; level <= MAX_LEVEL; level++)
        {
            if (IsValidZoomLevel(zoomLevels, level)) a[pos++] = level;
        }
        return a;
    }

    /// <summary>
    /// Returns the steps between zoom levels, encoded in an int. Returns -1 if there are
    /// more than 3 steps between any level (invalid).
    /// </summary>
    public static int ZoomSteps(int zoomLevels)
    {
        int zoomSteps = 0;
        int pos = 0;
        int step = BitOperations.TrailingZeroCount((uint)zoomLevels) + 1;
        for (; ; )
        {
            zoomLevels = (int)((uint)zoomLevels >> step);
            step = BitOperations.TrailingZeroCount((uint)zoomLevels) + 1;
            if (step == 33) return zoomSteps;
            if (step > 3) return -1;
            zoomSteps |= step << pos;
            pos += 2;
        }
    }
}
