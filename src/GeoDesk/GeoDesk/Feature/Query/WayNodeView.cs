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
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

public class WayNodeView : TableView
{
    private readonly int flags;

    private const int INCLUDE_GEOMETRY_NODES = 256;

    public WayNodeView(FeatureStore store, NioBuffer buf, int ptr)
        : this(store, buf, ptr, TypeBits.NODES, Matcher.ALL, null)
    {
    }

    public WayNodeView(FeatureStore store, NioBuffer buf, int ptr,
        int types, Matcher matcher, Filter? filter)
        : base(store, buf, ptr, types, matcher, filter)
    {
        flags = (buf.Get(ptr) & 0xff) |
            ((matcher == Matcher.ALL) ? INCLUDE_GEOMETRY_NODES : 0);
    }

    protected override Features NewWith(int types, Matcher matcher, Filter? filter)
    {
        return new WayNodeView(store, buf, ptr, types, matcher, filter);
    }

    private int BodyPtr()
    {
        int ppBody = ptr + 12;
        return buf.GetInt(ppBody) + ppBody;
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        if ((flags & INCLUDE_GEOMETRY_NODES) == 0)
        {
            return new StoredWay.Iter(store, buf, BodyPtr() - 4 -
                (flags & IFeatureFlags.RELATION_MEMBER_FLAG), matcher);
        }
        return new AllNodesIter(this, BodyPtr());
    }

    // TODO: apply spatial filters to geometric nodes
    // TODO: inverse this: derive from feature iterator instead?
    private class AllNodesIter : StoredWay.XYIterator, IEnumerator<Feature>
    {
        private readonly WayNodeView owner;
        private Feature? nextFeatureNode;
        private StoredWay.Iter? featureNodeIter;
        private Feature? current;

        public AllNodesIter(WayNodeView owner, int pBody)
            : base(owner.buf, pBody, owner.buf.GetInt(owner.ptr - 16),
                owner.buf.GetInt(owner.ptr - 12), owner.flags)
        {
            this.owner = owner;
            if ((owner.flags & IFeatureFlags.WAYNODE_FLAG) != 0)
            {
                featureNodeIter = new StoredWay.Iter(owner.store, owner.buf, pBody - 4 -
                    (owner.flags & IFeatureFlags.RELATION_MEMBER_FLAG), Matcher.ALL);
                    // TODO: filters must apply to anonymous nodes as well!
                if (featureNodeIter.HasNext()) nextFeatureNode = featureNodeIter.Next();
            }
        }

        private Feature NextFeature()
        {
            long xy = NextXY();
            int x = XY.X(xy);
            int y = XY.Y(xy);
            if (nextFeatureNode != null)
            {
                if (nextFeatureNode.X() == x && nextFeatureNode.Y() == y)
                {
                    Feature node = nextFeatureNode;
                    nextFeatureNode = featureNodeIter!.HasNext() ? featureNodeIter.Next() : null;
                    return node;
                }
            }
            return new AnonymousWayNode(owner.store, x, y);
        }

        public Feature Current => current!;

        object IEnumerator.Current => current!;

        public bool MoveNext()
        {
            if (!HasNext()) return false;
            current = NextFeature();
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
