/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using GeoDesk.Feature.Store;

namespace GeoDesk.Feature.Match;

/// <summary>
/// Resolves a GOQL query string to a <see cref="Matcher"/>, parsing/building and caching it on first use: a
/// cache hit returns the stored matcher; a miss builds one (compiled to a delegate, or interpreted).
///
/// <para>
/// Thread-safe: cache hits are lock-free; only the (rare) build path is serialized, since it drives the
/// stateful <see cref="QueryParser"/>.
/// </para>
/// <para>
/// Memory behaviour: entries are held through <see cref="WeakReference{T}"/>, so a matcher (and its compiled
/// delegate) can be reclaimed once nothing else references it and the GC needs the space. To stop that from
/// collapsing the hit rate — the cache is otherwise the only thing referencing a matcher between queries — the
/// most recently used matchers are also pinned by strong references in a small recency ring, so the hot
/// working set survives collections while cold entries age out under memory pressure.
/// </para>
///
/// PORT NOTE: the Java original generates JVM bytecode (via ASM) per query. This port compiles the query to
/// a delegate via a LINQ expression tree (<see cref="ExpressionMatcherCoder"/>), or interprets it with
/// <see cref="AstTagMatcher"/> when dynamic code is unavailable.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler</c>.</remarks>
internal class MatcherCompiler
{

    // How many most-recently-used matchers to pin with strong references so they survive GC.
    internal const int DefaultRecentCapacity = 256;

    readonly QueryParser _parser;
    readonly GlobalStringTable _globalStrings;

    // Serializes the build path — NOT the cache (the ConcurrentDictionary handles concurrent access itself).
    // It exists only because QueryParser is a single shared, stateful instance: Parse()/Query() mutate it, so
    // two threads cannot build concurrently.
    //
    // PERF: consequently every cache miss across the whole store is serialized through this one lock, so
    // matcher builds cannot run in parallel under concurrent load with many distinct queries. The fix is to
    // remove the shared mutable state — a stateless parser, or a pooled / per-build parser instance — which
    // would let this lock go away entirely. (Builds are rare once the cache is warm, so this is deferred.)
    readonly object _compileLock = new object();
    readonly ConcurrentDictionary<string, WeakReference<Matcher>> _matchers =
        new ConcurrentDictionary<string, WeakReference<Matcher>>();

    // Strong-reference ring of the most-recently-used matchers (the recency anchor). Reference writes are
    // atomic, so it needs no lock; overwrites simply let the displaced matcher fall back to weak-only.
    readonly Matcher[] _recent;
    int _recentSlot = -1;

    // Prune dead weak entries once the map grows well past the strong working set, to bound key/wrapper churn.
    readonly int _pruneThreshold;

    /// <summary>
    /// Creates a cache over the given global string table and key-to-category map, resolving the code for the
    /// special value <c>"no"</c> (which must be present) and building the query parser.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler(ObjectIntMap, String[], IntIntMap)</c>.</remarks>
    public MatcherCompiler(GlobalStringTable globalStrings, IReadOnlyDictionary<int, int> keysToCategories)
        : this(globalStrings, keysToCategories, DefaultRecentCapacity)
    {
    }

    /// <summary>
    /// Port-only overload that makes the recency-anchor size injectable, so tests can exercise aging-out with
    /// a small working set.
    /// </summary>
    internal MatcherCompiler(GlobalStringTable globalStrings,
        IReadOnlyDictionary<int, int> keysToCategories, int recentCapacity)
    {
        _globalStrings = globalStrings;
        _parser = new QueryParser(globalStrings, keysToCategories);
        _recent = new Matcher[Math.Max(1, recentCapacity)];
        _pruneThreshold = Math.Max(_recent.Length * 4, 1024);
    }

    /// <summary>
    /// Returns the matcher for the given query string, building and caching it on first use. Safe to call
    /// concurrently: a cache hit takes no lock; a miss serializes on the build path (the parser is stateful)
    /// and double-checks the cache before building.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler.getMatcher(String)</c>.</remarks>
    public Matcher GetMatcher(string query)
    {
        if (TryGetLive(query, out var matcher))
            return Touch(matcher);

        lock (_compileLock)
        {
            if (TryGetLive(query, out matcher))
                return Touch(matcher);

            matcher = CreateMatcher(query);
            _matchers[query] = new WeakReference<Matcher>(matcher);
            if (_matchers.Count > _pruneThreshold)
                PruneDead();
            return Touch(matcher);
        }
    }

    /// <summary>Looks up a still-live cached matcher, treating a collected entry as a miss.</summary>
    bool TryGetLive(string query, out Matcher matcher)
    {
        if (_matchers.TryGetValue(query, out var weak) && weak.TryGetTarget(out matcher!))
            return true;
        matcher = null!;
        return false;
    }

    /// <summary>Records the matcher as recently used by pinning it in the recency ring, then returns it.</summary>
    Matcher Touch(Matcher matcher)
    {
        var slot = Interlocked.Increment(ref _recentSlot) & int.MaxValue;
        _recent[slot % _recent.Length] = matcher;
        return matcher;
    }

    /// <summary>Removes entries whose matcher has been collected. Called under <see cref="_compileLock"/>.</summary>
    void PruneDead()
    {
        foreach (var entry in _matchers)
            if (!entry.Value.TryGetTarget(out _))
                _matchers.TryRemove(entry);
    }

    /// <summary>
    /// Parses the query string and builds a fresh matcher for it — the uncached path invoked by
    /// <see cref="GetMatcher"/> on a cache miss.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler.createMatcher(String)</c>.</remarks>
    Matcher CreateMatcher(string query)
    {
        _parser.Parse(query);

        var selectors = _parser.Query();
        if (selectors == null)
            return Matcher.ALL;

        var sel = selectors;
        var commonType = 0;
        while (sel != null)
        {
            var type = sel.MatchTypes();
            if (commonType == 0)
                commonType = type;
            else if (type != commonType)
                // Faithful to upstream: geodesk's MatcherCompiler throws this same message and keeps its
                // createPolyformMatchers/MatcherSet path commented out. Polyform is unfinished there too.
                throw new QueryException("Polyform queries are not supported.");

            sel = sel.Next();
        }

        // Compile the query to a delegate when possible; fall back to interpreting it otherwise.
        return ExpressionMatcherCoder.TryCompile(selectors, _globalStrings) ?? new AstTagMatcher(selectors, _globalStrings);
    }

    /// <summary>Port-only: number of cached entries whose matcher is still live, for tests asserting aging-out.</summary>
    internal int LiveCount
    {
        get
        {
            var n = 0;
            foreach (var entry in _matchers)
                if (entry.Value.TryGetTarget(out _))
                    n++;
            return n;
        }
    }

}
