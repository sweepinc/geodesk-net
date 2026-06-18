/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;

using GeoDesk.Feature;

using NetTopologySuite.Geometries;

using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Tests.Feature.Polygon;

/// <summary>
/// Builds geometries for the area relations in the monaco.gol fixture, exercising
/// PolygonBuilder / Ring assembly and the non-area GeometryCollection path. Soft-skips if no
/// fixture is available.
/// </summary>
public class PolygonBuilderTest
{

    readonly ITestOutputHelper _output;

    public PolygonBuilderTest(ITestOutputHelper output)
    {
        _output = output;
    }

    static string GolFile() => Path.Combine(AppContext.BaseDirectory, "Fixtures", "monaco.gol");

    [Fact]
    public void BuildsRelationGeometries()
    {
        var gol = GolFile();
        if (!File.Exists(gol))
        {
            _output.WriteLine($"No GOL fixture at {gol} - skipping.");
            return;
        }

        var world = FeatureLibrary.Open(gol);
        try
        {
            long areaRelations = 0;
            long validPolygons = 0;
            long collections = 0;
            foreach (var r in world.Relations())
            {
                var g = r.ToGeometry();
                Assert.NotNull(g);
                if (r.IsArea())
                {
                    areaRelations++;
                    // Area relations must build to a (Multi)Polygon.
                    Assert.True(g is NetTopologySuite.Geometries.Polygon || g is MultiPolygon, $"relation/{r.Id()} expected polygonal, got {g.GeometryType}");
                    if (!g.IsEmpty && g.IsValid)
                        validPolygons++;
                }
                else
                {
                    collections++;
                }
            }
            _output.WriteLine($"area relations={areaRelations} (valid={validPolygons}), non-area={collections}");
            Assert.True(areaRelations > 0, "expected area relations in Monaco");
            Assert.True(validPolygons > 0, "expected at least one valid polygon");
        }
        finally
        {
            ((IDisposable)world).Dispose();
        }
    }

}
