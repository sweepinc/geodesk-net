/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

/*
 *
 * This class is based on flatbush (https://github.com/mourner/flatbush)
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
 * (https://github.com/mourner/flatbush/blob/master/LICENSE)
 */

using System;
using System.Collections.Generic;

namespace GeoDesk.Geom;

/// <summary>
/// Builds a bulk-loaded R-tree by ordering the input bounding boxes along a Hilbert
/// space-filling curve and packing them bottom-up into fixed-size nodes. The Hilbert
/// ordering keeps spatially close items adjacent, producing a tree with good query
/// locality. Based on the flatbush algorithm by Volodymyr Agafonkin.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.HilbertTileTree</c>.</remarks>
internal class HilbertTileTree : RTree
{

    /// <summary>
    /// An item paired with its Hilbert-curve value, used to sort the input bounds
    /// into Hilbert order before packing.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.HilbertTileTree.Pair</c>.</remarks>
    sealed class Pair : IComparable<Pair>
    {

        public int _hilbertValue;
        public IBounds _item = null!;

        /// <summary>
        /// Orders pairs by ascending Hilbert value.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.geom.HilbertTileTree.Pair.compareTo(Pair)</c>.</remarks>
        public int CompareTo(Pair? o)
        {
            return _hilbertValue.CompareTo(o!._hilbertValue);
        }

    }

    /// <summary>
    /// Builds the tree from the given bounding boxes: computes each item's Hilbert
    /// value at the given zoom, sorts by it, packs items into leaf nodes of at most
    /// <paramref name="maxEntries"/> entries, then repeatedly packs nodes into parent
    /// levels until a single root remains.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.HilbertTileTree(List, int, int)</c>.</remarks>
    public HilbertTileTree(List<IBounds> items, int zoom, int maxEntries)
    {
        var pairs = new Pair[items.Count];
        for (var i = 0; i < pairs.Length; i++)
        {
            var b = items[i];
            var x = (int)((uint)b.CenterX >> (32 - zoom - 15)) & 0x7fff;
            var y = (int)((uint)b.CenterY >> (32 - zoom - 15)) & 0x7fff;
            var p = new Pair();
            p._hilbertValue = Hilbert.FromXY(x, y);
            p._item = b;
            pairs[i] = p;
        }

        Array.Sort(pairs);

        var children = new List<Node>();
        var start = 0;
        while (start < pairs.Length)
        {
            var end = System.Math.Min(start + maxEntries, pairs.Length);
            var child = new Node(new List<IBounds>(end - start), true);
            for (var i = start; i < end; i++) child.Add(pairs[i]._item);
            children.Add(child);
            start = end;
        }

        var parents = new List<Node>();
        for (; ; )
        {
            start = 0;
            while (start < children.Count)
            {
                var end = System.Math.Min(start + maxEntries, children.Count);
                var child = new Node(new List<IBounds>(end - start), false);
                for (var i = start; i < end; i++) child.Add(children[i]);
                parents.Add(child);
                start = end;
            }
            if (parents.Count == 1)
            {
                root = parents[0];
                return;
            }
            var temp = children;
            children = parents;
            parents = temp;
            parents.Clear();
        }
    }

}
