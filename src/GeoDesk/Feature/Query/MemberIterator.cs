/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;
using GeoDesk.Common.Util;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

// TODO: filter
// TODO: fix lazy tile loading
// TODO: Apply filter
/// <summary>
/// Iterates over the members of a relation, decoding the packed member table to
/// resolve each member feature (local or foreign), its role, and applying the
/// type filter, matcher, and optional spatial filter as it advances.
/// </summary>
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
    readonly Segment _segment;
    readonly int _types;
    readonly Matcher _matcher;
    readonly IFilter? _filter;
    int _pCurrent;
    Matcher? _currentMatcher;
    int _role;
    string? _roleString;
    int _tip = FeatureConstants.START_TIP;
    int _tex = FeatureConstants.MEMBERS_START_TEX;
    Segment? _foreignSegment;
    int _pExports;
    int _member;
    IFeature? _memberFeature;

    /// <summary>
    /// Creates an iterator over the relation member table starting at the given
    /// pointer, restricted to the given feature types and constrained by the
    /// supplied matcher and optional filter, and pre-fetches the first member.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public MemberIterator(FeatureStore store, Segment segment, int pTable, int types, Matcher matcher, IFilter? filter)
    {
        _store = store;
        _segment = segment;
        _pCurrent = pTable;
        _types = types;
        _matcher = matcher;
        _filter = filter;
        _currentMatcher = matcher.AcceptRole(0, null);
            // TODO: skip call to acceptRole if first member has DIFFERENT_ROLE flag set
        FetchNextFeature();
    }

    /// <summary>
    /// Advances through the packed member table, decoding flags, foreign-tile and
    /// role information, until a member whose role is accepted by the matcher is
    /// reached. Returns the buffer position past that member's header, or 0 when the
    /// table is exhausted.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator.fetchNext()</c>.</remarks>
    int FetchNext()
    {
        var buf = new NioBuffer(_segment.Memory);
        for (; ; )
        {
            var p = _pCurrent;
            if ((_member & MfLast) != 0)
            {
                _member = 0;
                return 0;
            }
            _member = buf.GetInt(p);
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
                    int tipDelta = buf.GetShort(p);
                    if ((tipDelta & 1) != 0)
                    {
                        // wide TIP delta
                        tipDelta = buf.GetInt(p);
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
                int rawRole = buf.GetChar(p);
                if ((rawRole & 1) != 0)
                {
                    // common role
                    _role = (int)((uint)rawRole >> 1);   // unsigned
                    _roleString = null;
                    p += 2;
                }
                else
                {
                    rawRole = buf.GetInt(p);
                    _role = -1;
                    _roleString = Bytes.ReadString(buf, p + (rawRole >> 1)); // signed
                    p += 4;
                }
                _currentMatcher = _matcher.AcceptRole(_role, _roleString);
            }
            if (_currentMatcher != null) return p;
            _pCurrent = p;
        }
    }

    /// <summary>
    /// Resolves the next accepted member to an actual feature, loading the foreign
    /// tile and exports table for foreign members, applying the typed matcher, and
    /// assigning the member's role. Caches the result in the member-feature field,
    /// or null when no further matching member exists.
    /// </summary>
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
            Segment featureSegment;
            int pFeature;
            if ((_member & MfForeign) != 0)
            {
                if (_pExports < 0)
                {
                    var entry = _store.TileIndexEntry(_tip);
                    if (!FeatureStore.IsTileLoadedAndCurrent(entry)) throw new MissingTileException(_tip);
                    var tilePage = FeatureStore.PageFromEntry(entry);
                    _foreignSegment = _store.SegmentOfPage(tilePage);
                    var ppExports = _store.OffsetOfPage(tilePage) + 24;
                    _pExports = ppExports + new NioBuffer(_foreignSegment.Memory).GetInt(ppExports);
                }
                var foreignBuf = new NioBuffer(_foreignSegment!.Memory);
                featureSegment = _foreignSegment!;
                var ppExported = _pExports + (_tex << 2);
                pFeature = ppExported + foreignBuf.GetInt(ppExported);
            }
            else
            {
                featureSegment = _segment;
                pFeature = (int)((uint)_pCurrent & 0xffff_fffc) + ((_member >> 3) << 2);
            }
            _pCurrent = pNext;
            if (_currentMatcher!.AcceptTyped(_types, featureSegment, pFeature))
            {
                var f = _store.GetFeature(featureSegment, pFeature);
                // TODO: allow any negative instead of -1?
                f.SetRole(_role == -1 ? _roleString : _store.StringFromCode(_role));
                _memberFeature = f;
                return;
            }
        }
    }

    /// <summary>
    /// Returns true if a pre-fetched member feature is available.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator.hasNext()</c>.</remarks>
    public override bool HasNext()
    {
        return _memberFeature != null;
    }

    /// <summary>
    /// Returns the current member feature and pre-fetches the next one.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberIterator.next()</c>.</remarks>
    public override IFeature? Next()
    {
        var next = _memberFeature;
        FetchNextFeature();
        return next;
    }

}
