/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;

namespace GeoDesk.Benchmarks;

/// <summary>
/// The benchmark plan: the named GOQL queries, the box/circle/polygon shape specs, and the list of
/// benchmark cases — the same data the upstream suite reads from <c>new-benchmark.fab</c>, encoded here
/// as C# rather than parsed from a resource. Tuned for a Germany extract.
/// </summary>
/// <remarks>Ported from the data in <c>com.geodesk.benchmark</c>'s <c>new-benchmark.fab</c> (see <c>BenchmarkPlan</c>).</remarks>
internal static class Plan
{

    /// <summary>The distance (meters) a generated shape's center may be jittered from its seed feature.</summary>
    /// <remarks>The constant <c>500</c> the upstream <c>BenchmarkPlan</c> passes to the shape maker.</remarks>
    public const double MaxOffsetMeters = 500;

    /// <summary>Box generation parameters: the seed query, how many to make, and the min/max side length (meters).</summary>
    public sealed record BoxSpec(string SeedQuery, int Count, int MinMeters, int MaxMeters);

    /// <summary>Circle (seed-point) generation parameters: the seed query, how many, and the min/max radius (meters).</summary>
    public sealed record CircleSpec(string SeedQuery, int Count, int MinRadiusMeters, int MaxRadiusMeters);

    /// <summary>Named GOQL queries (the <c>queries</c> section of the upstream plan).</summary>
    public static readonly IReadOnlyDictionary<string, string> Queries = new Dictionary<string, string>
    {
        ["admin_areas"] = "a[boundary=administrative]",
        ["anything"] = "*",
        ["any_restaurant"] = "na[amenity=restaurant]",
        ["areas"] = "a",
        ["bakeries"] = "na[shop=bakery]",
        ["bike_routes"] = "r[type=route][route=bicycle]",
        ["buildings"] = "a[building]",
        ["canals"] = "w[waterway=canal]",
        ["castles"] = "na[historic=castle]",
        ["coastlines"] = "w[natural=coastline]",
        ["eateries"] = "na[amenity=restaurant,cafe,fast_food,biergarten,bar,pub,ice_cream]",
        ["farmyards"] = "a[landuse=farmyard]",
        ["fire_hydrants"] = "n[emergency=fire_hydrant]",
        ["highways"] = "wa[highway]",
        ["hotels"] = "na[tourism=hotel,guest_house]",
        ["libraries"] = "na[amenity=library]",
        ["lighthouses"] = "na[man_made=lighthouse]",
        ["motorways"] = "w[highway=motorway]",
        ["named_parks"] = "a[leisure=park][name]",
        ["nodes"] = "n",
        ["places"] = "na[place]",
        ["post_boxes"] = "n[amenity=post_box]",
        ["pubs"] = "na[amenity=pub]",
        ["railways"] = "w[railway]",
        ["restaurants"] = "na[amenity=restaurant][cuisine=italian][website][phone][opening_hours]",
        ["soccer_fields"] = "a[leisure=pitch][sport=soccer]",
        ["towns"] = "n[place=city],n[place=town][population>20000]",
        ["hamlets"] = "n[place=isolated_dwelling,hamlet]",
        ["woods"] = "a[natural=wood]",
    };

    /// <summary>Box shape specs (the <c>boxes</c> section): &lt;seed query, count, min meters, max meters&gt;.</summary>
    public static readonly IReadOnlyDictionary<string, BoxSpec> Boxes = new Dictionary<string, BoxSpec>
    {
        ["urban-xs"] = new("towns", 100000, 10, 100),
        ["urban-s"] = new("towns", 100000, 250, 500),
        ["urban-m"] = new("towns", 10000, 500, 2500),
        ["urban-l"] = new("towns", 10000, 2500, 10000),
        ["urban-xl"] = new("towns", 1000, 10000, 100000),
        ["rural-xs"] = new("hamlets", 100000, 10, 100),
        ["rural-s"] = new("hamlets", 100000, 250, 500),
        ["rural-m"] = new("hamlets", 10000, 500, 2500),
        ["rural-l"] = new("hamlets", 10000, 2500, 10000),
        ["rural-xl"] = new("hamlets", 1000, 10000, 100000),
    };

    /// <summary>Circle (seed-point) specs (the <c>circles</c> section): &lt;seed query, count, min radius, max radius&gt;.</summary>
    public static readonly IReadOnlyDictionary<string, CircleSpec> Circles = new Dictionary<string, CircleSpec>
    {
        ["urban"] = new("towns", 10000, 0, 0),
        ["urban-s"] = new("towns", 10000, 250, 250),
        ["urban-m"] = new("towns", 10000, 1000, 1000),
        ["urban-l"] = new("towns", 10000, 5000, 5000),
    };

    /// <summary>Polygon specs (the <c>polygons</c> section): the seed query whose areas become query polygons.</summary>
    public static readonly IReadOnlyDictionary<string, string> Polygons = new Dictionary<string, string>
    {
        ["country"] = "a[boundary=administrative][admin_level=2][name:en=Germany]",
        ["state"] = "a[boundary=administrative][admin_level=4][name]",
        ["county"] = "a[boundary=administrative][admin_level=6][name]",
        ["city"] = "a[boundary=administrative][admin_level=8][name]",
    };

