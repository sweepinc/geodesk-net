/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

namespace GeoDesk.Feature.Store.Format;

/// <summary>A 20-byte R-tree trunk entry: a child bounding box plus a pointer to a child trunk or leaf.</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchTrunk(int)</c>.</remarks>
internal readonly struct TrunkEntry
{

    // Layout: word 0 = child pointer (high 30 bits) + last/leaf flags; bytes 4..19 = child bbox.
    const int ChildAndFlagsOfs = 0;
    const int BoundsOfs = 4;
    const int LastFlag = 1;
    const int LeafFlag = 2;
    const uint ChildPtrMask = 0xffff_fffc; // clears the 2 low flag bits to leave the pointer

    /// <summary>The number of bytes a trunk entry occupies; a consumer advances by this to reach the next.</summary>
    public const int Size = 20;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the trunk entry

    public TrunkEntry(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    int Word => _buf.Span.GetIntLE(ChildAndFlagsOfs);

    public bool IsLast => (Word & LastFlag) != 0;

    public bool IsLeaf => (Word & LeafFlag) != 0;

    public Bounds Bounds => new Bounds(_buf.Slice(BoundsOfs));

    /// <summary>The child node, with the flag bits cleared: a trunk if <see cref="IsLeaf"/> is false, else a leaf.</summary>
    public ReadOnlyMemory<byte> Child => _buf.Slice((int)((uint)Word & ChildPtrMask));

}
