/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Linq;

using BenchmarkDotNet.Attributes;

using GeoDesk.Feature;
using GeoDesk.Geom;

namespace GeoDesk.Benchmarks;

/// <summary>
/// The standalone "bridges across the Danube in Bavaria" benchmark: a chained spatial query that finds the
/// highway bridges intersecting both Bavaria (a state polygon) and the Danube (a river relation). Requires a
/// dataset that contains Bavaria and the Danube (i.e. Germany).
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.benchmark.BridgesBenchmark</c>.</remarks>
[MemoryDiagnoser]
public class BridgesBenchmark
{

    FeatureLibrary _lib = null!;
    IFeature _bavaria = null!;
    IFeature _danube = null!;
    IFeatureQuery _bridges = null!;

    /// <summary>Opens the GOL and resolves the Bavaria boundary, the Danube relation, and the bridge query.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _lib = FeatureLibrary.Open(GolFixture.Resolve());

        _bavaria = _lib
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .In(Box.AtLonLat(12.0231, 48.3310))
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Bavaria not found in this dataset (need a Germany extract).");

        _danube = _lib
            .Select("r[waterway=river][name:en=Danube]")
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Danube not found in this dataset (need a Germany extract).");

        _bridges = _lib.Select("w[highway][bridge]");
    }

    /// <summary>Closes the GOL after the run.</summary>
    [GlobalCleanup]
    public void Cleanup() => _lib.Close();

    /// <summary>Counts the highway bridges that intersect both Bavaria and the Danube.</summary>
    [Benchmark]
    public long BridgesAcrossDanubeInBavaria() =>
        _bridges.Intersecting(_bavaria).Intersecting(_danube).Count();

}
