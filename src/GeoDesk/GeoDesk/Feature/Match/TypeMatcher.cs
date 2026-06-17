/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

public class TypeMatcher : Matcher
{
    private readonly Matcher matcher;

    public TypeMatcher(int acceptedTypes, Matcher matcher)
        : base(acceptedTypes)
    {
        this.matcher = matcher;
    }

    public override bool Accept(NioBuffer buf, int pos)
    {
        int flags = (sbyte)buf.Get(pos);
        int type = 1 << ((int)((uint)flags >> 1) & 0x1f);
        if ((type & acceptedTypes) == 0) return false;
        return matcher.Accept(buf, pos);
    }

    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return matcher.AcceptTyped(types & acceptedTypes, buf, pos);
    }

    public override bool AcceptIndex(int keys)
    {
        return matcher.AcceptIndex(keys);
    }
}
