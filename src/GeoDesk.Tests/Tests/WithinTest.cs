/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature.Filters;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.WithinTest</c>.</remarks>
public class WithinTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.WithinTest.testWithin()</c>.</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestWithin()
    {
        var bavaria = world
            .Select("a[boundary=administrative][admin_level=4][name:en=Bavaria]")
            .First()!.ToGeometry();
        var highways = world.Select("w[highway]");
        var slow = TestUtils.GetSet(highways.Select(new SlowWithinFilter(bavaria)));
        var fast = TestUtils.GetSet(highways.Within(bavaria));
        TestUtils.CheckNoDupes("fast", fast);
        TestUtils.CompareSets("slow", slow, "fast", fast);
    }

}
