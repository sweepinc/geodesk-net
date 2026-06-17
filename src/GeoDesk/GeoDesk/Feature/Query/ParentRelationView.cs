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

public class ParentRelationView : TableView
{
    public ParentRelationView(FeatureStore store, NioBuffer buf, int ptr)
        : this(store, buf, ptr, TypeBits.RELATIONS, Matcher.ALL, null)
    {
    }

    public ParentRelationView(FeatureStore store, NioBuffer buf, int ptr,
        int types, Matcher matcher, Filter? filter)
        : base(store, buf, ptr, types, matcher, filter)
    {
    }

    protected override Features NewWith(int types, Matcher matcher, Filter? filter)
    {
        return new ParentRelationView(store, buf, ptr, types, matcher, filter);
    }

    public bool IsEmpty()
    {
        // can never be empty
        return false;
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        return new Iter(this);
    }

    protected class Iter : FeatureIterator
    {
        protected readonly ParentRelationView view;
        protected int tip = FeatureConstants.START_TIP;
        protected int tex = FeatureConstants.RELATIONS_START_TEX;
        protected NioBuffer? foreignBuf;
        private int pExports;
        private int p;
        private int rel;
        private Feature? current;

        private const int LAST_FLAG = 1;
        private const int FOREIGN_FLAG = 2;
        private const int DIFFERENT_TILE_FLAG = 4;
        private const int WIDE_TEX_FLAG = 8;

        public Iter(ParentRelationView view)
        {
            this.view = view;
            p = view.ptr;
            FetchNext();
        }

        private void FetchNext()
        {
            for (; ; )
            {
                NioBuffer relBuf;
                int pRel;
                if ((rel & LAST_FLAG) != 0)
                {
                    current = null;
                    return;
                }
                int pCurrent = p;
                rel = view.buf.GetInt(pCurrent);
                p += 4;
                if ((rel & FOREIGN_FLAG) != 0)
                {
                    if ((rel & WIDE_TEX_FLAG) == 0)
                    {
                        rel = (short)rel;
                        p -= 2;
                    }
                    tex += rel >> 4;
                    if ((rel & DIFFERENT_TILE_FLAG) != 0)
                    {
                        int tipDelta = view.buf.GetShort(p);
                        if ((tipDelta & 1) != 0)
                        {
                            // wide TIP delta
                            tipDelta = view.buf.GetInt(p);
                            p += 2;
                        }
                        tipDelta >>= 1;     // signed
                        tip += tipDelta;
                        p += 2;
                        int entry = view.store.TileIndexEntry(tip);
                        if (!FeatureStore.IsTileLoadedAndCurrent(entry))
                        {
                            throw new MissingTileException(tip);
                        }
                        int tilePage = FeatureStore.PageFromEntry(entry);
                        foreignBuf = view.store.BufferOfPage(tilePage);
                        int ppExports = view.store.OffsetOfPage(tilePage) + 24;
                        pExports = ppExports + foreignBuf.GetInt(ppExports);
                    }
                    relBuf = foreignBuf!;
                    int ppExported = pExports + (tex << 2);
                    pRel = ppExported + foreignBuf!.GetInt(ppExported);
                }
                else
                {
                    relBuf = view.buf;
                    pRel = (int)((uint)pCurrent & 0xffff_fffe) + ((rel >> 2) << 1);
                        // TODO: simplify alignment rules!
                }
                if (view.matcher.Accept(relBuf, pRel))
                {
                    StoredRelation r = new StoredRelation(view.store, relBuf, pRel);
                    if (view.filter == null || view.filter.Accept(r))
                    {
                        current = r;
                        return;
                    }
                }
            }
        }

        public override bool HasNext()
        {
            return current != null;
        }

        public override Feature? Next()
        {
            Feature? next = current;
            FetchNext();
            return next;
        }
    }
}
