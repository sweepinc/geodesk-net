/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Xunit;

using static GeoDesk.Common.Math.MathUtils;

namespace GeoDesk.Tests.Common.Math;

public class MathUtilsTest
{
    [Fact]
    public void TestDoubleFromString()
    {
        Assert.True(double.IsNaN(DoubleFromString("Test")), "Must be NaN");
        Assert.True(double.IsNaN(DoubleFromString("--2")), "Must be NaN");
        Assert.True(double.IsNaN(DoubleFromString("..5")), "Must be NaN");
        Assert.True(double.IsNaN(DoubleFromString("-..5")), "Must be NaN");
        Assert.Equal(457d, DoubleFromString("457"));
        Assert.Equal(457d, DoubleFromString("457.0"));
        Assert.Equal(457d, DoubleFromString("457.000000000000000"));
        Assert.Equal(0d, DoubleFromString("-00000.000000000000000"));
        Assert.Equal(-13100d, DoubleFromString("-0013100.0000000000000000"));
        Assert.Equal(-13100.999, DoubleFromString("-0013100.999000000000000000"));
        Assert.Equal(-1413100.99, DoubleFromString("   -001413100.99abc9000000000000000"));
    }
}
