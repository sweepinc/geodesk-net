/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;
using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

/// <summary>
/// One entry in the tile-index tree. The first word is either a tile's page (when the low two bits
/// are not <c>01</c>) or, for a tile that has children, a pointer to the child level (low bits
/// <c>01</c>); a tile with children is followed by a 64-bit child-tile mask and the packed child
/// entries.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker</c> (v2 entry layout).</remarks>
internal readonly struct TileIndexEntry
{

    // Layout: word 0 = page (high 30 bits) or child pointer (low bits 01); for a tile with children,
    // an 8-byte child-tile mask follows at +4, then the packed child entries.
    const int EntryOfs = 0;
    const int FlagMask = 3;
    const int ChildPointerFlag = 1; // low bits 01 ⇒ a pointer to a child level
    const int PageShift = 2;
    const int ChildTileMaskOfs = 4;
    const int SmallMatrixEntriesOfs = 8;  // 4×4 matrix: 4-byte mask, entries begin at +8
    const int LargeMatrixEntriesOfs = 12; // 8×8 matrix: 8-byte mask, entries begin at +12
    const int LargeMatrixExtent = 8;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the entry

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="buf"></param>
    public TileIndexEntry(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    public int Raw => _buf.Span.GetIntLE(EntryOfs);

    /// <summary>
    /// True if this entry points to a child level rather than naming a tile page (low bits <c>01</c>).
    /// </summary>
    public bool IsChildPointer => (Raw & FlagMask) == ChildPointerFlag;

    /// <summary>
    /// The tile's page. Only meaningful when <see cref="IsChildPointer"/> is false.
    /// </summary>
    public PageIndex Page => new PageIndex((int)((uint)Raw >> PageShift));

    /// <summary>
    /// The child level's entry block. Only valid when <see cref="IsChildPointer"/> is true.
    /// </summary>
    public ReadOnlyMemory<byte> ChildLevel => _buf.Slice(Raw ^ ChildPointerFlag);

    /// <summary>The 64-bit mask of which cells in the child matrix actually have tiles.</summary>
    public long ChildTileMask => _buf.Span.GetLongLE(ChildTileMaskOfs);

    /// <summary>
    /// The packed child entries. The mask occupies 4 bytes for a 4×4 matrix and 8 bytes for an 8×8,
    /// so the entries begin at +8 or +12 respectively — hence <paramref name="extent"/>.
    /// </summary>
    public ReadOnlyMemory<byte> ChildEntries(int extent) => _buf.Slice(extent == LargeMatrixExtent ? LargeMatrixEntriesOfs : SmallMatrixEntriesOfs);

}
