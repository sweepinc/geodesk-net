/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.ContainingTest</c>.</remarks>
public class ContainingTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.ContainingTest.testWithin()</c>.</remarks>
    [Fact]
    public void TestWithin()
    {
        var counties = world.Select("a[boundary=administrative][admin_level=6]");

        foreach (var county in counties)
        {
            foreach (var f in world.Select("na").Within(county))
            {
                Assert.True(world.Containing(f).Contains(county));
            }
        }
    }

}
