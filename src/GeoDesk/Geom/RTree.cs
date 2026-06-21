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
internal abstract class RTree
{

    protected Node? root;

    /// <summary>
    /// A node of the R-tree: a bounding box enclosing its children, which are either
    /// leaf items (bounds) or further nodes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node</c>.</remarks>
    public class Node : Box
    {

        readonly List<IBounds> _children;
        readonly bool _isLeaf;

        /// <summary>
        /// Creates a node over the given children (a new empty list when null),
        /// expanding the node's box to enclose them.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node(List, boolean)</c>.</remarks>
        public Node(List<IBounds>? children, bool isLeaf)
        {
            if (children == null)
            {
                children = new List<IBounds>();
            }
            else
            {
                foreach (var b in children) ExpandToInclude(b);
            }
            _children = children;
            _isLeaf = isLeaf;
        }

        /// <summary>True if this node's children are leaf items rather than further nodes.</summary>
        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.isLeaf()</c>.</remarks>
        public bool IsLeaf => _isLeaf;

        /// <summary>The node's children (leaf items or child nodes).</summary>
        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.children()</c>.</remarks>
        public List<IBounds> Children => _children;

        // may only be called before tree is built
        /// <summary>Adds a child to this node and expands its box to enclose it. Only valid while building the tree.</summary>
        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.add(Bounds)</c>.</remarks>
        internal void Add(IBounds child)
        {
            _children.Add(child);
            ExpandToInclude(child);
        }

        /// <summary>
        /// Visits every leaf item whose bounds intersect the query box, recursing into
        /// intersecting child nodes.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.Node.visit(Bounds, Consumer)</c>.</remarks>
        public void Visit<T>(IBounds bbox, Action<T> consumer) where T : IBounds
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

    /// <summary>The root node of the tree.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.root()</c>.</remarks>
    public Node Root => root!;

    /// <summary>
    /// Visits every leaf item in the tree whose bounds intersect the given query box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.RTree.query(Bounds, Consumer)</c>.</remarks>
    public void Query<T>(IBounds bbox, Action<T> consumer) where T : IBounds
    {
        root!.Visit(bbox, consumer);
    }

}
