/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

using static GeoDesk.Common.Store.BlobStoreConstants;

namespace GeoDesk.Common.Store.Format;

/// <summary>
/// The BlobStore file header: the fixed-layout record at offset 0 of the store (the root block). The
/// only structure in the store with an absolute, non-content-addressed layout.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore</c> / <c>BlobStoreConstants</c>
/// (the <c>*_OFS</c> header reads).</remarks>
internal readonly struct StoreHeader
{

    const int MagicOfs = 0;
    const int FreeTableSlotSize = 4; // bytes per trunk free-table slot

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the store (offset 0)

    public StoreHeader(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    public int Magic => _buf.Span.GetIntLE(MagicOfs);

    public int Version => _buf.Span.GetIntLE(VERSION_OFS);

    public long Timestamp => _buf.Span.GetLongLE(TIMESTAMP_OFS);

    public int TotalPages => _buf.Span.GetIntLE(TOTAL_PAGES_OFS);

    /// <summary>The page-size code (0 = 4K … 15 = 128 MB).</summary>
    public int PageSizeCode => _buf.Span.GetIntLE(PAGE_SIZE_OFS);

    /// <summary>The length of the metadata section in bytes (includes all header fields).</summary>
    public int MetadataSize => _buf.Span.GetIntLE(METADATA_SIZE_OFS);

    /// <summary>The resolved (absolute) pointer to the index, or 0 if the store is empty.</summary>
    public int IndexPointer => INDEX_PTR_OFS + _buf.Span.GetIntLE(INDEX_PTR_OFS);

    public bool IsValid => Magic == MAGIC;

    /// <summary>An empty store is valid but has no contents (no index pointer).</summary>
    public bool IsEmpty => _buf.Span.GetIntLE(INDEX_PTR_OFS) == 0;

    /// <summary>Bitmask of which 16-slot ranges of the trunk free-table are in use.</summary>
    public int TrunkRangeBits => _buf.Span.GetIntLE(TRUNK_FT_RANGE_BITS_OFS);

    /// <summary>The first page of the free-blob list (leaf free-table) for the given trunk slot, or <see cref="PageIndex.Nil"/>.</summary>
    public PageIndex TrunkFreeTablePage(int slot) => new PageIndex(_buf.Span.GetIntLE(TRUNK_FREE_TABLE_OFS + slot * FreeTableSlotSize));

    /// <summary>
    /// The store's unique identifier. The 16 bytes are stored in Java <c>UUID</c> layout
    /// (big-endian most/least-significant halves), so they are parsed as big-endian to yield the
    /// same value the exporter assigned.
    /// </summary>
    public Guid Guid => new Guid(_buf.Span.Slice(GUID_OFS, GUID_LEN), bigEndian: true);

}
