/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Clarisma.Common.Util;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

// TODO: filter
// TODO: fix lazy tile loading
// TODO: Apply filter
public class MemberIterator : FeatureIterator
{
    private readonly FeatureStore store;
    private readonly NioBuffer buf;
    private readonly int types;
    private readonly Matcher matcher;
    private readonly Filter? filter;
    private int pCurrent;
    private Matcher? currentMatcher;
    private int role;
    private string? roleString;
    private int tip = FeatureConstants.START_TIP;
    private int tex = FeatureConstants.MEMBERS_START_TEX;
    private NioBuffer? foreignBuf;
    private int pExports;
    private int member;
    private Feature? memberFeature;

    // TODO: consolidate these flags?
    private const int MF_LAST = 1;
    private const int MF_FOREIGN = 2;
    private const int MF_DIFFERENT_ROLE = 4;
    private const int MF_DIFFERENT_TILE = 8;
    private const int MF_WIDE_TEX = 16;

    public MemberIterator(FeatureStore store, NioBuffer buf, int pTable,
        int types, Matcher matcher, Filter? filter)
    {
        this.store = store;
        this.buf = buf;
        pCurrent = pTable;
        this.types = types;
        this.matcher = matcher;
        this.filter = filter;
        currentMatcher = matcher.AcceptRole(0, null);
            // TODO: skip call to acceptRole if first member has DIFFERENT_ROLE flag set
        FetchNextFeature();
    }

    private int FetchNext()
    {
        for (; ; )
        {
            int p = pCurrent;
            if ((member & MF_LAST) != 0)
            {
                member = 0;
                return 0;
            }
            member = buf.GetInt(p);
            p += 4;
            if ((member & MF_FOREIGN) != 0)
            {
                if ((member & MF_WIDE_TEX) == 0)
                {
                    member = (short)member;
                    p -= 2;
                }
                if ((member & MF_DIFFERENT_TILE) != 0)
                {
                    // TODO: test wide tip delta
                    pExports = -1;
                    int tipDelta = buf.GetShort(p);
                    if ((tipDelta & 1) != 0)
                    {
                        // wide TIP delta
                        tipDelta = buf.GetInt(p);
                        p += 2;
                    }
                    tipDelta >>= 1;     // signed
                    p += 2;
                    tip += tipDelta;
                }
                tex += member >> 5;
            }
            if ((member & MF_DIFFERENT_ROLE) != 0)
            {
                int rawRole = buf.GetChar(p);
                if ((rawRole & 1) != 0)
                {
                    // common role
                    role = (int)((uint)rawRole >> 1);   // unsigned
                    roleString = null;
                    p += 2;
                }
                else
                {
                    rawRole = buf.GetInt(p);
                    role = -1;
                    roleString = Bytes.ReadString(buf, p + (rawRole >> 1)); // signed
                    p += 4;
                }
                currentMatcher = matcher.AcceptRole(role, roleString);
            }
            if (currentMatcher != null) return p;
            pCurrent = p;
        }
    }

    private void FetchNextFeature()
    {
        for (; ; )
        {
            int pNext = FetchNext();
            if (pNext == 0)
            {
                memberFeature = null;
                return;
            }
            NioBuffer featureBuf;
            int pFeature;
            if ((member & MF_FOREIGN) != 0)
            {
                if (pExports < 0)
                {
                    int entry = store.TileIndexEntry(tip);
                    if (!FeatureStore.IsTileLoadedAndCurrent(entry))
                    {
                        throw new MissingTileException(tip);
                    }
                    int tilePage = FeatureStore.PageFromEntry(entry);
                    foreignBuf = store.BufferOfPage(tilePage);
                    int ppExports = store.OffsetOfPage(tilePage) + 24;
                    pExports = ppExports + foreignBuf.GetInt(ppExports);
                }
                featureBuf = foreignBuf!;
                int ppExported = pExports + (tex << 2);
                pFeature = ppExported + foreignBuf!.GetInt(ppExported);
            }
            else
            {
                featureBuf = buf;
                pFeature = (int)((uint)pCurrent & 0xffff_fffc) + ((member >> 3) << 2);
            }
            pCurrent = pNext;
            if (currentMatcher!.AcceptTyped(types, featureBuf, pFeature))
            {
                StoredFeature f = store.GetFeature(featureBuf, pFeature);
                // TODO: allow any negative instead of -1?
                f.SetRole(role == -1 ? roleString : store.StringFromCode(role));
                memberFeature = f;
                return;
            }
        }
    }

    public override bool HasNext()
    {
        return memberFeature != null;
    }

    public override Feature? Next()
    {
        Feature? next = memberFeature;
        FetchNextFeature();
        return next;
    }
}
