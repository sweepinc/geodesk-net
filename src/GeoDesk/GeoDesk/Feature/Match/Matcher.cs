/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

// In Java this is abstract (subclasses are generated as bytecode). Here it is a concrete
// base so Matcher.ALL can be instantiated directly; the hand-written matcher subclasses
// (IdMatcher, TypeMatcher, AndMatcher, …) extend it as usual.
public class Matcher
{
    protected readonly int acceptedTypes;

    public Matcher(int types)
    {
        acceptedTypes = types;
    }

    /// <summary>
    /// Checks whether a feature meets the conditions of this Matcher.
    /// </summary>
    public virtual bool Accept(NioBuffer buf, int pos)
    {
        return true;
    }

    /// <summary>
    /// Accepts this feature only if its type matches the given type mask.
    /// </summary>
    public virtual bool AcceptTyped(int types, NioBuffer buf, int pos)
    {
        return (types & (1 << ((sbyte)buf.Get(pos) >> 1))) != 0;
    }

    /// <summary>
    /// Checks whether this Matcher might be fulfilled by features stored in an index
    /// with the given key bits.
    /// </summary>
    public virtual bool AcceptIndex(int keys)
    {
        return true;
    }

    /// <summary>
    /// Checks whether relation members with the given role are accepted by this Matcher.
    /// </summary>
    public virtual Matcher? AcceptRole(int roleCode, string? roleString)
    {
        return this;
    }

    public int AcceptedTypes()
    {
        return acceptedTypes;
    }

    public static readonly Matcher ALL = new Matcher(TypeBits.ALL);
}
