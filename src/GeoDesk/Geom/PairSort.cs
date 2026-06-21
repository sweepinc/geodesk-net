/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Diagnostics;

namespace GeoDesk.Geom;

// not used
/// <summary>
/// In-place quicksort that sorts an integer key array while keeping a parallel list of associated
/// values in the same order, so the two arrays remain paired.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.PairSort</c>.</remarks>
internal static class PairSort
{

    /// <summary>
    /// Sorts the entire key array (and the parallel value list) in ascending key order.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.PairSort.sort(int[], List)</c>.</remarks>
    public static void Sort<T>(int[] keys, IList<T> values)
    {
        Debug.Assert(keys.Length == values.Count);
        Sort(keys, values, 0, keys.Length - 1);
    }

    /// <summary>
    /// Recursively quicksorts the keys (and parallel values) within the inclusive index range
    /// <paramref name="left"/>..<paramref name="right"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.PairSort.sort(int[], List, int, int)</c>.</remarks>
    public static void Sort<T>(int[] keys, IList<T> values, int left, int right)
    {
        if (left >= right) return;

        var pivot = keys[(left + right) >> 1];
        var i = left - 1;
        var j = right + 1;

        for (; ; )
        {
            do i++; while (keys[i] < pivot);
            do j--; while (keys[j] > pivot);
            if (i >= j) break;
            Swap(keys, values, i, j);
        }

        Sort(keys, values, left, j);
        Sort(keys, values, j + 1, right);
    }

    /// <summary>
    /// Swaps the entries at indices <paramref name="i"/> and <paramref name="j"/> in both the key
    /// array and the value list.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.PairSort.swap(int[], List, int, int)</c>.</remarks>
    static void Swap<T>(int[] keys, IList<T> values, int i, int j)
    {
        var tk = keys[i];
        keys[i] = keys[j];
        keys[j] = tk;
        var tv = values[i];
        values[i] = values[j];
        values[j] = tv;
    }

}
