/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using GeoDesk.Feature;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace GeoDesk.Benchmarks;

/// <summary>
/// Generates (and caches) the random shape sets the spatial benchmarks query against: boxes and seed
/// points placed near features matching a seed query, and polygons taken from area features. Mirrors the
/// upstream <c>ShapeMaker</c>.
/// </summary>
/// <remarks>
/// <para>
/// Caching is the key to comparability: a shape set is generated once and written to disk, then reused on
/// every later run, so "dataset today vs dataset today" compares identical inputs. The cache is keyed by the
/// dataset (the GOL file name + size), so switching GOLs regenerates. Generation is seeded deterministically
/// (per dataset + shape set) so even a first run before the cache exists is reproducible.
/// </para>
/// <para>Ported from Java <c>com.geodesk.benchmark.ShapeMaker</c>.</para>
/// </remarks>
internal sealed class ShapeMaker
{

    readonly IFeatureQuery _world;
    readonly string _cacheDir;
    readonly string _datasetKey;

    /// <summary>Creates a shape maker over the given library, caching shapes under the executing assembly's output.</summary>
    public ShapeMaker(IFeatureQuery world, string golPath)
    {
        _world = world;
        _cacheDir = Path.Combine(AppContext.BaseDirectory, "shapes");
        Directory.CreateDirectory(_cacheDir);
        var info = new FileInfo(golPath);
        _datasetKey = string.Create(CultureInfo.InvariantCulture, $"{Path.GetFileNameWithoutExtension(golPath)}-{info.Length}");
    }

    /// <summary>Loads or generates the named box set (from <see cref="Plan.Boxes"/>).</summary>
    public List<Box> Boxes(string name)
    {
        var spec = Plan.Boxes[name];
        var path = CachePath("boxes", name, "bin");
        if (File.Exists(path))
            return ReadBoxes(path);

        var boxes = CreateBoxes(name, spec);
        WriteBoxes(path, boxes);
        return boxes;
    }

    /// <summary>Loads or generates the named circle (seed-point) set (from <see cref="Plan.Circles"/>).</summary>
    public List<Circle> Circles(string name)
    {
        var spec = Plan.Circles[name];
        var path = CachePath("circles", name, "bin");
        if (File.Exists(path))
            return ReadCircles(path);

        var circles = CreateCircles(name, spec);
        WriteCircles(path, circles);
        return circles;
    }

    /// <summary>Loads or generates the named polygon set (the areas matching <see cref="Plan.Polygons"/>'s seed query).</summary>
    public List<Geometry> Polygons(string name)
    {
        var seedQuery = Plan.Polygons[name];
        var path = CachePath("polygons", name, "wkt");
        if (File.Exists(path))
            return ReadPolygons(path);

        var polygons = CreatePolygons(name, seedQuery);
        WritePolygons(path, polygons);
        return polygons;
    }

    // === generation (mirrors ShapeMaker.randomBoxes / randomCircles / randomPolygons) ===

    List<Box> CreateBoxes(string name, Plan.BoxSpec spec)
    {
        var rng = SeededRandom("boxes", name);
        var seeds = LoadSeeds(Plan.Queries[spec.SeedQuery], rng);
        var boxes = new List<Box>(spec.Count);
        if (seeds.Count == 0)
            return boxes;

        Console.WriteLine($"  Creating {spec.Count:N0} random boxes ({spec.MinMeters}m - {spec.MaxMeters}m) for '{name}'");
        var i = 0;
        while (boxes.Count < spec.Count)
        {
            var seed = seeds[i];
            int y = seed.Y;
            var offset = Mercator.DeltaFromMeters(Plan.MaxOffsetMeters, y);
            int x = seed.X + (int)Jitter(rng, offset);
            y += (int)Jitter(rng, offset);
            var minExtent = (int)Mercator.DeltaFromMeters(spec.MinMeters, y);
            var maxExtent = (int)Mercator.DeltaFromMeters(spec.MaxMeters, y);
            var w = RandInt(rng, minExtent, maxExtent);
            var h = RandInt(rng, minExtent, maxExtent);
            boxes.Add(Box.OfXYWidthHeight(x - w / 2, y - h / 2, w, h));
            if (++i >= seeds.Count) i = 0;
        }
        Shuffle(boxes, rng);
        return boxes;
    }

