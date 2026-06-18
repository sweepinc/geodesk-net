/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

// In Java this is abstract (subclasses are generated as bytecode). Here it is a concrete
// base so Matcher.ALL can be instantiated directly; the hand-written matcher subclasses
// (IdMatcher, TypeMatcher, AndMatcher, …) extend it as usual.
/// <remarks>Ported from Java <c>com.geodesk.feature.match.Matcher</c>.</remarks>
public class Matcher
{

    public static readonly Matcher ALL = new Matcher(TypeBits.ALL);

    protected readonly int acceptedTypes;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Matcher(int)</c>.</remarks>
    public Matcher(int types)
    {
        acceptedTypes = types;
    }

    /// <summary>
    /// Checks whether a feature meets the conditions of this Matcher.
    /// </summary>
    /// <param name="buf">the Buffer of the feature</param>
    /// <param name="pos">the anchor position of the feature in the Buffer</param>
    /// <returns><c>true</c> if the feature matches the filter condition</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Matcher.accept(ByteBuffer, int)</c>.</remarks>
    public virtual bool Accept(NioBuffer buf, int pos)
    {
        return true;
    }

    /// <summary>
    /// Accepts this feature only if its type matches the given type mask.
    /// </summary>
    /// <param name="types">the type mask to match (must not be 0)</param>
    /// <param name="buf">the Buffer of the feature</param>
    /// <param name="pos">the anchor position of the feature in the Buffer</param>
    /// <returns><c>true</c> if the feature matches the filter condition</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Matcher.acceptTyped(int, ByteBuffer, int)</c>.</remarks>
    public virtual bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return (types & (1 << ((sbyte)buf.Get(pos) >> 1))) != 0;
    }

    /// <summary>
    /// Checks whether this Matcher might be fulfilled by features that are stored in an index with the
    /// given key bits.
    /// </summary>
    /// <param name="keys">the key bits of the index</param>
    /// <returns>
    /// <c>true</c> if features that match this Matcher might be found in the given index, or
    /// <c>false</c> if none of those features could be a match
    /// </returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Matcher.acceptIndex(int)</c>.</remarks>
    public virtual bool AcceptIndex(int keys)
    {
        return true;
    }

    /// <summary>
    /// Checks whether relation members with the given role are accepted by this Matcher. If so,
    /// returns a Matcher that can be applied to each individual member with the same role.
    /// </summary>
    /// <param name="roleCode">
    /// the global-string code of the role; a negative value indicates that the role is passed as
    /// <paramref name="roleString"/>
    /// </param>
    /// <param name="roleString">the role (only valid if <paramref name="roleCode"/> is negative)</param>
    /// <returns>
    /// a <c>Matcher</c> to be applied to member features with the given role, or <c>null</c> if
    /// members with this role are not accepted
    /// </returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Matcher.acceptRole(int, String)</c>.</remarks>
    public virtual Matcher? AcceptRole(int roleCode, string? roleString)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Matcher.acceptedTypes()</c>.</remarks>
    public int AcceptedTypes()
    {
        return acceptedTypes;
    }

}
