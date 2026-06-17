/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using GeoDesk.Geom;
using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Feature.Query;

/// <summary>
/// End-to-end query of the generated monaco.gol fixture through the full
/// FeatureLibrary -> WorldView -> Query -> TileQueryTask/RTreeQueryTask path
/// (faithful ForkJoinPool execution). Soft-skips if no GOL fixture is available.
/// </summary>
[Collection("GolFixture")]
public class WorldQueryTest
{
    private readonly ITestOutputHelper output;

    public WorldQueryTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static string GolFile() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "monaco.gol");

    [Fact]
    public void QueriesHighwaysInMonaco()
    {
        string gol = GolFile();
        if (!File.Exists(gol))
        {
            output.WriteLine($"No GOL fixture at {gol} - skipping.");
            return;
        }

        Features world = Features.Open(gol);
        try
        {
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
            Feature? first = world.Ways("w[highway]").First();
            Assert.NotNull(first);
            Assert.True(first!.HasTag("highway"));
        }
        finally
        {
            ((IDisposable)world).Dispose();
        }
    }
}
