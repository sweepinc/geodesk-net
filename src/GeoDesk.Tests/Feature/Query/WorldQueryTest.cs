/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;

using GeoDesk.Feature;
using GeoDesk.Geom;

using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Tests.Feature.Query;

/// <summary>
/// End-to-end query of the generated monaco.gol fixture through the full
/// FeatureLibrary -> WorldView -> Query -> TileScanner path
/// (Parallel.ForEachAsync / Channel execution). Soft-skips if no GOL fixture is available.
/// </summary>
public class WorldQueryTest
{

    readonly ITestOutputHelper output;

    public WorldQueryTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    static string GolFile() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "monaco.gol");

    [Fact]
    public void QueriesHighwaysInMonaco()
    {
        string gol = GolFile();
        if (!File.Exists(gol))
        {
            output.WriteLine($"No GOL fixture at {gol} - skipping.");
            return;
        }

        using var world = FeatureLibrary.Open(gol);

        // Count all highways (ways tagged highway) across the whole library.
        long highways = world.Ways("w[highway]").Count();
        output.WriteLine($"monaco highways (ways): {highways}");
        Assert.True(highways > 0, "expected at least one highway in Monaco");

        // Restrict to a bbox covering all of Monaco; result must not exceed the global count.
        Box monaco = Box.OfWSEN(7.40, 43.71, 7.45, 43.76);
        long highwaysInBox = world.Ways("w[highway]").In(monaco).Count();
        output.WriteLine($"monaco highways in bbox: {highwaysInBox}");
        Assert.True(highwaysInBox > 0, "expected highways within the Monaco bbox");
        Assert.True(highwaysInBox <= highways);

        // Spot-check that we can read tags off a returned feature.
        IFeature? first = world.Ways("w[highway]").First();
        Assert.NotNull(first);
        Assert.True(first!.HasTag("highway"));
    }

    [Fact]
    public void AllFeaturesAreWithinMonacoBounds()
    {
        string gol = GolFile();
        if (!File.Exists(gol))
        {
            output.WriteLine($"No GOL fixture at {gol} - skipping.");
            return;
        }

        var world = FeatureLibrary.Open(gol);

        // First, discover the actual bounds of the data
        double actualMinLon = double.MaxValue;
        double actualMaxLon = double.MinValue;
        double actualMinLat = double.MaxValue;
        double actualMaxLat = double.MinValue;

        foreach (var node in world.Nodes())
        {
            double lon = node.Lon;
            double lat = node.Lat;

            if (lon < actualMinLon)
                actualMinLon = lon;
            if (lon > actualMaxLon)
                actualMaxLon = lon;
            if (lat < actualMinLat)
                actualMinLat = lat;
            if (lat > actualMaxLat)
                actualMaxLat = lat;
        }

        output.WriteLine($"Actual data bounds:");
        output.WriteLine($"  Longitude: {actualMinLon:F6} to {actualMaxLon:F6}");
        output.WriteLine($"  Latitude: {actualMinLat:F6} to {actualMaxLat:F6}");

        // Monaco's official bounding box according to Geofabrik extract:
        // Approximately 7.4091 to 7.4496 longitude, 43.7247 to 43.7519 latitude
        // However, the PBF extract includes a buffer zone for complete ways/relations
        // that cross the boundary. We'll verify the data is reasonable for Monaco region.

        // Monaco proper is around: 7.408-7.448E, 43.724-43.752N
        // But Geofabrik extracts include buffer zones, so we expect slightly wider bounds
        // Reasonable bounds for Monaco extract (including immediate surroundings):
        const double expectedMinLon = 7.35;  // West of Monaco
        const double expectedMaxLon = 7.60;  // East of Monaco (includes buffer)
        const double expectedMinLat = 43.50; // South of Monaco
        const double expectedMaxLat = 43.80; // North of Monaco

        output.WriteLine($"Expected bounds (with Geofabrik buffer):");
        output.WriteLine($"  Longitude: {expectedMinLon:F6} to {expectedMaxLon:F6}");
        output.WriteLine($"  Latitude: {expectedMinLat:F6} to {expectedMaxLat:F6}");

        // Validate that the data is centered around Monaco
        // The center should be close to Monaco's center: ~7.426E, ~43.738N
        double centerLon = (actualMinLon + actualMaxLon) / 2.0;
        double centerLat = (actualMinLat + actualMaxLat) / 2.0;

        output.WriteLine($"Data center: lon={centerLon:F6}, lat={centerLat:F6}");
        output.WriteLine($"Monaco center: lon=7.426000, lat=43.738000");

        const double monacoExpectedCenterLon = 7.426;
        const double monacoExpectedCenterLat = 43.738;
        const double centerTolerance = 0.15; // 0.15 degrees (~16.5km at this latitude) - allows for Geofabrik buffer zone

        // Assert the bounds are reasonable for a Monaco extract
        Assert.True(actualMinLon >= expectedMinLon && actualMinLon <= monacoExpectedCenterLon, $"Minimum longitude {actualMinLon:F6} is outside expected range for Monaco region");
        Assert.True(actualMaxLon <= expectedMaxLon && actualMaxLon >= monacoExpectedCenterLon, $"Maximum longitude {actualMaxLon:F6} is outside expected range for Monaco region");
        Assert.True(actualMinLat >= expectedMinLat && actualMinLat <= monacoExpectedCenterLat, $"Minimum latitude {actualMinLat:F6} is outside expected range for Monaco region");
        Assert.True(actualMaxLat <= expectedMaxLat && actualMaxLat >= monacoExpectedCenterLat, $"Maximum latitude {actualMaxLat:F6} is outside expected range for Monaco region");

        // Verify the center is close to Monaco's center
        double centerDistanceLon = Math.Abs(centerLon - monacoExpectedCenterLon);
        double centerDistanceLat = Math.Abs(centerLat - monacoExpectedCenterLat);

        Assert.True(centerDistanceLon < centerTolerance,
            $"Data center longitude {centerLon:F6} is too far from Monaco center {monacoExpectedCenterLon:F6} " +
            $"(distance: {centerDistanceLon:F6} degrees, max: {centerTolerance:F6})");
        Assert.True(centerDistanceLat < centerTolerance,
            $"Data center latitude {centerLat:F6} is too far from Monaco center {monacoExpectedCenterLat:F6} " +
            $"(distance: {centerDistanceLat:F6} degrees, max: {centerTolerance:F6})");

        // Verify we have a reasonable number of features
        long totalNodes = world.Nodes().Count();
        output.WriteLine($"Total nodes in dataset: {totalNodes}");
        Assert.True(totalNodes > 0, "Expected to find nodes in Monaco dataset");

        // Monaco is small (~2 km²) but Geofabrik extracts include surroundings
        // Reasonable range: 1,000 to 100,000 nodes
        Assert.True(totalNodes >= 1000 && totalNodes <= 100000,
            $"Node count {totalNodes} seems unreasonable for Monaco extract (expected 1,000-100,000)");

        output.WriteLine("✓ All bounds checks passed - data is centered on Monaco");
    }
}
