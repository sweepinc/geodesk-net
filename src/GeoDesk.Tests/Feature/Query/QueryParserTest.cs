/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;

using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;

using Xunit;

namespace GeoDesk.Tests.Feature.Query;

public class QueryParserTest
{
    private readonly QueryParser parser;
    private readonly GlobalStringTable globalStrings;

    public QueryParserTest()
    {
        globalStrings = LoadStrings();
        parser = new QueryParser(globalStrings, null);
    }

    private static GlobalStringTable LoadStrings()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "TestResources", "feature", "strings.txt");
        var list = new List<string> { "" }; // entry 0 is the empty string
        list.AddRange(File.ReadLines(path));
        return GlobalStringTable.FromStrings(list.ToArray());
    }

    private static int CountSelectors(Selector? first)
    {
        int n = 0;
        for (Selector? s = first; s != null; s = s.Next()) n++;
        return n;
    }

    [Fact]
    public void TestQuery()
    {
        parser.Parse(
            "na[amenity=pub,bar,cafe,restaurant][local_key != 'banana']," +
            "n[emergency]," +
            "wa[maxspeed='*mph'][maxspeed < 35][maxspeed < 4][maxspeed = 10]");
        Selector? q = parser.Query();
        Assert.NotNull(q);
        Assert.Equal(3, CountSelectors(q));
        // first selector targets nodes + areas
        Assert.Equal(TypeBits.NODES | TypeBits.AREAS, q!.MatchTypes());
    }

    [Fact]
    public void TestQuery2()
    {
        parser.Parse("na[amenity=restaurant][cuisine=greek][name='Acro*','Akro*']");
        Selector? q = parser.Query();
        Assert.NotNull(q);
        Assert.Equal(1, CountSelectors(q));
        Assert.Equal(TypeBits.NODES | TypeBits.AREAS, q!.MatchTypes());
    }

    [Fact]
    public void TestSimpleClause()
    {
        parser.Parse("[amenity=pub]");
        Selector? q = parser.Query();
        Assert.NotNull(q);
        Assert.Equal(TypeBits.ALL, q!.MatchTypes());
        // amenity is a global key => key-required-implicitly => global-required clause
        Assert.True((q.ClauseTypes() & Selector.CLAUSE_GLOBAL_REQUIRED) != 0);
    }

    [Fact]
    public void TestNegatedKey()
    {
        parser.Parse("n[!amenity]");
        Selector? q = parser.Query();
        Assert.NotNull(q);
        Assert.Equal(TypeBits.NODES, q!.MatchTypes());
        // [!amenity] is optional-key
        Assert.True((q.ClauseTypes() & Selector.CLAUSE_GLOBAL_OPTIONAL) != 0);
    }

    [Fact]
    public void TestInvalidTypeThrows()
    {
        Assert.Throws<QueryException>(() =>
        {
            parser.Parse("x[amenity]");
            parser.Query();
        });
    }
}
