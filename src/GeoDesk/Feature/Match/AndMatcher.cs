/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A matcher that accepts a feature only when both of its two child matchers accept it; the
/// conjunction also intersects their accepted-type sets and role handling.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher</c>.</remarks>
internal class AndMatcher : Matcher
{

    readonly Matcher _a;
    readonly Matcher _b;

    /// <summary>
    /// Creates a conjunction of two matchers, accepting the intersection of their accepted types.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher(Matcher, Matcher)</c>.</remarks>
    public AndMatcher(Matcher a, Matcher b) :
        base(a.AcceptedTypes & b.AcceptedTypes)
    {
        _a = a;
        _b = b;
    }

    /// <summary>
    /// Accepts the feature only if both child matchers accept it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher.accept(ByteBuffer, int)</c>.</remarks>
    public override bool Accept(NioBuffer buf, int pos)
    {
        return _a.Accept(buf, pos) && _b.Accept(buf, pos);
    }

    /// <summary>
    /// Accepts the typed feature only if both child matchers accept it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher.acceptTyped(int, ByteBuffer, int)</c>.</remarks>
    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return _a.AcceptTyped(types, buf, pos) && _b.AcceptTyped(types, buf, pos);
    }

    /// <summary>
    /// Accepts an index key set only if both child matchers accept it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.AndMatcher.acceptIndex(int)</c>.</remarks>
    public override bool AcceptIndex(int keys)
    {
        return _a.AcceptIndex(keys) && _b.AcceptIndex(keys);
    }

    /// <summary>
    /// Combines the role-specialized matchers from both children: returns a new <see cref="AndMatcher"/>
    /// when both children accept the role, or null if either rejects it.
    /// </summary>
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
