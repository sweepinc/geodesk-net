/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.IdMatcher</c>.</remarks>
public class IdMatcher : Matcher
{

    readonly long _idBits;
    const long TypeIdMask = unchecked((long)0xffff_ffff_ffff_f018UL);

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.IdMatcher(int, long)</c>.</remarks>
    public IdMatcher(int typeCode, long id)
        : base(TypeBits.ALL)
    {
        _idBits = (id << 12) | ((long)typeCode << 3);
        // TODO: change if FeatureFlags change
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.IdMatcher.accept(ByteBuffer, int)</c>.</remarks>
    public override bool Accept(NioBuffer buf, int pos)
    {
        return (buf.GetLong(pos) & TypeIdMask) == _idBits;
    }

}
