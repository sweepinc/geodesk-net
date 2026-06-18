/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.FeatureTest</c>.</remarks>
public class FeatureTest : AbstractFeatureTest
{

    /// <summary>
    /// Coordinates returned by <c>ToXY</c> should match those of the feature's geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.FeatureTest.testCoordinates()</c>.</remarks>
    [Fact]
    public void TestCoordinates()
    {
var ways = world.Ways();
        foreach (var w in ways)
        {
            var coords = w.ToXY();
            var g = w.ToGeometry();
            Assert.True(coords.Length % 2 == 0);
            Assert.True(coords.Length >= 4);
            Assert.Equal(coords.Length / 2, g.NumPoints);
        }
    }

    /// <summary>Raise this limit as planet file grows.</summary>
    const long MaxRealisticId = 16_000_000_000L;

    /// <summary>
    /// Make sure IDs are not 0, negative, or unusually large (this limit can change!).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.FeatureTest.testIds()</c>.</remarks>
    [Fact]
    public void TestIds()
    {
foreach (var f in world)
        {
            var id = f.Id();
            Assert.True(id > 0);
            Assert.True(id <= MaxRealisticId);
        }
    }

}
