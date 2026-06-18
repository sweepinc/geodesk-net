/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using GeoDesk.Feature;
using GeoDesk.Util;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.MapMakerTest</c>.</remarks>
[Collection("GolFixture")]
public class MapMakerTest : IDisposable
{

    readonly IFeatures features;

    public MapMakerTest()
    {
        features = FeatureLibrary.Open(TestSettings.Resolve("w-good.gol"));
    }

    public void Dispose() => ((IDisposable)features).Dispose();

    /// <remarks>Ported from Java <c>com.geodesk.tests.MapMakerTest.testFrance()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestFrance()
    {
        var france = features.Select("a[boundary=administrative][admin_level=2][name=France]").First();
        var map = new MapMaker();

        map.Add(features
            .Select("a[boundary=administrative][admin_level=6]")
            .Within(france!));
        map.Save(Path.Combine(TestSettings.OutputPath(), "france.html"));
    }

}
