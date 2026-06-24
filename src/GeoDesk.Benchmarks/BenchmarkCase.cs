/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Benchmarks;

/// <summary>The spatial relation a benchmark applies to each shape in its batch.</summary>
/// <remarks>Mirrors the spatial query types of the upstream benchmark runner (bbox / intersects / within / enclosing).</remarks>
public enum Spatial
{
    /// <summary>Bounding-box query (<c>features.In(box)</c>) over a set of random boxes.</summary>
    Bbox,
    /// <summary>Intersection query (<c>features.Intersecting(geom)</c>) over a set of polygons.</summary>
    Intersects,
    /// <summary>Containment query (<c>features.Within(geom)</c>) over a set of polygons.</summary>
    Within,
    /// <summary>Point-enclosing query (<c>features.ContainingXY(x, y)</c>) over a set of seed points.</summary>
    Enclosing,
}

/// <summary>The work performed on each query result.</summary>
/// <remarks>Mirrors the upstream <c>QueryBenchmark.Action</c> set (count / name / length / tags).</remarks>
public enum BenchAction
{
    /// <summary>Count the matching features.</summary>
    Count,
    /// <summary>Read each feature's <c>name</c> tag (sums its length).</summary>
    Name,
    /// <summary>Measure each feature's geometry length.</summary>
    Length,
    /// <summary>Iterate each feature's full tag table.</summary>
    Tags,
}

/// <summary>
/// One parameterized benchmark: a named query restricted by a spatial relation over a named shape set,
/// with an action applied to each result. Decomposed from a benchmark name such as
/// <c>pubs-name-bbox-urban-s</c> (query <c>pubs</c>, action <c>name</c>, spatial <c>bbox</c>, shapes
/// <c>urban-s</c>), exactly as the upstream <c>FeatureBenchmarkRunner.createBenchmark</c> splits it.
/// </summary>
/// <remarks>Used as a BenchmarkDotNet parameter; <see cref="ToString"/> is the row label (the original name).</remarks>
public sealed class BenchmarkCase
{
    /// <summary>The original benchmark name (e.g. <c>pubs-name-bbox-urban-s</c>); also the BDN row label.</summary>
    public required string Name { get; init; }

    /// <summary>The GOQL query string the case selects (resolved from the plan's named queries).</summary>
    public required string Query { get; init; }

    /// <summary>The action applied to each query result.</summary>
    public required BenchAction Action { get; init; }

    /// <summary>The spatial relation applied to each shape.</summary>
    public required Spatial Spatial { get; init; }

    /// <summary>The name of the shape set (boxes, circles, or polygons) this case queries against.</summary>
    public required string ShapeSet { get; init; }

    public override string ToString() => Name;
}
