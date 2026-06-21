/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

namespace GeoDesk.Feature.Store.Format;

/// <summary>An 8-byte index bucket: an accepted-tag key set plus the R-tree root for its features.</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchRTree(int, Matcher, RTreeQueryTask)</c>.</remarks>
internal readonly struct IndexBucket
{

    // Layout: word 0 = R-tree root pointer (high 30 bits) + last-bucket flag; word 1 = key bits.
    const int RootAndFlagsOfs = 0;
    const int KeyBitsOfs = 4;
    const int LastFlag = 1;
    const uint RootPtrMask = 0xffff_fffc; // clears the 2 low flag bits to leave the pointer

    /// <summary>The number of bytes a bucket occupies; a consumer advances by this to reach the next.</summary>
    public const int Size = 8;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the bucket entry

    /// <summary>Wraps the given memory window, sliced to the start of a bucket, as a cursor.</summary>
    public IndexBucket(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>The bucket's accepted-tag key bits, AND-tested against a query's key set.</summary>
    public int KeyBits => _buf.Span.GetIntLE(KeyBitsOfs);

    /// <summary>True if this is the last bucket in the index.</summary>
    public bool IsLast => (_buf.Span.GetIntLE(RootAndFlagsOfs) & LastFlag) != 0;

    /// <summary>The bucket's R-tree root trunk node.</summary>
    public TrunkEntry Root => new TrunkEntry(_buf.Slice((int)((uint)_buf.Span.GetIntLE(RootAndFlagsOfs) & RootPtrMask)));

}
