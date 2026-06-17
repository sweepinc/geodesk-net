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
    private readonly Matcher a;
    private readonly Matcher b;

    public AndMatcher(Matcher a, Matcher b)
        : base(a.AcceptedTypes() & b.AcceptedTypes())
    {
        this.a = a;
        this.b = b;
    }

    public override bool Accept(NioBuffer buf, int pos)
    {
        return a.Accept(buf, pos) && b.Accept(buf, pos);
    }

    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return a.AcceptTyped(types, buf, pos) && b.AcceptTyped(types, buf, pos);
    }

    public override bool AcceptIndex(int keys)
    {
        return a.AcceptIndex(keys) && b.AcceptIndex(keys);
    }

    public override Matcher? AcceptRole(int roleCode, string? roleString)
    {
        Matcher? ma = a.AcceptRole(roleCode, roleString);
        if (ma == null) return null;
        Matcher? mb = b.AcceptRole(roleCode, roleString);
        if (mb == null) return null;
        return new AndMatcher(ma, mb);
    }
}
