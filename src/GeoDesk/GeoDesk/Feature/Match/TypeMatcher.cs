/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher</c>.</remarks>
internal class TypeMatcher : Matcher
{

    readonly Matcher _matcher;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher(int, Matcher)</c>.</remarks>
    public TypeMatcher(int acceptedTypes, Matcher matcher)
        : base(acceptedTypes)
    {
        _matcher = matcher;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher.accept(ByteBuffer, int)</c>.</remarks>
    public override bool Accept(NioBuffer buf, int pos)
    {
        int flags = (sbyte)buf.Get(pos);
        var type = 1 << ((int)((uint)flags >> 1) & 0x1f);
        if ((type & acceptedTypes) == 0) return false;
        return _matcher.Accept(buf, pos);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher.acceptTyped(int, ByteBuffer, int)</c>.</remarks>
    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return _matcher.AcceptTyped(types & acceptedTypes, buf, pos);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher.acceptIndex(int)</c>.</remarks>
    public override bool AcceptIndex(int keys)
    {
        return _matcher.AcceptIndex(keys);
    }

}
