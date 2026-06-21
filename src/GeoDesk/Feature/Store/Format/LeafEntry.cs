/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

namespace GeoDesk.Feature.Store.Format;

/// <summary>A 32-byte R-tree leaf entry for a way / area / relation feature.</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchLeaf(int)</c>.</remarks>
internal readonly struct LeafEntry
{

    // Layout: bytes 0..15 = bbox; the embedded feature starts at +16, its first word being the flags.
    const int BoundsOfs = 0;
    const int FeatureOfs = 16;
    const int LastFlag = 1;
    const int TypeBitShift = 1; // drops the last-item flag to leave the spatial type-bit exponent
    const int TypeShift = 3;
    const int TypeMask = 3;

    /// <summary>The number of bytes a leaf entry occupies; a consumer advances by this to reach the next.</summary>
    public const int Size = 32;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the leaf entry (its bbox)

    /// <summary>Wraps the given memory window, sliced to the start of a leaf entry, as a cursor.</summary>
    public LeafEntry(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>The flags word of the embedded feature.</summary>
    public int Flags => _buf.Span.GetIntLE(FeatureOfs);

    /// <summary>True if this is the last entry in its leaf.</summary>
    public bool IsLast => (Flags & LastFlag) != 0;

    /// <summary>The entry's bounding box.</summary>
    public Bounds Bounds => new Bounds(_buf.Slice(BoundsOfs));

    /// <summary>This entry's single type bit, to AND against the query's accepted-types mask.</summary>
    public int TypeBit => 1 << (Flags >> TypeBitShift);

    /// <summary>The feature's type, from the 2-bit tag (folded into the pointer stored in QueryResults).</summary>
    public FeatureType StoredType => (FeatureType)(((uint)Flags >> TypeShift) & TypeMask);

    /// <summary>The embedded feature, anchored at its flags word.</summary>
    public FeatureHeader Feature => new FeatureHeader(_buf.Slice(FeatureOfs));

}
