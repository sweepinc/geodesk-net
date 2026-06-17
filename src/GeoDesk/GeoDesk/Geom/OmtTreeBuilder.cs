/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

/*
 * This class is based on rbush (https://github.com/mourner/rbush)
 * by Volodymyr Agafonkin. The original work is licensed as follows:
 *
 * MIT License
 *
 * Copyright (c) 2016 Volodymyr Agafonkin
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 * (https://github.com/mourner/rbush/blob/master/LICENSE)
 */

using System.Collections.Generic;
using static System.Math;

namespace GeoDesk.Geom;

// careful when translating code from JavaScript:
// must cast ints explicitly into doubles
/// <remarks>Ported from Java <c>com.geodesk.geom.OmtTreeBuilder</c>.</remarks>
public class OmtTreeBuilder<B> : ISpatialTreeBuilder<B> where B : Bounds
{
    private static readonly IComparer<Bounds> CompareMinX = new MinXComparer();
    private static readonly IComparer<Bounds> CompareMinY = new MinYComparer();

    private readonly int maxEntries;
    private readonly ISpatialTreeFactory<B> factory;

    public OmtTreeBuilder(ISpatialTreeFactory<B> factory, int maxEntries)
    {
        this.factory = factory;
        this.maxEntries = maxEntries;
    }

    private B Build(IList<Bounds> items, int left, int right, int height, int maxEntries)
    {
        int n = right - left + 1;
        int m = maxEntries;

        if (n <= maxEntries)
        {
            // reached leaf level; return leaf
            return factory.CreateLeaf(items, left, right + 1);
        }

        if (height == 0)
        {
            // target height of the bulk-loaded tree
            height = (int)Ceiling(Log(n) / Log(m));

            // target number of root entries to maximize storage utilization
            m = (int)Ceiling((double)n / Pow(m, height - 1));
        }

        List<B> children = new List<B>();

        // split the items into M mostly square tiles

        int n2 = (int)Ceiling((double)n / m);
        int n1 = n2 * (int)Ceiling(Sqrt(m));

        QuickSelect.MultiSelect(items, left, right, n1, CompareMinX);

        for (int i = left; i <= right; i += n1)
        {
            int right2 = Min(i + n1 - 1, right);
            QuickSelect.MultiSelect(items, i, right2, n2, CompareMinY);

            for (int j = i; j <= right2; j += n2)
            {
                int right3 = Min(j + n2 - 1, right2);

                // pack each entry recursively
                children.Add(Build(items, j, right3, height - 1, maxEntries));
            }
        }
        return factory.CreateBranch(children, 0, children.Count);
    }

    private sealed class MinXComparer : IComparer<Bounds>
    {
        public int Compare(Bounds? a, Bounds? b) => a!.MinX - b!.MinX;
    }

    private sealed class MinYComparer : IComparer<Bounds>
    {
        public int Compare(Bounds? a, Bounds? b) => a!.MinY - b!.MinY;
    }

    public B Build(IList<Bounds> items)
    {
        return Build(items, 0, items.Count - 1, 0, maxEntries);
    }
}
