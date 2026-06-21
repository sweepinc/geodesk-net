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
/// The 4-byte header word every blob begins with: the payload size in the low 30 bits, plus the
/// "this blob is free" and "the preceding blob is free" marker bits.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore</c> (the <c>sizeAndFlags</c> reads
/// at a blob's offset 0).</remarks>
internal readonly struct BlobHeader
{

    const int HeaderWordOfs = 0;

    readonly ReadOnlyMemory<byte> _buf;

    /// <summary>
    /// Wraps the given memory window, sliced to the start of a blob, as a header cursor.
    /// </summary>
    public BlobHeader(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>
    /// The raw 32-bit header word, exactly as stored: payload size in the low 30 bits, the
    /// preceding-free and free marker flags in bits 30 and 31.
    /// </summary>
    public int Word => _buf.Span.GetIntLE(HeaderWordOfs);

    /// <summary>
    /// The blob's payload size in bytes (excludes the 4-byte header).
    /// </summary>
    public int PayloadSize => Word & PAYLOAD_SIZE_MASK;

    /// <summary>
    /// The marker bits above the payload size (the free / preceding-free flags), as stored.
    /// </summary>
    public int Flags => Word & ~PAYLOAD_SIZE_MASK;

    /// <summary>
    /// True if this blob is on the free list (the free marker, bit 31).
    /// </summary>
    public bool IsFree => (Word & FREE_BLOB_FLAG) != 0;

    /// <summary>
    /// True if the blob immediately preceding this one in the file is free (the preceding-free
    /// marker, bit 30). Used to coalesce adjacent free blobs.
    /// </summary>
    public bool IsPrecedingFree => (Word & PRECEDING_BLOB_FREE_FLAG) != 0;

}
