/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using GeoDesk.Feature;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

namespace GeoDesk.Benchmarks;

/// <summary>
/// The spatial query suite ported from upstream geodesk: each case selects a named query, restricts it by a
/// spatial relation (bbox / intersects / within / enclosing) over a batch of pre-generated, cached shapes,
/// and applies an action (count / name / length / tags) to every result. One measured operation processes
/// the whole batch — BenchmarkDotNet handles repetition and statistics, while the batch size (the upstream
/// "count") controls spatial coverage and the memory regime, not timing.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.benchmark.QueryBenchmark</c> and its spatial subclasses.</remarks>
[MemoryDiagnoser]
public class SpatialQueryBenchmarks
{

    /// <summary>The full case set (the upstream <c>benchmarks</c> list); BDN runs one row per case.</summary>
    public static IEnumerable<BenchmarkCase> Cases => Plan.Cases();

    /// <summary>The case BenchmarkDotNet is currently measuring.</summary>
    [ParamsSource(nameof(Cases))]
    public BenchmarkCase Case { get; set; } = null!;

    FeatureLibrary _lib = null!;
    IFeatureQuery _view = null!;
    List<Box>? _boxes;
    List<Geometry>? _polygons;
    List<Circle>? _circles;

    /// <summary>Opens the GOL and loads (or generates) the current case's shape batch.</summary>
    [GlobalSetup]
    public void Setup()
    {
        var gol = GolFixture.Resolve();
        _lib = FeatureLibrary.Open(gol);
        _view = _lib.Select(Case.Query);

        var maker = new ShapeMaker(_lib, gol);
        switch (Case.Spatial)
        {
            case Spatial.Bbox:
                _boxes = maker.Boxes(Case.ShapeSet);
                break;
            case Spatial.Enclosing:
                _circles = maker.Circles(Case.ShapeSet);
                break;
            default: // Intersects / Within
                _polygons = maker.Polygons(Case.ShapeSet);
                break;
        }
    }

    /// <summary>Closes the GOL after the run.</summary>
    [GlobalCleanup]
    public void Cleanup() => _lib.Close();

    /// <summary>
    /// Runs the case's spatial query against every shape in the batch, applying the action to each result.
    /// Returns a checksum of the work so the JIT cannot elide it.
    /// </summary>
    [Benchmark]
    public long Run()
    {
        var acc = default(Accumulator);
        switch (Case.Spatial)
        {
            case Spatial.Bbox:
                foreach (var box in _boxes!)
                    Apply(Case.Action, _view.In(box), ref acc);
                break;
            case Spatial.Enclosing:
                foreach (var circle in _circles!)
                    Apply(Case.Action, _view.ContainingXY(circle.X, circle.Y), ref acc);
                break;
            case Spatial.Intersects:
                foreach (var geom in _polygons!)
                    Apply(Case.Action, _view.Intersecting(geom), ref acc);
                break;
            case Spatial.Within:
                foreach (var geom in _polygons!)
                    Apply(Case.Action, _view.Within(geom), ref acc);
                break;
        }
        return acc.Count + (long)acc.Result;
    }

    // Applies the action to one query result set, accumulating into the run's checksum. Mirrors the
    // upstream QueryBenchmark.Action implementations (count / name / length / tags).
    static void Apply(BenchAction action, IFeatureQuery view, ref Accumulator acc)
    {
        switch (action)
        {
            case BenchAction.Count:
                foreach (var _ in view)
                    acc.Count++;
                break;

            case BenchAction.Name:
                foreach (var f in view)
                {
                    acc.Result += (f.Tag("name") ?? string.Empty).Length;
                    acc.Count++;
                }
                break;

            case BenchAction.Length:
                foreach (var f in view)
                {
                    acc.Result += f.Length;
                    acc.Count++;
                }
                break;

            case BenchAction.Tags:
                foreach (var f in view)
                {
                    foreach (var tag in f.Tags)
                        acc.Result += tag.Key.Length + (tag.Value ?? string.Empty).Length;
                    acc.Count++;
                }
                break;
        }
    }

    // Accumulates the matched-feature count and an action-specific value; both are folded into the
    // benchmark's return value so the work is not eliminated as dead code.
    struct Accumulator
    {
        public long Count;
        public double Result;
    }

}
