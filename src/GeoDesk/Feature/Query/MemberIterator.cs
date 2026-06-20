/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

// TODO: filter
// TODO: fix lazy tile loading
// TODO: Apply filter
/// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator</c>.</remarks>
internal class MemberIterator : FeatureIterator
{

    // TODO: consolidate these flags?
    const int MfLast = 1;
    const int MfForeign = 2;
    const int MfDifferentRole = 4;
    const int MfDifferentTile = 8;
    const int MfWideTex = 16;

    readonly FeatureStore _store;
    readonly NioBuffer _buf;
    readonly int _types;
    readonly Matcher _matcher;
    readonly IFilter? _filter;
    int _pCurrent;
    Matcher? _currentMatcher;
    int _role;
    string? _roleString;
    int _tip = FeatureConstants.START_TIP;
    int _tex = FeatureConstants.MEMBERS_START_TEX;
    NioBuffer? _foreignBuf;
    int _pExports;
    int _member;
    IFeature? _memberFeature;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public MemberIterator(FeatureStore store, NioBuffer buf, int pTable, int types, Matcher matcher, IFilter? filter)
    {
        _store = store;
        _buf = buf;
        _pCurrent = pTable;
        _types = types;
        _matcher = matcher;
        _filter = filter;
        _currentMatcher = matcher.AcceptRole(0, null);
            // TODO: skip call to acceptRole if first member has DIFFERENT_ROLE flag set
        FetchNextFeature();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator.fetchNext()</c>.</remarks>
    int FetchNext()
    {
        for (; ; )
        {
            var p = _pCurrent;
            if ((_member & MfLast) != 0)
            {
                _member = 0;
                return 0;
            }
            _member = _buf.GetInt(p);
            p += 4;
            if ((_member & MfForeign) != 0)
            {
                if ((_member & MfWideTex) == 0)
                {
                    _member = (short)_member;
                    p -= 2;
                }
                if ((_member & MfDifferentTile) != 0)
                {
                    // TODO: test wide tip delta
                    _pExports = -1;
                    int tipDelta = _buf.GetShort(p);
                    if ((tipDelta & 1) != 0)
                    {
                        // wide TIP delta
                        tipDelta = _buf.GetInt(p);
                        p += 2;
                    }
                    tipDelta >>= 1;     // signed
                    p += 2;
                    _tip += tipDelta;
                }
                _tex += _member >> 5;
            }
            if ((_member & MfDifferentRole) != 0)
            {
                int rawRole = _buf.GetChar(p);
                if ((rawRole & 1) != 0)
                {
                    // common role
                    _role = (int)((uint)rawRole >> 1);   // unsigned
                    _roleString = null;
                    p += 2;
                }
                else
                {
                    rawRole = _buf.GetInt(p);
                    _role = -1;
                    _roleString = Bytes.ReadString(_buf, p + (rawRole >> 1)); // signed
                    p += 4;
                }
                _currentMatcher = _matcher.AcceptRole(_role, _roleString);
            }
            if (_currentMatcher != null) return p;
            _pCurrent = p;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator.fetchNextFeature()</c>.</remarks>
    void FetchNextFeature()
    {
        for (; ; )
        {
            var pNext = FetchNext();
            if (pNext == 0)
            {
                _memberFeature = null;
                return;
            }
            NioBuffer featureBuf;
            int pFeature;
            if ((_member & MfForeign) != 0)
            {
                if (_pExports < 0)
                {
                    var entry = _store.TileIndexEntry(_tip);
                    if (!FeatureStore.IsTileLoadedAndCurrent(entry)) throw new MissingTileException(_tip);
                    var tilePage = FeatureStore.PageFromEntry(entry);
                    _foreignBuf = NioBuffer.Of(_store.SegmentOfPage(tilePage).Memory);
                    var ppExports = _store.OffsetOfPage(tilePage) + 24;
                    _pExports = ppExports + _foreignBuf.GetInt(ppExports);
                }
                featureBuf = _foreignBuf!;
                var ppExported = _pExports + (_tex << 2);
                pFeature = ppExported + _foreignBuf!.GetInt(ppExported);
            }
            else
            {
                featureBuf = _buf;
                pFeature = (int)((uint)_pCurrent & 0xffff_fffc) + ((_member >> 3) << 2);
            }
            _pCurrent = pNext;
            if (_currentMatcher!.AcceptTyped(_types, featureBuf, pFeature))
            {
                var f = _store.GetFeature(featureBuf, pFeature);
                // TODO: allow any negative instead of -1?
                f.SetRole(_role == -1 ? _roleString : _store.StringFromCode(_role));
                _memberFeature = f;
                return;
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator.hasNext()</c>.</remarks>
    public override bool HasNext()
    {
        return _memberFeature != null;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator.next()</c>.</remarks>
    public override IFeature? Next()
    {
        var next = _memberFeature;
        FetchNextFeature();
        return next;
    }

}
