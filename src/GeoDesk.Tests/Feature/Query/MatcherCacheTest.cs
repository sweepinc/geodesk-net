/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;

using Xunit;

namespace GeoDesk.Tests.Feature.Query;

/// <summary>
/// Exercises <see cref="MatcherCompiler"/> directly: per-query caching, aging-out of cold matchers (the strong
/// recency anchor plus weak overflow), and concurrent access (the lock-free hit path plus the serialized
/// build path over the stateful parser).
/// </summary>
public class MatcherCacheTest
{
    private readonly string[] globalStrings;
    private readonly Dictionary<string, int> stringsToCodes;
    private readonly GlobalStringTable globalStringTable;

    public MatcherCacheTest()
    {
        (globalStrings, stringsToCodes) = LoadStrings();
        globalStringTable = GlobalStringTable.FromStrings(globalStrings);
    }

    private static string ResourcePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "TestResources", "feature", name);

    private static (string[], Dictionary<string, int>) LoadStrings()
    {
        var list = new List<string> { "" };
        var map = new Dictionary<string, int>();
        foreach (string s in File.ReadLines(ResourcePath("strings.txt")))
        {
            map[s] = list.Count; // 1-based
            list.Add(s);
        }
        return (list.ToArray(), map);
    }

    private MatcherCompiler NewCache(int recentCapacity = MatcherCompiler.DefaultRecentCapacity) =>
        new MatcherCompiler(globalStringTable, new Dictionary<int, int>(), recentCapacity);

    [Fact]
    public void CachesOneMatcherPerQuery()
    {
        var cache = NewCache();

        var a1 = cache.GetMatcher("w[highway=primary]");
        var a2 = cache.GetMatcher("w[highway=primary]");
        Assert.Same(a1, a2); // repeated query → the same cached instance (pinned by the recency anchor)

        var b = cache.GetMatcher("w[highway=residential]");
        Assert.NotSame(a1, b); // a different query → a distinct matcher
    }

    [Fact]
    public void ColdMatchersAgeOut()
    {
        const int recent = 8;
        var cache = NewCache(recent);

        // Insert many more distinct queries than the recency anchor holds, retaining no references.
        for (int i = 0; i < recent * 20; i++)
            cache.GetMatcher($"*[highway={i}]");

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Only the recency-anchored matchers keep a strong reference; the rest are reclaimed.
        Assert.True(cache.LiveCount <= recent,
            $"{cache.LiveCount} matchers still live, expected at most the recency anchor ({recent})");
    }

    [Fact]
    public void GetMatcherIsThreadSafe()
    {
        // All corpus queries that compile to a delegate (so the matchers are pure and safe to share).
        var queries = new[]
        {
            "w[highway=primary]",
            "*[name=\"*ap*\"]",
            "*[name!=\"*ap*\"]",
            "w[name~\".*str.*\", \".*house$\"]",
            "w[highway][name=\"Löwengrube\"]",
            "w[highway=motorway,\"monkeys&bananas\",\"applecherry\"]",
        };
        var caseNames = new[] { "highway3", "highway_residential", "highway_motorway", "long_name" };

        var tester = new GeoDesk.Feature.Query.TagTableTester();
        var segments = caseNames.Select(n => (name: n, seg: tester.MakeCase(n, 0, null))).ToList();
        try
        {
            // Reference results, computed single-threaded.
            var reference = NewCache();
            var expected = new Dictionary<(string Query, string Case), bool>();
            foreach (var q in queries)
            {
                var m = reference.GetMatcher(q);
                foreach (var (name, seg) in segments)
                    expected[(q, name)] = m.Accept(seg, 0);
            }

            var failures = new ConcurrentQueue<string>();
            // A fresh cache each round forces every query's build path to be raced concurrently,
            // which is where the stateful parser could be corrupted if the locking were wrong.
            for (int round = 0; round < 25; round++)
            {
                var cache = NewCache();
                Parallel.For(0, 256, i =>
                {
                    var q = queries[i % queries.Length];
                    Matcher m;
                    try
                    {
                        m = cache.GetMatcher(q);
                    }
                    catch (Exception e)
                    {
                        failures.Enqueue($"GetMatcher threw for [{q}]: {e}");
                        return;
                    }

                    foreach (var (name, seg) in segments)
                    {
                        var actual = m.Accept(seg, 0);
                        if (actual != expected[(q, name)])
                            failures.Enqueue($"[{q}] on '{name}': expected {expected[(q, name)]}, got {actual}");
                    }
                });
            }

            Assert.True(failures.IsEmpty, string.Join("\n", failures));
        }
        finally
        {
            foreach (var (_, seg) in segments)
                ((IDisposable)seg).Dispose();
        }
    }
}
