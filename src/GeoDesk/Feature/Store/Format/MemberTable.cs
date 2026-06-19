/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Segment = GeoDesk.Common.Store.Segment;

namespace GeoDesk.Feature.Store.Format;

/// <summary>
/// A typed handle for a relation's member table: the <c>(store, segment, pointer)</c> triple that
/// locates the table and lets its (possibly cross-tile) member references be resolved. Replaces the
/// loose <c>(FeatureStore, Segment, int)</c> arguments previously threaded through the member
/// iterator/view. <c>Store</c> is part of the handle because foreign members resolve via the tile
/// index into other segments.
/// </summary>
/// <remarks>Port-only handle (no direct Java counterpart); the member-table layout it locates is
/// read by <c>com.geodesk.feature.query.MemberIterator</c> / <c>MemberView</c>.</remarks>
internal readonly struct MemberTable
{

    readonly FeatureStore _store;
    readonly Segment _buf;
    readonly int _ptr;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="store"></param>
    /// <param name="buf"></param>
    /// <param name="ptr"></param>
    public MemberTable(FeatureStore store, Segment buf, int ptr)
    {
        _store = store;
        _buf = buf;
        _ptr = ptr;
    }

    public FeatureStore Store => _store;

    public Segment Buf => _buf;

    public int Ptr => _ptr;

    /// <summary>True if the table has no entries.</summary>
    public bool IsEmpty => _buf.GetInt(_ptr) == 0;

}
