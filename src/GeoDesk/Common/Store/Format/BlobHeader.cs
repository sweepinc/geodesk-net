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

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the blob (its header word)

    public BlobHeader(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    public int Raw => _buf.Span.GetIntLE(HeaderWordOfs);

    /// <summary>The blob's payload size in bytes (excludes the 4-byte header).</summary>
    public int PayloadSize => Raw & PAYLOAD_SIZE_MASK;

    public bool IsFree => (Raw & FREE_BLOB_FLAG) != 0;

    public bool IsPrecedingFree => (Raw & PRECEDING_BLOB_FREE_FLAG) != 0;

}
