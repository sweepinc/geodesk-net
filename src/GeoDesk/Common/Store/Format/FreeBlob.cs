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
/// A free blob: a blob on the free list. Beyond the shared <see cref="BlobHeader"/> at offset 0, it
/// carries the doubly-linked free-list pointers and — when it also hosts a size range's leaf
/// free-table — that table's range bits and 512 slots.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore</c> (the free-blob fields:
/// <c>PREV_/NEXT_FREE_BLOB_OFS</c>, <c>LEAF_FT_*</c>).</remarks>
internal readonly struct FreeBlob
{

    const int FreeTableSlotSize = 4; // bytes per leaf free-table slot

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the free blob's first block

    /// <summary>
    /// Wraps the given memory window, sliced to the start of a free blob, as a cursor.
    /// </summary>
    public FreeBlob(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>The shared blob header at the start of this free blob.</summary>
    public BlobHeader Header => new BlobHeader(_buf);

    /// <summary>Page of the previous free blob in the same size range, or <see cref="PageIndex.Nil"/>.</summary>
    public PageIndex PrevPage => new PageIndex(_buf.Span.GetIntLE(PREV_FREE_BLOB_OFS));

    /// <summary>Page of the next free blob in the same size range, or <see cref="PageIndex.Nil"/>.</summary>
    public PageIndex NextPage => new PageIndex(_buf.Span.GetIntLE(NEXT_FREE_BLOB_OFS));

    /// <summary>Bitmask of which 16-slot ranges of this blob's leaf free-table are in use.</summary>
    public int LeafRangeBits => _buf.Span.GetIntLE(LEAF_FT_RANGE_BITS_OFS);

    /// <summary>The first page of the free-blob list for the given leaf slot, or <see cref="PageIndex.Nil"/>.</summary>
    public PageIndex LeafFreeTablePage(int slot) => new PageIndex(_buf.Span.GetIntLE(LEAF_FREE_TABLE_OFS + slot * FreeTableSlotSize));

}
