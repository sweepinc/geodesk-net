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

    /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node</c>.</remarks>
    public class Node : Box
    {

        readonly List<Bounds> _children;
        readonly bool _isLeaf;

        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node(List, boolean)</c>.</remarks>
        public Node(List<Bounds>? children, bool isLeaf)
        {
            if (children == null)
            {
                children = new List<Bounds>();
            }
            else
            {
                foreach (var b in children) ExpandToInclude(b);
            }
            _children = children;
            _isLeaf = isLeaf;
        }

        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.isLeaf()</c>.</remarks>
        public bool IsLeaf => _isLeaf;

        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.children()</c>.</remarks>
        public List<Bounds> Children => _children;

        // may only be called before tree is built
        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.add(Bounds)</c>.</remarks>
        internal void Add(Bounds child)
        {
            _children.Add(child);
            ExpandToInclude(child);
        }

        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.visit(Bounds, Consumer)</c>.</remarks>
        public void Visit<T>(Bounds bbox, Action<T> consumer) where T : Bounds
        {
            if (_isLeaf)
            {
                foreach (var child in _children)
                {
                    if (child.Intersects(bbox)) consumer((T)child);
                }
            }
            else
            {
                foreach (var child in _children)
                {
                    if (child.Intersects(bbox)) ((Node)child).Visit(bbox, consumer);
                }
            }
        }

    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.root()</c>.</remarks>
    public Node Root => root!;

    /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.query(Bounds, Consumer)</c>.</remarks>
    public void Query<T>(Bounds bbox, Action<T> consumer) where T : Bounds
    {
        root!.Visit(bbox, consumer);
    }

}
