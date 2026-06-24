/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Linq;

using BenchmarkDotNet.Attributes;

using GeoDesk.Feature;

namespace GeoDesk.Benchmarks;

/// <summary>
/// End-to-end query benchmarks over a real GOL. Each benchmark builds a fresh query and fully
/// enumerates it (<c>Count()</c>), so it measures the whole path — tile walk, parallel tile scan,
/// the R-tree descent, and the per-feature tag matcher (the part the <c>FindTagGlobal</c> early-exit
/// affects). The multi-clause cases (e.g. <see cref="WaysHighwayMaxspeed"/>) are the ones most
/// sensitive to tag-table scan cost, since each clause performs its own lookup.
/// </summary>
[MemoryDiagnoser]
public class QueryBenchmarks
{

    IFeatureQuery _world = null!;
    FeatureLibrary _lib = null!;

    /// <summary>Opens the GOL once for the whole run.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _lib = FeatureLibrary.Open(GolFixture.Resolve());
        _world = _lib;
    }

    /// <summary>Closes the GOL after the run.</summary>
    [GlobalCleanup]
    public void Cleanup()
    {
        _lib.Close();
    }

    /// <summary>Baseline: every node, matched by the universal matcher (no tag conditions).</summary>
    [Benchmark(Baseline = true)]
    public long AllNodes() => _world.Nodes().Count();

    /// <summary>Single global-key clause: ways tagged <c>highway</c>.</summary>
    [Benchmark]
    public long WaysHighway() => _world.Ways("w[highway]").Count();

    /// <summary>Two global-key clauses: ways tagged <c>highway</c> and <c>maxspeed</c>.</summary>
    [Benchmark]
    public long WaysHighwayMaxspeed() => _world.Ways("w[highway][maxspeed]").Count();

    /// <summary>Value comparison on a heavily-tagged set: buildings (areas) tagged <c>building</c>.</summary>
    [Benchmark]
    public long Buildings() => _world.Select("a[building]").Count();

}
