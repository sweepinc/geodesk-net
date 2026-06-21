/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A matcher that first restricts candidates to an accepted set of feature types (read from the
/// feature flags) and then delegates the remaining test to a wrapped inner matcher.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher</c>.</remarks>
internal class TypeMatcher : Matcher
{

    readonly Matcher _matcher;

    /// <summary>
    /// Creates a type matcher restricting to <paramref name="acceptedTypes"/> and delegating to the
    /// inner <paramref name="matcher"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher(int, Matcher)</c>.</remarks>
    public TypeMatcher(int acceptedTypes, Matcher matcher)
        : base(acceptedTypes)
    {
        _matcher = matcher;
    }

    /// <summary>
    /// Rejects the feature if its type is not in the accepted set, otherwise defers to the inner matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher.accept(ByteBuffer, int)</c>.</remarks>
    public override bool Accept(NioBuffer buf, int pos)
    {
        int flags = (sbyte)buf.Get(pos);
        var type = 1 << ((int)((uint)flags >> 1) & 0x1f);
        if ((type & acceptedTypes) == 0) return false;
        return _matcher.Accept(buf, pos);
    }

    /// <summary>
    /// Intersects the caller's types with the accepted set and delegates the typed test to the inner
    /// matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher.acceptTyped(int, ByteBuffer, int)</c>.</remarks>
    public override bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return _matcher.AcceptTyped(types & acceptedTypes, buf, pos);
    }

    /// <summary>
    /// Delegates the index-key acceptance test to the inner matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TypeMatcher.acceptIndex(int)</c>.</remarks>
    public override bool AcceptIndex(int keys)
    {
        return _matcher.AcceptIndex(keys);
    }

}
