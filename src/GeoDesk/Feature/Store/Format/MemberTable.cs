/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

namespace GeoDesk.Feature.Store.Format;

/// <summary>
/// A typed handle for a relation's member table: the <c>store</c> plus a memory sliced to the table,
/// which locates it and lets its (possibly cross-tile) member references be resolved. Replaces the
/// loose <c>(FeatureStore, Segment, int)</c> arguments previously threaded through the member
/// iterator/view. <c>Store</c> is part of the handle because foreign members resolve via the tile
/// index into other segments.
/// </summary>
/// <remarks>Port-only handle (no direct Java counterpart); the member-table layout it locates is
/// read by <c>com.geodesk.feature.query.MemberIterator</c> / <c>MemberView</c>.</remarks>
internal readonly struct MemberTable
{

    const int FirstEntryOfs = 0; // the table's first word; 0 ⇒ no entries

    readonly FeatureStore _store;
    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the member table

    /// <summary>
    /// Creates a member-table handle over the given store and memory sliced to the table.
    /// </summary>
    public MemberTable(FeatureStore store, ReadOnlyMemory<byte> buf)
    {
        _store = store;
        _buf = buf;
    }

    /// <summary>The store the member table belongs to, needed to resolve foreign members.</summary>
    public FeatureStore Store => _store;

    /// <summary>The memory window sliced to the member table.</summary>
    public ReadOnlyMemory<byte> Buf => _buf;

    /// <summary>True if the table has no entries.</summary>
    public bool IsEmpty => _buf.Span.GetIntLE(FirstEntryOfs) == 0;

}
