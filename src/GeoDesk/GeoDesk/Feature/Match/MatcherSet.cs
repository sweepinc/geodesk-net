/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using static GeoDesk.Feature.Match.TypeBits;

namespace GeoDesk.Feature.Match;

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

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet(int, Matcher)</c>.</remarks>
    public MatcherSet(int types, Matcher matcher)
    {
        _types = types;
        _nodes = _ways = _areas = _relations = _members = matcher;
    }

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

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.types()</c>.</remarks>
    public int Types => _types;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.nodes()</c>.</remarks>
    public Matcher? Nodes => _nodes;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.ways()</c>.</remarks>
    public Matcher? Ways => _ways;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.areas()</c>.</remarks>
    public Matcher? Areas => _areas;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.relations()</c>.</remarks>
    public Matcher? Relations => _relations;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.members()</c>.</remarks>
    public Matcher? Members => _members;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.mergeFilter(int, int, Matcher, Matcher)</c>.</remarks>
    static Matcher? MergeFilter(int newTypes, int indexType, Matcher? a, Matcher? b)
    {
        newTypes &= indexType;
        if (newTypes == 0) return null;
        Matcher filter = new AndMatcher(a!, b!);
        if (newTypes == indexType) return filter;
        return new TypeMatcher(newTypes, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherSet.constrainFilter(int, int, Matcher)</c>.</remarks>
    static Matcher? ConstrainFilter(int newTypes, int indexType, Matcher? filter)
    {
        newTypes &= indexType;
        if (newTypes == 0) return null;
        if (newTypes == indexType) return filter;
        return new TypeMatcher(newTypes, filter!);
    }

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
