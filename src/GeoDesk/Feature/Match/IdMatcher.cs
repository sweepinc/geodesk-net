/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A matcher that accepts the single feature with a specific type and id, by comparing the relevant
/// bits of the feature's header word directly against a precomputed pattern.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.IdMatcher</c>.</remarks>
internal class IdMatcher : Matcher
{

    readonly long _idBits;
    const long TypeIdMask = unchecked((long)0xffff_ffff_ffff_f018UL);

    /// <summary>
    /// Creates a matcher for the feature with the given type code and id, packing them into the bit
    /// pattern compared against feature headers.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.IdMatcher(int, long)</c>.</remarks>
    public IdMatcher(int typeCode, long id)
        : base(TypeBits.ALL)
    {
        _idBits = (id << 12) | ((long)typeCode << 3);
        // TODO: change if FeatureFlags change
    }

    /// <summary>
    /// Accepts the feature whose header at <paramref name="pos"/> matches the target type and id bits.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.IdMatcher.accept(ByteBuffer, int)</c>.</remarks>
    public override bool Accept(NioBuffer buf, int pos)
    {
        return (buf.GetLong(pos) & TypeIdMask) == _idBits;
    }

}
