/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using static GeoDesk.Feature.Match.TypeBits;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A bundle of per-feature-type matchers (separate matchers for nodes, ways, areas, relations, and
/// relation members) together with the overall accepted-type bitmask. Supports combining sets with
/// logical AND while keeping the type partitioning, used to compile a query into type-specialized
/// matchers.
///
/// <para>
/// NOT CURRENTLY USED — and that faithfully mirrors upstream, it is not port drift. This is the vehicle
/// for polyform (multi-type) queries, which are disabled in geodesk itself: the only thing that builds a
/// <c>MatcherSet</c> is <c>createPolyformMatchers</c>, whose sole caller is commented out in upstream
/// <c>MatcherCompiler</c>, which throws "Polyform queries are not supported." instead — exactly as
/// <c>MatcherCompiler.CreateMatcher</c> does here. Kept (together with <see cref="TypeMatcher"/>) so the
/// scaffolding is in place if polyform is ever finished, matching upstream.
/// </para>
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet</c>.</remarks>
internal class MatcherSet
{

    public static readonly MatcherSet ALL = new MatcherSet(TypeBits.ALL, Matcher.ALL);

    readonly int _types;
    readonly Matcher? _nodes;
    readonly Matcher? _ways;
    readonly Matcher? _areas;
    readonly Matcher? _relations;
    readonly Matcher? _members;

    /// <summary>
    /// Creates a set that applies the same matcher to every feature type.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet(int, Matcher)</c>.</remarks>
    public MatcherSet(int types, Matcher matcher)
    {
        _types = types;
        _nodes = _ways = _areas = _relations = _members = matcher;
    }

    /// <summary>
    /// Creates a set with distinct matchers for nodes, ways, areas, relations, and members.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet(int, Matcher, Matcher, Matcher, Matcher, Matcher)</c>.</remarks>
    public MatcherSet(int types, Matcher? n, Matcher? w, Matcher? a, Matcher? r, Matcher? m)
    {
        _types = types;
        _nodes = n;
        _ways = w;
        _areas = a;
        _relations = r;
        _members = m;
    }

    /// <summary>
    /// The bitmask of feature types this set as a whole can accept.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.types()</c>.</remarks>
    public int Types => _types;

    /// <summary>
    /// The matcher applied to node features, or null if nodes are excluded.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.nodes()</c>.</remarks>
    public Matcher? Nodes => _nodes;

    /// <summary>
    /// The matcher applied to non-area way features, or null if such ways are excluded.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.ways()</c>.</remarks>
    public Matcher? Ways => _ways;

    /// <summary>
    /// The matcher applied to area features, or null if areas are excluded.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.areas()</c>.</remarks>
    public Matcher? Areas => _areas;

    /// <summary>
    /// The matcher applied to non-area relation features, or null if such relations are excluded.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.relations()</c>.</remarks>
    public Matcher? Relations => _relations;

    /// <summary>
    /// The matcher applied to relation members, or null if members are not matched.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.members()</c>.</remarks>
    public Matcher? Members => _members;

    /// <summary>
    /// Builds the combined matcher for one type slot by ANDing two matchers, restricted to the
    /// intersection of the requested types and that slot's type, or null if nothing remains. Wraps the
    /// result in a <see cref="TypeMatcher"/> when a partial type restriction is needed.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.mergeFilter(int, int, Matcher, Matcher)</c>.</remarks>
    static Matcher? MergeFilter(int newTypes, int indexType, Matcher? a, Matcher? b)
    {
        newTypes &= indexType;
        if (newTypes == 0) return null;
        Matcher filter = new AndMatcher(a!, b!);
        if (newTypes == indexType) return filter;
        return new TypeMatcher(newTypes, filter);
    }

    /// <summary>
    /// Restricts a single type slot's matcher to the intersection of the requested types and that
    /// slot's type, returning null if empty or wrapping it in a <see cref="TypeMatcher"/> for a partial
    /// restriction.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.constrainFilter(int, int, Matcher)</c>.</remarks>
    static Matcher? ConstrainFilter(int newTypes, int indexType, Matcher? filter)
    {
        newTypes &= indexType;
        if (newTypes == 0) return null;
        if (newTypes == indexType) return filter;
        return new TypeMatcher(newTypes, filter!);
    }

    /// <summary>
    /// Returns a new matcher set that is the logical AND of this set and <paramref name="other"/>,
    /// restricted to the given types and combining each per-type matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.and(int, MatcherSet)</c>.</remarks>
    public MatcherSet And(int newTypes, MatcherSet other)
    {
        newTypes &= _types & other._types;
        var n = MergeFilter(newTypes, NODES, _nodes, other._nodes);
        var w = MergeFilter(newTypes, NONAREA_WAYS, _ways, other._ways);
        var a = MergeFilter(newTypes, AREAS, _areas, other._areas);
        var r = MergeFilter(newTypes, NONAREA_RELATIONS, _relations, other._relations);
        Matcher m = new AndMatcher(_members!, other._members!);
        return new MatcherSet(newTypes, n, w, a, r, m);
    }

    /// <summary>
    /// Returns a new matcher set restricting this one to the given feature types, narrowing each
    /// per-type matcher accordingly while keeping the member matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.and(int)</c>.</remarks>
    public MatcherSet And(int newTypes)
    {
        newTypes &= _types;
        var n = ConstrainFilter(newTypes, NODES, _nodes);
        var w = ConstrainFilter(newTypes, NONAREA_WAYS, _ways);
        var a = ConstrainFilter(newTypes, AREAS, _areas);
        var r = ConstrainFilter(newTypes, NONAREA_RELATIONS, _relations);
        return new MatcherSet(newTypes, n, w, a, r, _members);
    }

}
