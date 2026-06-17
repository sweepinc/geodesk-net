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

/// <remarks>Ported from Java <c>com.geodesk.geom.HilbertTileTree</c>.</remarks>
public class HilbertTileTree : RTree
{
    private sealed class Pair : IComparable<Pair>
    {
        public int hilbertValue;
        public Bounds item = null!;

        public int CompareTo(Pair? o)
        {
            return hilbertValue.CompareTo(o!.hilbertValue);
        }
    }

    public HilbertTileTree(List<Bounds> items, int zoom, int maxEntries)
    {
        Pair[] pairs = new Pair[items.Count];
        for (int i = 0; i < pairs.Length; i++)
        {
            Bounds b = items[i];
            int x = (int)((uint)b.CenterX >> (32 - zoom - 15)) & 0x7fff;
            int y = (int)((uint)b.CenterY >> (32 - zoom - 15)) & 0x7fff;
            Pair p = new Pair();
            p.hilbertValue = Hilbert.FromXY(x, y);
            p.item = b;
            pairs[i] = p;
        }
        Array.Sort(pairs);

        List<Node> children = new List<Node>();
        int start = 0;
        while (start < pairs.Length)
        {
            int end = System.Math.Min(start + maxEntries, pairs.Length);
            Node child = new Node(new List<Bounds>(end - start), true);
            for (int i = start; i < end; i++) child.Add(pairs[i].item);
            children.Add(child);
            start = end;
        }

        List<Node> parents = new List<Node>();
        for (;;)
        {
            start = 0;
            while (start < children.Count)
            {
                int end = System.Math.Min(start + maxEntries, children.Count);
                Node child = new Node(new List<Bounds>(end - start), false);
                for (int i = start; i < end; i++) child.Add(children[i]);
                parents.Add(child);
                start = end;
            }
            if (parents.Count == 1)
            {
                root = parents[0];
                return;
            }
            List<Node> temp = children;
            children = parents;
            parents = temp;
            parents.Clear();
        }
    }
}
