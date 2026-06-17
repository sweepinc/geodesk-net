/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using static GeoDesk.Feature.Match.TypeBits;

namespace GeoDesk.Feature.Match;

public class MatcherSet
{
    private readonly int types;
    private readonly Matcher? nodes;
    private readonly Matcher? ways;
    private readonly Matcher? areas;
    private readonly Matcher? relations;
    private readonly Matcher? members;

    public static readonly MatcherSet ALL = new MatcherSet(TypeBits.ALL, Matcher.ALL);

    public MatcherSet(int types, Matcher matcher)
    {
        this.types = types;
        nodes = ways = areas = relations = members = matcher;
    }

    public MatcherSet(int types, Matcher? n, Matcher? w, Matcher? a, Matcher? r, Matcher? m)
    {
        this.types = types;
        nodes = n;
        ways = w;
        areas = a;
        relations = r;
        members = m;
    }

    public int Types => types;

    public Matcher? Nodes => nodes;

    public Matcher? Ways => ways;

    public Matcher? Areas => areas;

    public Matcher? Relations => relations;

    public Matcher? Members => members;

    private static Matcher? MergeFilter(int newTypes, int indexType, Matcher? a, Matcher? b)
    {
        newTypes &= indexType;
        if (newTypes == 0) return null;
        Matcher filter = new AndMatcher(a!, b!);
        if (newTypes == indexType) return filter;
        return new TypeMatcher(newTypes, filter);
    }

    private static Matcher? ConstrainFilter(int newTypes, int indexType, Matcher? filter)
    {
        newTypes &= indexType;
        if (newTypes == 0) return null;
        if (newTypes == indexType) return filter;
        return new TypeMatcher(newTypes, filter!);
    }

    public MatcherSet And(int newTypes, MatcherSet other)
    {
        newTypes &= types & other.types;
        Matcher? n = MergeFilter(newTypes, NODES, nodes, other.nodes);
        Matcher? w = MergeFilter(newTypes, NONAREA_WAYS, ways, other.ways);
        Matcher? a = MergeFilter(newTypes, AREAS, areas, other.areas);
        Matcher? r = MergeFilter(newTypes, NONAREA_RELATIONS, relations, other.relations);
        Matcher m = new AndMatcher(members!, other.members!);
        return new MatcherSet(newTypes, n, w, a, r, m);
    }

    public MatcherSet And(int newTypes)
    {
        newTypes &= types;
        Matcher? n = ConstrainFilter(newTypes, NODES, nodes);
        Matcher? w = ConstrainFilter(newTypes, NONAREA_WAYS, ways);
        Matcher? a = ConstrainFilter(newTypes, AREAS, areas);
        Matcher? r = ConstrainFilter(newTypes, NONAREA_RELATIONS, relations);
        return new MatcherSet(newTypes, n, w, a, r, members);
    }
}
