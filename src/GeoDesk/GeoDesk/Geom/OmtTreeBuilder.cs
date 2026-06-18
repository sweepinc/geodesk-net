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
internal class OmtTreeBuilder<B> : ISpatialTreeBuilder<B> where B : Bounds
{

    static readonly IComparer<Bounds> CompareMinX = new MinXComparer();
    static readonly IComparer<Bounds> CompareMinY = new MinYComparer();

    readonly int _maxEntries;
    readonly ISpatialTreeFactory<B> _factory;

    /// <remarks>Ported from Java <c>com.geodesk.geom.OmtTreeBuilder(SpatialTreeFactory, int)</c>.</remarks>
    public OmtTreeBuilder(ISpatialTreeFactory<B> factory, int maxEntries)
    {
        _factory = factory;
        _maxEntries = maxEntries;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.OmtTreeBuilder.build(ArrayList, int, int, int, int)</c>.</remarks>
    B Build(IList<Bounds> items, int left, int right, int height, int maxEntries)
    {
        var n = right - left + 1;
        var m = maxEntries;

        if (n <= maxEntries)
        {
            // reached leaf level; return leaf
            return _factory.CreateLeaf(items, left, right + 1);
        }

        if (height == 0)
        {
            // target height of the bulk-loaded tree
            height = (int)Ceiling(Log(n) / Log(m));

            // target number of root entries to maximize storage utilization
            m = (int)Ceiling((double)n / Pow(m, height - 1));
        }

        var children = new List<B>();

        // split the items into M mostly square tiles

        var n2 = (int)Ceiling((double)n / m);
        var n1 = n2 * (int)Ceiling(Sqrt(m));

        QuickSelect.MultiSelect(items, left, right, n1, CompareMinX);

        for (var i = left; i <= right; i += n1)
        {
            var right2 = Min(i + n1 - 1, right);
            QuickSelect.MultiSelect(items, i, right2, n2, CompareMinY);

            for (var j = i; j <= right2; j += n2)
            {
                var right3 = Min(j + n2 - 1, right2);

                // pack each entry recursively
                children.Add(Build(items, j, right3, height - 1, maxEntries));
            }
        }
        return _factory.CreateBranch(children, 0, children.Count);
    }

    // Port of Java's method reference OmtTreeBuilder::compareMinX.
    /// <remarks>Ported from Java <c>com.geodesk.geom.OmtTreeBuilder.compareMinX(Bounds, Bounds)</c>.</remarks>
    sealed class MinXComparer : IComparer<Bounds>
    {
        public int Compare(Bounds? a, Bounds? b) => a!.MinX - b!.MinX;
    }

    // Port of Java's method reference OmtTreeBuilder::compareMinY.
    /// <remarks>Ported from Java <c>com.geodesk.geom.OmtTreeBuilder.compareMinY(Bounds, Bounds)</c>.</remarks>
    sealed class MinYComparer : IComparer<Bounds>
    {
        public int Compare(Bounds? a, Bounds? b) => a!.MinY - b!.MinY;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.OmtTreeBuilder.build(ArrayList)</c>.</remarks>
    public B Build(IList<Bounds> items)
    {
        return Build(items, 0, items.Count - 1, 0, _maxEntries);
    }

}