    /// <summary>
    /// The benchmark cases (the <c>benchmarks</c> section). Each name decodes to query-action-spatial-shapes,
    /// where the shapes token may itself contain a dash (e.g. <c>urban-s</c>).
    /// </summary>
    static readonly string[] Names =
    [
        "areas-count-enclosing-urban",
        "buildings-count-intersects-country",
        "highways-count-intersects-country",
        "any_restaurant-count-intersects-country",
        "buildings-count-within-state",
        "libraries-count-within-city",
        "pubs-name-bbox-urban-s",
        "pubs-name-bbox-urban-m",
        "canals-count-bbox-urban-l",
        "canals-length-bbox-rural-m",
        "canals-length-bbox-rural-xl",
        "castles-name-bbox-urban-l",
        "post_boxes-count-bbox-urban-m",
        "woods-count-bbox-rural-l",
        "woods-count-bbox-rural-xl",
        "libraries-name-bbox-urban-m",
        "libraries-name-bbox-urban-l",
        "railways-length-bbox-urban-l",
        "railways-length-bbox-urban-xl",
        "anything-count-bbox-urban-xs",
        "anything-count-bbox-urban-s",
        "anything-count-bbox-urban-xl",
        "coastlines-length-bbox-urban-xl",
        "farmyards-count-bbox-rural-m",
        "farmyards-count-bbox-rural-l",
        "lighthouses-count-bbox-urban-xl",
        "lighthouses-tags-bbox-urban-xl",
        "places-count-bbox-urban-xl",
        "nodes-count-bbox-urban-xs",
        "nodes-count-bbox-urban-s",
        "nodes-count-bbox-urban-xl",
        "nodes-tags-bbox-urban-xl",
        "bakeries-name-bbox-urban-m",
        "bakeries-name-bbox-urban-l",
        "hotels-name-bbox-urban-s",
        "hotels-name-bbox-urban-m",
        "hotels-name-bbox-urban-l",
        "named_parks-name-bbox-urban-l",
        "named_parks-name-bbox-urban-xl",
        "fire_hydrants-count-bbox-urban-s",
        "fire_hydrants-count-bbox-urban-m",
        "motorways-length-bbox-urban-m",
        "motorways-length-bbox-urban-l",
        "motorways-length-bbox-urban-xl",
        "restaurants-count-bbox-urban-m",
        "restaurants-count-bbox-urban-l",
        "highways-count-bbox-urban-xs",
        "highways-count-bbox-urban-s",
        "highways-length-bbox-urban-m",
        "highways-length-bbox-urban-l",
        "soccer_fields-count-bbox-urban-l",
        "admin_areas-count-bbox-urban-l",
        "admin_areas-count-bbox-urban-xl",
        "eateries-count-bbox-urban-m",
        "eateries-count-bbox-urban-l",
        "eateries-tags-bbox-urban-m",
        "eateries-tags-bbox-urban-l",
    ];

    /// <summary>Parses every benchmark name into a <see cref="BenchmarkCase"/> (the BDN parameter set).</summary>
    public static IEnumerable<BenchmarkCase> Cases()
    {
        foreach (var name in Names)
            yield return Parse(name);
    }

    // Decodes query-action-spatial-shapes, mirroring FeatureBenchmarkRunner.createBenchmark: the shapes
    // token is parts[3] (+ "-" + parts[4] when present), so a name has exactly 4 or 5 dash-separated parts.
    static BenchmarkCase Parse(string name)
    {
        var parts = name.Split('-');
        if (parts.Length is < 4 or > 5)
            throw new FormatException($"Benchmark name '{name}' must have 4 or 5 dash-separated parts.");

        var queryKey = parts[0];
        if (!Queries.TryGetValue(queryKey, out var query))
            throw new FormatException($"Benchmark '{name}': unknown query '{queryKey}'.");

        var action = parts[1] switch
        {
            "count" => BenchAction.Count,
            "name" => BenchAction.Name,
            "length" => BenchAction.Length,
            "tags" => BenchAction.Tags,
            _ => throw new FormatException($"Benchmark '{name}': unknown action '{parts[1]}'."),
        };

        var spatial = parts[2] switch
        {
            "bbox" => Spatial.Bbox,
            "intersects" => Spatial.Intersects,
            "within" => Spatial.Within,
            "enclosing" => Spatial.Enclosing,
            _ => throw new FormatException($"Benchmark '{name}': unknown spatial relation '{parts[2]}'."),
        };

        var shapeSet = parts.Length > 4
            ? string.Create(CultureInfo.InvariantCulture, $"{parts[3]}-{parts[4]}")
            : parts[3];

        return new BenchmarkCase
        {
            Name = name,
            Query = query,
            Action = action,
            Spatial = spatial,
            ShapeSet = shapeSet,
        };
    }

}
