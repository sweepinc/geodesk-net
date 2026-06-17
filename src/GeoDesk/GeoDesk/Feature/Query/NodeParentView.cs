/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A view that contains the parent ways/relations of a specific node.
/// </summary>
public class NodeParentView : ParentRelationView
{
    internal readonly StoredNode node;

    public NodeParentView(FeatureStore store, NioBuffer buf,
        StoredNode node, int pRelations, int types, Matcher matcher, Filter? filter)
        : base(store, buf, pRelations, types, matcher, filter)
    {
        this.node = node;
    }

    protected override Features NewWith(int types, Matcher matcher, Filter? filter)
    {
        if ((types & TypeBits.RELATIONS) == 0)
        {
            // view has been restricted to ways only
            return node.ParentWays(types, matcher, filter);
        }
        else if ((types & TypeBits.WAYS) == 0)
        {
            // view has been restricted to relations only
            return new ParentRelationView(store, buf, ptr, types, matcher, filter);
        }
        return new NodeParentView(store, buf, node, ptr, types, matcher, filter);
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        return new NodeParentIter(this);
    }

    private class NodeParentIter : Iter
    {
        private readonly NodeParentView npView;
        private readonly Query wayQuery;
        private Feature? nextFeature;
        private int phase;

        public NodeParentIter(NodeParentView view)
            : base(view)
        {
            this.npView = view;
            wayQuery = new Query(npView.node.ParentWays(npView.types, npView.matcher, npView.filter));
            // TODO: To improve performance, we could start the query so it
            //  can fetch the parent ways in the background, while the caller
            //  is iterating over the parent relations
            FetchNext();
        }

        private void FetchNext()
        {
            if (phase == 0)
            {
                nextFeature = base.Next();
                if (nextFeature != null) return;
                phase++;
            }
            nextFeature = wayQuery.Next();
        }

        public override bool HasNext()
        {
            return nextFeature != null;
        }

        public override Feature? Next()
        {
            Feature? next = nextFeature;
            FetchNext();
            return next;
        }
    }
}
