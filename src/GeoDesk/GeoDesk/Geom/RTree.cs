/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;

namespace GeoDesk.Geom;

/// <summary>
/// A generic r-tree. Supports querying via bounding box.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.RTree</c>.</remarks>
public abstract class RTree
{
    protected Node? root;

    public class Node : Box
    {
        private readonly List<Bounds> children;
        private readonly bool isLeaf;

        public Node(List<Bounds>? children, bool isLeaf)
        {
            if (children == null)
            {
                children = new List<Bounds>();
            }
            else
            {
                foreach (Bounds b in children) ExpandToInclude(b);
            }
            this.children = children;
            this.isLeaf = isLeaf;
        }

        public bool IsLeaf => isLeaf;

        public List<Bounds> Children => children;

        // may only be called before tree is built
        internal void Add(Bounds child)
        {
            children.Add(child);
            ExpandToInclude(child);
        }

        public void Visit<T>(Bounds bbox, Action<T> consumer) where T : Bounds
        {
            if (isLeaf)
            {
                foreach (Bounds child in children)
                {
                    if (child.Intersects(bbox)) consumer((T)child);
                }
            }
            else
            {
                foreach (Bounds child in children)
                {
                    if (child.Intersects(bbox)) ((Node)child).Visit(bbox, consumer);
                }
            }
        }
    }

    public Node Root => root!;

    public void Query<T>(Bounds bbox, Action<T> consumer) where T : Bounds
    {
        root!.Visit(bbox, consumer);
    }
}
