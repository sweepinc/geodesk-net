/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A queryable view over the relations that a feature belongs to (its parent
/// relations). Enumerating it yields each parent relation, resolving local and
/// foreign references and applying the view's type, matcher, and filter constraints.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView</c>.</remarks>
internal class ParentRelationView : TableView
{

    /// <summary>
    /// Creates a view over the parent relations referenced at the given pointer,
    /// accepting all relations with no additional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public ParentRelationView(FeatureStore store, NioBuffer buf, int ptr)
        : this(store, buf, ptr, TypeBits.RELATIONS, Matcher.ALL, null)
    {
    }

    /// <summary>
    /// Creates a view over the parent relations at the given pointer, constrained by
    /// the supplied feature types, matcher, and optional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public ParentRelationView(FeatureStore store, NioBuffer buf, int ptr, int types, Matcher matcher, IFilter? filter)
        : base(store, buf, ptr, types, matcher, filter)
    {
    }

    /// <summary>
    /// Returns a new parent-relation view over the same table with the given type,
    /// matcher, and filter constraints applied.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return new ParentRelationView(store, buf, ptr, types, matcher, filter);
    }

    /// <summary>
    /// Always returns false; a parent-relation table is only created when at least
    /// one parent relation exists, so the view can never be empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.isEmpty()</c>.</remarks>
    public bool IsEmpty()
    {
        // can never be empty
        return false;
    }

    /// <summary>
    /// Returns an iterator over the parent relations matched by this view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return new Iter(this);
    }

    /// <summary>
    /// Iterator that walks the packed parent-relation table, decoding local and
    /// foreign references (loading foreign tiles and their exports tables as needed)
    /// and yielding each relation that satisfies the view's matcher and filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter</c>.</remarks>
    protected class Iter : FeatureIterator
    {

        const int LastFlag = 1;
        const int ForeignFlag = 2;
        const int DifferentTileFlag = 4;
        const int WideTexFlag = 8;

        protected readonly ParentRelationView view;
        protected int tip = FeatureConstants.START_TIP;
        protected int tex = FeatureConstants.RELATIONS_START_TEX;
        protected NioBuffer foreignBuf;
        int _pExports;
        int _p;
        int _rel;
        IFeature? _current;

        /// <summary>
        /// Creates the iterator positioned at the start of the view's parent-relation
        /// table and pre-fetches the first matching relation.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter()</c>.</remarks>
        public Iter(ParentRelationView view)
        {
            this.view = view;
            _p = view.ptr;
            FetchNext();
        }

        /// <summary>
        /// Advances to the next parent relation that satisfies the matcher and filter,
        /// decoding foreign-tile and TEX/TIP deltas and resolving the relation's
        /// buffer location. Caches the result, or null once the table is exhausted.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter.fetchNext()</c>.</remarks>
        void FetchNext()
        {
            for (; ; )
            {
                NioBuffer relBuf;
                int pRel;
                if ((_rel & LastFlag) != 0)
                {
                    _current = null;
                    return;
                }
                var pCurrent = _p;
                _rel = view.buf.GetInt(pCurrent);
                _p += 4;
                if ((_rel & ForeignFlag) != 0)
                {
                    if ((_rel & WideTexFlag) == 0)
                    {
                        _rel = (short)_rel;
                        _p -= 2;
                    }
                    tex += _rel >> 4;
                    if ((_rel & DifferentTileFlag) != 0)
                    {
                        int tipDelta = view.buf.GetShort(_p);
                        if ((tipDelta & 1) != 0)
                        {
                            // wide TIP delta
                            tipDelta = view.buf.GetInt(_p);
                            _p += 2;
                        }
                        tipDelta >>= 1;     // signed
                        tip += tipDelta;
                        _p += 2;
                        var entry = view.store.TileIndexEntry(tip);
                        if (!FeatureStore.IsTileLoadedAndCurrent(entry)) throw new MissingTileException(tip);
                        var tilePage = FeatureStore.PageFromEntry(entry);
                        foreignBuf = new NioBuffer(view.store.SegmentOfPage(tilePage).Memory);
                        var ppExports = view.store.OffsetOfPage(tilePage) + 24;
                        _pExports = ppExports + foreignBuf.GetInt(ppExports);
                    }
                    relBuf = foreignBuf;
                    var ppExported = _pExports + (tex << 2);
                    pRel = ppExported + foreignBuf.GetInt(ppExported);
                }
                else
                {
                    relBuf = view.buf;
                    pRel = (int)((uint)pCurrent & 0xffff_fffe) + ((_rel >> 2) << 1);
                        // TODO: simplify alignment rules!
                }
                if (view.matcher.Accept(relBuf, pRel))
                {
                    var r = new StoredRelation(view.store, relBuf, pRel);
                    if (view.filter == null || view.filter.Accept(r))
                    {
                        _current = r;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if a pre-fetched parent relation is available.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter.hasNext()</c>.</remarks>
        public override bool HasNext()
        {
            return _current != null;
        }

        /// <summary>
        /// Returns the current parent relation and pre-fetches the next one.
        /// </summary>
        /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter.next()</c>.</remarks>
        public override IFeature? Next()
        {
            var next = _current;
            FetchNext();
            return next;
        }

    }

}
