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
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView</c>.</remarks>
internal class WayNodeView : TableView
{

    const int IncludeGeometryNodes = 256;

    readonly int _flags;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public WayNodeView(FeatureStore store, NioBuffer buf, int ptr)
        : this(store, buf, ptr, TypeBits.NODES, Matcher.ALL, null)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public WayNodeView(FeatureStore store, NioBuffer buf, int ptr, int types, Matcher matcher, IFilter? filter)
        : base(store, buf, ptr, types, matcher, filter)
    {
        _flags = (buf.Get(ptr) & 0xff) | ((matcher == Matcher.ALL) ? IncludeGeometryNodes : 0);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatures NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return new WayNodeView(store, buf, ptr, types, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.bodyPtr()</c>.</remarks>
    int BodyPtr()
    {
        var ppBody = ptr + 12;
        return buf.GetInt(ppBody) + ppBody;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        if ((_flags & IncludeGeometryNodes) == 0)
            return new StoredWay.Iter(store, buf, BodyPtr() - 4 - (_flags & IFeatureFlags.RELATION_MEMBER_FLAG), matcher);
        return new AllNodesIter(this, BodyPtr());
    }

    // TODO: apply spatial filters to geometric nodes
    // TODO: inverse this: derive from feature iterator instead?
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.AllNodesIter</c>.</remarks>
    class AllNodesIter : StoredWay.XYIterator, IEnumerator<IFeature>
    {

        readonly WayNodeView _owner;
        IFeature? _nextFeatureNode;
        StoredWay.Iter? _featureNodeIter;
        IFeature? _current;

        /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.AllNodesIter(int)</c>.</remarks>
        public AllNodesIter(WayNodeView owner, int pBody)
            : base(owner.buf, pBody, owner.buf.GetInt(owner.ptr - 16), owner.buf.GetInt(owner.ptr - 12), owner._flags)
        {
            _owner = owner;
            if ((owner._flags & IFeatureFlags.WAYNODE_FLAG) != 0)
            {
                _featureNodeIter = new StoredWay.Iter(owner.store, owner.buf,
                    pBody - 4 - (owner._flags & IFeatureFlags.RELATION_MEMBER_FLAG), Matcher.ALL);
                    // TODO: filters must apply to anonymous nodes as well!
                if (_featureNodeIter.HasNext()) _nextFeatureNode = _featureNodeIter.Next();
            }
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.query.WayNodeView.AllNodesIter.next()</c>.</remarks>
        IFeature NextFeature()
        {
            var xy = NextXY();
            var x = XY.X(xy);
            var y = XY.Y(xy);
            if (_nextFeatureNode != null)
            {
                if (_nextFeatureNode.X() == x && _nextFeatureNode.Y() == y)
                {
                    var node = _nextFeatureNode;
                    _nextFeatureNode = _featureNodeIter!.HasNext() ? _featureNodeIter.Next() : null;
                    return node;
                }
            }
            return new AnonymousWayNode(_owner.store, x, y);
        }

        public IFeature Current => _current!;

        object IEnumerator.Current => _current!;

        public bool MoveNext()
        {
            if (!HasNext()) return false;
            _current = NextFeature();
            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }

    }

}
