/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Common.Store;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A view that contains the parent ways/relations of a specific node.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView</c>.</remarks>
internal class NodeParentView : ParentRelationView
{

    internal readonly StoredNode node;

    /// <summary>
    /// Creates a view over the parent ways and relations of the given node, constrained
    /// by the supplied types, matcher, and optional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView(FeatureStore, ByteBuffer, StoredNode, int, int, Matcher, Filter)</c>.</remarks>
    public NodeParentView(FeatureStore store, Segment segment, StoredNode node, int pRelations, int types,
        Matcher matcher, IFilter? filter)
        : base(store, segment, pRelations, types, matcher, filter)
    {
        this.node = node;
    }

    /// <summary>
    /// Returns a new view with the given constraints applied, collapsing to a ways-only
    /// or relations-only view when the requested types exclude one kind of parent.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        if ((types & TypeBits.RELATIONS) == 0)
        {
            // view has been restricted to ways only
            return node.ParentWays(types, matcher, filter);
        }
        else if ((types & TypeBits.WAYS) == 0)
        {
            // view has been restricted to relations only
            return new ParentRelationView(store, segment, pTable, types, matcher, filter);
        }
        return new NodeParentView(store, segment, node, pTable, types, matcher, filter);
    }

    /// <summary>
    /// Returns an iterator that yields the node's parent relations followed by its parent ways.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return new NodeParentIter(this);
    }

    /// <summary>
    /// Iterator that first walks the node's parent relations (through the base parent-relation
    /// iterator), then switches to a query over its parent ways.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView.Iter</c>.</remarks>
    class NodeParentIter : Iter
    {

        readonly NodeParentView _npView;
        readonly Query _wayQuery;
        IFeature? _nextFeature;
        int _phase;

        /// <summary>
        /// Creates the iterator, starting the parent-ways query and pre-fetching the first parent.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView.Iter()</c>.</remarks>
        public NodeParentIter(NodeParentView view)
            : base(view)
        {
            _npView = view;
            _wayQuery = new Query(_npView.node.ParentWays(_npView.types, _npView.matcher, _npView.filter));
            // TODO: To improve performance, we could start the query so it can fetch the parent
            //  ways in the background, while the caller is iterating over the parent relations
            FetchNext();
        }

        /// <summary>
        /// Advances to the next parent: parent relations first (phase 0), then parent ways (phase 1).
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView.Iter.fetchNext()</c>.</remarks>
        void FetchNext()
        {
            if (_phase == 0)
            {
                _nextFeature = base.Next();
                if (_nextFeature != null) return;
                _phase++;
            }
            _nextFeature = _wayQuery.Next();
        }

        /// <summary>
        /// Returns true if a pre-fetched parent feature is available.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView.Iter.hasNext()</c>.</remarks>
        public override bool HasNext()
        {
            return _nextFeature != null;
        }

        /// <summary>
        /// Returns the current parent feature and pre-fetches the next.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.NodeParentView.Iter.next()</c>.</remarks>
        public override IFeature? Next()
        {
            var next = _nextFeature;
            FetchNext();
            return next;
        }

    }

}
