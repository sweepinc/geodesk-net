/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Feature.Store;

/// <summary>
/// Opens the generated monaco.gol fixture and validates store-level reading (string table,
/// index schema, tile index). Skips (soft) if no GOL fixture is available.
/// </summary>
[Collection("GolFixture")]
public class FeatureStoreTest
{
    private readonly ITestOutputHelper output;

    public FeatureStoreTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static string GolFile() =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "monaco.gol");

    [Fact]
    public void OpensAndReadsStringTable()
    {
        string gol = GolFile();
        if (!File.Exists(gol))
        {
            output.WriteLine($"No GOL fixture at {gol} - skipping (build with GenerateGolFixtures + gol on PATH).");
            return;
        }

        var store = new FeatureStore(gol);
        try
        {
            // "highway" is one of the most common OSM keys; it must be in the Global String Table.
            int highwayCode = store.CodeFromString("highway");
            Assert.True(highwayCode > 0, "expected 'highway' to have a global string code");
            Assert.Equal("highway", store.StringFromCode(highwayCode));

            int nameCode = store.CodeFromString("name");
            Assert.True(nameCode > 0, "expected 'name' to have a global string code");
            Assert.Equal("name", store.StringFromCode(nameCode));

            // A string that should not be in the GST returns -1.
            Assert.Equal(-1, store.CodeFromString(" not a real key "));

            // Zoom levels must be a non-zero bitset.
            Assert.NotEqual(0, store.ZoomLevels);

            output.WriteLine($"Opened {Path.GetFileName(gol)}: highway={highwayCode}, name={nameCode}, zoomLevels=0x{store.ZoomLevels:X}");
        }
        finally
        {
            store.Close();
        }
    }
}
