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
/// <summary>
/// An R-tree bulk-loaded with the Overlap Minimizing Top-down (OMT) algorithm,
/// partitioning items into roughly square tiles by alternating X/Y selection to keep
/// node overlap low. Unlike <see cref="OmtTreeBuilder{B}"/> this materializes the
/// concrete <see cref="RTree.Node"/> tree directly. Based on the rbush implementation
/// by Volodymyr Agafonkin.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.OverlapMinimizingTree</c>.</remarks>
internal class OverlapMinimizingTree : RTree
{

    static readonly IComparer<IBounds> CompareMinX = new MinXComparer();
    static readonly IComparer<IBounds> CompareMinY = new MinYComparer();

    /// <summary>
    /// Builds the tree from the given items with at most <paramref name="maxEntries"/>
    /// entries per node, setting the resulting root.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.OverlapMinimizingTree(List, int)</c>.</remarks>
    public OverlapMinimizingTree(List<IBounds> items, int maxEntries)
    {
        root = Build(items, 0, items.Count - 1, 0, maxEntries);
    }

    /// <summary>
    /// Recursively builds the subtree covering items in the index range
    /// [<paramref name="left"/>, <paramref name="right"/>]: returns a leaf node when
    /// the range fits, otherwise partitions it into mostly-square tiles via X/Y
    /// quickselect and packs each tile into a child node.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.OverlapMinimizingTree.build(List, int, int, int, int)</c>.</remarks>
    Node Build(List<IBounds> items, int left, int right, int height, int maxEntries)
    {
        var n = right - left + 1;
        var m = maxEntries;

        if (n <= maxEntries)
        {
            // reached leaf level; return leaf
            return new Node(items.GetRange(left, right + 1 - left), true);
        }

        if (height == 0)
        {
            // target height of the bulk-loaded tree
            height = (int)Ceiling(Log(n) / Log(m));

            // target number of root entries to maximize storage utilization
            m = (int)Ceiling((double)n / Pow(m, height - 1));
        }

        var node = new Node(null, false);

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
                node.Add(Build(items, j, right3, height - 1, maxEntries));
            }
        }
        return node;
    }

    // Port of Java's method reference OverlapMinimizingTree::compareMinX.
    /// <summary>
    /// Comparer that orders bounds by ascending minimum X coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.OverlapMinimizingTree.compareMinX(Bounds, Bounds)</c>.</remarks>
    sealed class MinXComparer : IComparer<IBounds>
    {
        /// <summary>
        /// Compares two bounds by their minimum X coordinate.
        /// </summary>
        public int Compare(IBounds? a, IBounds? b) => a!.MinX - b!.MinX;
    }

    // Port of Java's method reference OverlapMinimizingTree::compareMinY.
    /// <summary>
    /// Comparer that orders bounds by ascending minimum Y coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.OverlapMinimizingTree.compareMinY(Bounds, Bounds)</c>.</remarks>
    sealed class MinYComparer : IComparer<IBounds>
    {
        /// <summary>
        /// Compares two bounds by their minimum Y coordinate.
        /// </summary>
        public int Compare(IBounds? a, IBounds? b) => a!.MinY - b!.MinY;
    }

}
