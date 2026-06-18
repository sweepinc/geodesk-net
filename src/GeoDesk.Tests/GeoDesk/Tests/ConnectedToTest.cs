/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.ConnectedToTest</c>.</remarks>
[Collection("GolFixture")]
public class ConnectedToTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.ConnectedToTest.testConnectedTo()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestConnectedTo()
    {
        var route = world
            .Select("r[type=route_master][route_master=bicycle][ref=D10]")
            .First();

        foreach (var f in world.Select("r[route=bicycle]").ConnectedTo(route!))
        {
            Log.Debug("- %s %s", f, f.StringValue("name"));
        }
    }

}
