/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

public class IdMatcher : Matcher
{
    private readonly long idBits;
    private const long TYPE_ID_MASK = unchecked((long)0xffff_ffff_ffff_f018UL);

    public IdMatcher(int typeCode, long id)
        : base(TypeBits.ALL)
    {
        idBits = (id << 12) | ((long)typeCode << 3);
        // TODO: change if FeatureFlags change
    }

    public override bool Accept(NioBuffer buf, int pos)
    {
        return (buf.GetLong(pos) & TYPE_ID_MASK) == idBits;
    }
}
