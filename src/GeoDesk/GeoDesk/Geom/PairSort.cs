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
public static class PairSort
{
    public static void Sort<T>(int[] keys, IList<T> values)
    {
        Debug.Assert(keys.Length == values.Count);
        Sort(keys, values, 0, keys.Length - 1);
    }

    public static void Sort<T>(int[] keys, IList<T> values, int left, int right)
    {
        if (left >= right) return;

        int pivot = keys[(left + right) >> 1];
        int i = left - 1;
        int j = right + 1;

        for (;;)
        {
            do i++; while (keys[i] < pivot);
            do j--; while (keys[j] > pivot);
            if (i >= j) break;
            Swap(keys, values, i, j);
        }

        Sort(keys, values, left, j);
        Sort(keys, values, j + 1, right);
    }

    private static void Swap<T>(int[] keys, IList<T> values, int i, int j)
    {
        int tk = keys[i];
        keys[i] = keys[j];
        keys[j] = tk;
        T tv = values[i];
        values[i] = values[j];
        values[j] = tv;
    }
}