    List<Circle> CreateCircles(string name, Plan.CircleSpec spec)
    {
        var rng = SeededRandom("circles", name);
        var seeds = LoadSeeds(Plan.Queries[spec.SeedQuery], rng);
        var circles = new List<Circle>(spec.Count);
        if (seeds.Count == 0)
            return circles;

        Console.WriteLine($"  Creating {spec.Count:N0} random circles ({spec.MinRadiusMeters}m - {spec.MaxRadiusMeters}m radius) for '{name}'");
        var i = 0;
        while (circles.Count < spec.Count)
        {
            var seed = seeds[i];
            int y = seed.Y;
            var offset = Mercator.DeltaFromMeters(Plan.MaxOffsetMeters, y);
            int x = seed.X + (int)Jitter(rng, offset);
            y += (int)Jitter(rng, offset);
            var radius = spec.MinRadiusMeters == spec.MaxRadiusMeters
                ? spec.MinRadiusMeters
                : RandInt(rng, spec.MinRadiusMeters, spec.MaxRadiusMeters);
            circles.Add(new Circle(x, y, radius));
            if (++i >= seeds.Count) i = 0;
        }
        Shuffle(circles, rng);
        return circles;
    }

    List<Geometry> CreatePolygons(string name, string seedQuery)
    {
        var rng = SeededRandom("polygons", name);
        var seeds = LoadSeeds(seedQuery, rng);
        Console.WriteLine($"  Creating polygons from {seeds.Count:N0} seed areas for '{name}'");
        var geoms = new List<Geometry>(seeds.Count);
        foreach (var f in seeds)
        {
            if (!f.IsArea) continue;
            var g = f.ToGeometry();
            if (g.IsValid) geoms.Add(g);
        }
        return geoms;
    }

    List<IFeature> LoadSeeds(string seedQuery, Random rng)
    {
        var seeds = _world.Select(seedQuery).ToList();
        Shuffle(seeds, rng);
        return seeds;
    }

    // === helpers ===

    // Uniform value in [-delta, delta), matching Java's random.nextDouble(-delta, delta).
    static double Jitter(Random rng, double delta) => (rng.NextDouble() * 2.0 - 1.0) * delta;

    // Uniform int in [min, max) (max exclusive, as Java's nextInt(min, max)); collapses to min when min >= max.
    static int RandInt(Random rng, int min, int max) => min >= max ? min : rng.Next(min, max);

    static void Shuffle<T>(IList<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // A process-stable seed (string.GetHashCode is randomized per run, so use FNV-1a over the dataset + set).
    Random SeededRandom(string kind, string name)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var c in $"{_datasetKey}:{kind}:{name}")
            hash = (hash ^ c) * prime;
        return new Random(unchecked((int)hash));
    }

    string CachePath(string kind, string name, string ext) =>
        Path.Combine(_cacheDir, string.Create(CultureInfo.InvariantCulture, $"{_datasetKey}.{kind}-{name}.{ext}"));

    // === cache I/O ===

    static List<Box> ReadBoxes(string path)
    {
        using var r = new BinaryReader(File.OpenRead(path));
        var count = r.ReadInt32();
        var boxes = new List<Box>(count);
        for (var i = 0; i < count; i++)
            boxes.Add(new Box(r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32()));
        return boxes;
    }

    static void WriteBoxes(string path, List<Box> boxes)
    {
        using var w = new BinaryWriter(File.Create(path));
        w.Write(boxes.Count);
        foreach (var b in boxes)
        {
            w.Write(b.MinX);
            w.Write(b.MinY);
            w.Write(b.MaxX);
            w.Write(b.MaxY);
        }
    }

    static List<Circle> ReadCircles(string path)
    {
        using var r = new BinaryReader(File.OpenRead(path));
        var count = r.ReadInt32();
        var circles = new List<Circle>(count);
        for (var i = 0; i < count; i++)
            circles.Add(new Circle(r.ReadInt32(), r.ReadInt32(), r.ReadInt32()));
        return circles;
    }

    static void WriteCircles(string path, List<Circle> circles)
    {
        using var w = new BinaryWriter(File.Create(path));
        w.Write(circles.Count);
        foreach (var c in circles)
        {
            w.Write(c.X);
            w.Write(c.Y);
            w.Write(c.Radius);
        }
    }

    static List<Geometry> ReadPolygons(string path)
    {
        var reader = new WKTReader();
        var geoms = new List<Geometry>();
        foreach (var line in File.ReadLines(path))
        {
            if (line.Length != 0)
                geoms.Add(reader.Read(line));
        }
        return geoms;
    }

    static void WritePolygons(string path, List<Geometry> geoms)
    {
        var writer = new WKTWriter();
        using var w = new StreamWriter(File.Create(path));
        foreach (var g in geoms)
            w.WriteLine(writer.Write(g));
    }

}
