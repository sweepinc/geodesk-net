/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

/*
 * This class is based on QuickSelect (https://github.com/mourner/quickselect)
 * by Volodymyr Agafonkin. The original work is licensed as follows:
 *
 * ISC License
 *
 * Copyright (c) 2018, Volodymyr Agafonkin
 *
 * Permission to use, copy, modify, and/or distribute this software for any purpose
 * with or without fee is hereby granted, provided that the above copyright notice
 * and this permission notice appear in all copies.
 *
 * THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
 * REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND
 * FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
 * INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS
 * OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER
 * TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF
 * THIS SOFTWARE.
 *
 * (https://github.com/mourner/quickselect/blob/master/LICENSE)
 */

using System.Collections.Generic;
using static System.Math;

namespace GeoDesk.Geom;

/// <remarks>Ported from Java <c>com.geodesk.geom.QuickSelect</c>.</remarks>
internal static class QuickSelect
{

    // sort an array so that items come in groups of n unsorted items, with groups sorted between each
    // other; combines selection algorithm with binary divide & conquer approach

    /// <remarks>Ported from Java <c>com.geodesk.geom.QuickSelect.multiSelect(List, int, int, int, Comparator)</c>.</remarks>
    public static void MultiSelect<T>(IList<T> arr, int left, int right, int n, IComparer<T> compare)
    {
        var stack = new Stack<int>();
        stack.Push(left);
        stack.Push(right);

        while (stack.Count != 0)
        {
            right = stack.Pop();
            left = stack.Pop();

            if (right - left <= n) continue;

            var mid = left + (int)Ceiling((double)(right - left) / n / 2) * n;
            QuickselectStep(arr, mid, left, right, compare);

            stack.Push(left);
            stack.Push(mid);
            stack.Push(mid);
            stack.Push(right);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.QuickSelect.quickselect(List, int, int, int, Comparator)</c>.</remarks>
    public static void Quickselect<T>(IList<T> arr, int k, int left, int right, IComparer<T> compare)
    {
        QuickselectStep(arr, k, left, right != 0 ? right : (arr.Count - 1), compare);
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.QuickSelect.quickselectStep(List, int, int, int, Comparator)</c>.</remarks>
    static void QuickselectStep<T>(IList<T> arr, int k, int left, int right, IComparer<T> compare)
    {
        while (right > left)
        {
            if (right - left > 600)
            {
                double n = right - left + 1;
                double m = k - left + 1;
                var z = Log(n);
                var s = 0.5 * Exp(2 * z / 3);
                var sd = 0.5 * Sqrt(z * s * (n - s) / n) * (m - n / 2 < 0 ? -1 : 1);
                var newLeft = Max(left, (int)Floor(k - m * s / n + sd));
                var newRight = Min(right, (int)Floor(k + (n - m) * s / n + sd));
                QuickselectStep(arr, k, newLeft, newRight, compare);
            }

            var t = arr[k];
            var i = left;
            var j = right;

            Swap(arr, left, k);
            if (compare.Compare(arr[right], t) > 0) Swap(arr, left, right);

            while (i < j)
            {
                Swap(arr, i, j);
                i++;
                j--;
                while (compare.Compare(arr[i], t) < 0) i++;
                while (compare.Compare(arr[j], t) > 0) j--;
            }

            if (compare.Compare(arr[left], t) == 0)
            {
                Swap(arr, left, j);
            }
            else
            {
                j++;
                Swap(arr, j, right);
            }
            if (j <= k) left = j + 1;
            if (k <= j) right = j - 1;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.QuickSelect.swap(List, int, int)</c>.</remarks>
    static void Swap<T>(IList<T> arr, int i, int j)
    {
        var tmp = arr[i];
        arr[i] = arr[j];
        arr[j] = tmp;
    }

}
