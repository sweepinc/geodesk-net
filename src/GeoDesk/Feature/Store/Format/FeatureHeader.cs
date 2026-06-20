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
/// The shared, forward part of a feature record, read from its anchor pointer (the flags word). Every
/// feature type lays this out identically: flags / id at offset 0 and the tag-table pointer at +8. The
/// body pointer (+12) is deliberately not exposed here — the body is the feature's payload, not part of
/// its header, and is resolved by the body-specific code instead.
/// </summary>
/// <remarks>
/// The cursor is sliced to the anchor and only ever reads (and slices) forward. The geometry header
/// — the bbox (ways/areas/relations) or x/y (nodes) — sits at <em>negative</em> offsets and is a
/// variable-size, type-discriminated sibling; positioning a <see cref="Bounds"/> there is the job of
/// the owner that holds the full segment memory and knows the type, not of this cursor.
/// <para>Ported from Java <c>com.geodesk.feature.store.StoredFeature</c> (the <c>ptr</c> and
/// <c>ptr+8</c> accessors).</para>
/// </remarks>
internal readonly struct FeatureHeader
{

    // Layout: flags/id word at 0; relative pointer to the tag table at +8.
    const int FlagsOfs = 0;
    const int IdShift = 12; // the id occupies the high 52 bits of the flags/id word
    const int TypeShift = 3; // the 2-bit feature type code sits at bits 3..4
    const int TypeMask = 3;
    const int TagTablePpOfs = 8;
    const int UncommonKeysFlag = 1; // low bit of the tag-table pointer

    readonly ReadOnlyMemory<byte> _buf; // sliced to the feature anchor (the flags word)

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="buf"></param>
    public FeatureHeader(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <remarks>Ported from Java <c>StoredFeature.flags()</c>.</remarks>
    public int Flags => _buf.Span.GetIntLE(FlagsOfs);

    /// <remarks>Ported from Java <c>StoredFeature.id(ByteBuffer, int)</c>.</remarks>
    public long Id => (long)((ulong)_buf.Span.GetLongLE(FlagsOfs) >> IdShift);

    /// <remarks>Ported from Java <c>StoredFeature.typeCode(ByteBuffer, int)</c> (the 2-bit code, as the enum).</remarks>
    public FeatureType Type => (FeatureType)((Flags >> TypeShift) & TypeMask);

    /// <remarks>Ported from Java <c>StoredFeature.isArea()</c>.</remarks>
    public bool IsArea => (Flags & FeatureFlags.AREA_FLAG) != 0;

    /// <remarks>Ported from Java <c>StoredFeature.belongsToRelation()</c>.</remarks>
    public bool BelongsToRelation => (Flags & FeatureFlags.RELATION_MEMBER_FLAG) != 0;

    /// <summary>True if the tag table carries uncommon (non-builtin) keys, encoded in the pointer's low bit.</summary>
    public bool HasUncommonKeys => (_buf.Span.GetIntLE(TagTablePpOfs) & UncommonKeysFlag) != 0;

    /// <summary>The tag table, with the uncommon-keys flag stripped from the relative pointer.</summary>
    public ReadOnlyMemory<byte> TagTable
    {
        get
        {
            var raw = _buf.Span.GetIntLE(TagTablePpOfs);
            var uncommonKeysFlag = raw & UncommonKeysFlag;
            return _buf.Slice(TagTablePpOfs + (raw ^ uncommonKeysFlag));
        }
    }

}
