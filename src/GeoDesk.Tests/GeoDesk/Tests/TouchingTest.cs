/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.TouchingTest</c>.</remarks>
[Collection("GolFixture")]
public class TouchingTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.TouchingTest.testTouching()</c>.</remarks>
    [Fact]
    public void TestTouching()
    {
var adminAreas = world.Select("a[boundary=administrative]");
        var counties = world.Select("a[boundary=administrative][admin_level=6]");

        foreach (var county in counties)
        {
            Console.Write("Admin areas that touch {0} ({1}):\n", county.StringValue("name"), county);
            foreach (var neighbor in adminAreas.Touching(county))
            {
                Console.Write("- {0}: {1}\n", neighbor, neighbor.StringValue("name"));
            }
        }
    }

}
