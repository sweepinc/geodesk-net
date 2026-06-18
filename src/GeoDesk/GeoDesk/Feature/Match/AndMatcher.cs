/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher</c>.</remarks>
public class AndMatcher : Matcher
{

    readonly Matcher _a;
    readonly Matcher _b;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher(Matcher, Matcher)</c>.</remarks>
    public AndMatcher(Matcher a, Matcher b) :
        base(a.AcceptedTypes() & b.AcceptedTypes())
    {
        _a = a;
        _b = b;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher.accept(ByteBuffer, int)</c>.</remarks>
    public override bool Accept(NioBuffer buf, int pos)
    {
        return _a.Accept(buf, pos) && _b.Accept(buf, pos);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher.acceptTyped(int, ByteBuffer, int)</c>.</remarks>
    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return _a.AcceptTyped(types, buf, pos) && _b.AcceptTyped(types, buf, pos);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher.acceptIndex(int)</c>.</remarks>
    public override bool AcceptIndex(int keys)
    {
        return _a.AcceptIndex(keys) && _b.AcceptIndex(keys);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher.acceptRole(int, String)</c>.</remarks>
    public override Matcher? AcceptRole(int roleCode, string? roleString)
    {
        var ma = _a.AcceptRole(roleCode, roleString);
        if (ma == null)
            return null;

        var mb = _b.AcceptRole(roleCode, roleString);
        if (mb == null)
            return null;

        return new AndMatcher(ma, mb);
    }

}
