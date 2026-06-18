/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView</c>.</remarks>
internal class ParentRelationView : TableView
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView(FeatureStore, ByteBuffer, int)</c>.</remarks>
    public ParentRelationView(FeatureStore store, NioBuffer buf, int ptr)
        : this(store, buf, ptr, TypeBits.RELATIONS, Matcher.ALL, null)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public ParentRelationView(FeatureStore store, NioBuffer buf, int ptr, int types, Matcher matcher, IFilter? filter)
        : base(store, buf, ptr, types, matcher, filter)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatures NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return new ParentRelationView(store, buf, ptr, types, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.isEmpty()</c>.</remarks>
    public bool IsEmpty()
    {
        // can never be empty
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return new Iter(this);
    }

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
        protected NioBuffer? foreignBuf;
        int _pExports;
        int _p;
        int _rel;
        IFeature? _current;

        /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter()</c>.</remarks>
        public Iter(ParentRelationView view)
        {
            this.view = view;
            _p = view.ptr;
            FetchNext();
        }

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
                        foreignBuf = view.store.BufferOfPage(tilePage);
                        var ppExports = view.store.OffsetOfPage(tilePage) + 24;
                        _pExports = ppExports + foreignBuf.GetInt(ppExports);
                    }
                    relBuf = foreignBuf!;
                    var ppExported = _pExports + (tex << 2);
                    pRel = ppExported + foreignBuf!.GetInt(ppExported);
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

        /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter.hasNext()</c>.</remarks>
        public override bool HasNext()
        {
            return _current != null;
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.query.ParentRelationView.Iter.next()</c>.</remarks>
        public override IFeature? Next()
        {
            var next = _current;
            FetchNext();
            return next;
        }

    }

}
