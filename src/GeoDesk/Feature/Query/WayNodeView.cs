/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A queryable view over the nodes of a way. Depending on the matcher it yields
/// only the way's feature-nodes (tagged nodes that exist as features) or, when no
/// type constraint is given, every coordinate including anonymous geometry nodes.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView</c>.</remarks>
internal class WayNodeView : TableView
{

    const int IncludeGeometryNodes = 256;

    readonly int _flags;

    /// <summary>
    /// Creates a view over the nodes of the way at the given pointer, accepting all
    /// nodes with no additional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public WayNodeView(FeatureStore store, NioBuffer buf, int ptr)
        : this(store, buf, ptr, TypeBits.NODES, Matcher.ALL, null)
    {
    }

    /// <summary>
    /// Creates a view over the nodes of the way at the given pointer, constrained by
    /// the supplied types, matcher, and optional filter. Anonymous geometry nodes are
    /// included only when the matcher imposes no constraint.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public WayNodeView(FeatureStore store, NioBuffer buf, int ptr, int types, Matcher matcher, IFilter? filter)
        : base(store, buf, ptr, types, matcher, filter)
    {
        _flags = (buf.Get(ptr) & 0xff) | ((matcher == Matcher.ALL) ? IncludeGeometryNodes : 0);
    }

    /// <summary>
    /// Returns a new way-node view over the same way with the given type, matcher,
    /// and filter constraints applied.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return new WayNodeView(store, buf, ptr, types, matcher, filter);
    }

    /// <summary>
    /// Resolves and returns the absolute buffer pointer to the way's body (its
    /// coordinate and node-reference table).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.bodyPtr()</c>.</remarks>
    int BodyPtr()
    {
        var ppBody = ptr + 12;
        return buf.GetInt(ppBody) + ppBody;
    }

    /// <summary>
    /// Returns an iterator over the way's nodes: only feature-nodes when geometry
    /// nodes are excluded, otherwise every coordinate including anonymous nodes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        if ((_flags & IncludeGeometryNodes) == 0)
            return new StoredWay.Iter(store, buf, BodyPtr() - 4 - (_flags & FeatureFlags.RELATION_MEMBER_FLAG), matcher);

        return new AllNodesIter(this, BodyPtr());
    }

    // TODO: apply spatial filters to geometric nodes
    // TODO: inverse this: derive from feature iterator instead?
    /// <summary>
    /// Iterator that yields every node of a way in order, returning the corresponding
    /// feature-node where one exists at a coordinate and an anonymous way-node
    /// otherwise. It walks the way's coordinate sequence and merges in the separate
    /// feature-node stream.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.AllNodesIter</c>.</remarks>
    class AllNodesIter : StoredWay.XYIterator, IEnumerator<IFeature>
    {

        readonly WayNodeView _owner;
        IFeature? _nextFeatureNode;
        StoredWay.Iter? _featureNodeIter;
        IFeature? _current;

        /// <summary>
        /// Creates the iterator over the way body, also priming the feature-node
        /// stream when the way carries feature-nodes so they can be matched by
        /// coordinate against the geometry nodes.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.AllNodesIter(int)</c>.</remarks>
        public AllNodesIter(WayNodeView owner, int pBody)
            : base(owner.buf, pBody, owner.buf.GetInt(owner.ptr - 16), owner.buf.GetInt(owner.ptr - 12), owner._flags)
        {
            _owner = owner;
            if ((owner._flags & FeatureFlags.WAYNODE_FLAG) != 0)
            {
                _featureNodeIter = new StoredWay.Iter(owner.store, owner.buf, pBody - 4 - (owner._flags & FeatureFlags.RELATION_MEMBER_FLAG), Matcher.ALL);
                // TODO: filters must apply to anonymous nodes as well!
                if (_featureNodeIter.HasNext())
                    _nextFeatureNode = _featureNodeIter.Next();
            }
        }

        /// <summary>
        /// Returns the next node: the matching feature-node when the next coordinate
        /// coincides with one, otherwise a freshly constructed anonymous way-node at
        /// that coordinate.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.AllNodesIter.next()</c>.</remarks>
        IFeature NextFeature()
        {
            var xy = NextXY();
            var x = XY.X(xy);
            var y = XY.Y(xy);
            if (_nextFeatureNode != null)
            {
                if (_nextFeatureNode.X == x && _nextFeatureNode.Y == y)
                {
                    var node = _nextFeatureNode;
                    _nextFeatureNode = _featureNodeIter!.HasNext() ? _featureNodeIter.Next() : null;
                    return node;
                }
            }
            return new AnonymousWayNode(_owner.store, x, y);
        }

        /// <summary>
        /// The node produced by the most recent successful <see cref="MoveNext"/> call.
        /// </summary>
        public IFeature Current => _current!;

        /// <summary>
        /// The non-generic view of <see cref="Current"/>.
        /// </summary>
        object IEnumerator.Current => _current!;

        /// <summary>
        /// Advances to the next node, caching it in <see cref="Current"/>; returns
        /// false once all of the way's nodes have been produced.
        /// </summary>
        public bool MoveNext()
        {
            if (!HasNext())
                return false;
            _current = NextFeature();
            return true;
        }

        /// <summary>
        /// Not supported; this iterator cannot be rewound.
        /// </summary>
        public void Reset()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Releases resources held by the iterator; a no-op.
        /// </summary>
        public void Dispose()
        {
        }

    }

}
