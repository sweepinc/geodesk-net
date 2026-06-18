/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Xunit;

using DecimalType = GeoDesk.Common.Math.Decimal;

namespace Clarisma.Common.Math;

public class DecimalTest
{
    private static void Test(string original, bool strict, double expected, string expectedStr)
    {
        long d = DecimalType.Parse(original, strict);
        double actual = DecimalType.ToDouble(d);
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"expected NaN for \"{original}\" (strict={strict}), got {actual}");
        }
        else
        {
            Assert.True(System.Math.Abs(expected - actual) <= 0.0000001,
                $"expected {expected} for \"{original}\" (strict={strict}), got {actual}");
        }
        Assert.Equal(expectedStr, DecimalType.ToString(d));
    }

    [Fact]
    public void TestDecimal()
    {
        Test(".5", false, 0.5, "0.5");
        Test(".5", true, double.NaN, "invalid");

        Test("", false, double.NaN, "invalid");
        Test("", true, double.NaN, "invalid");

        Test("0", false, 0.0, "0");
        Test("0", true, 0.0, "0");

        Test("007", false, 7, "7");
        Test("007", true, double.NaN, "invalid");

        Test("08135", false, 8135, "8135");
        Test("08135", true, double.NaN, "invalid");

        Test("3.5 t", false, 3.5, "3.5");
        Test("3.5 t", true, double.NaN, "invalid");

        Test("50", false, 50.0, "50");
        Test("50", true, 50.0, "50");

        Test("01", false, 1.0, "1");
        Test("01", true, double.NaN, "invalid");

        Test("0.0", false, 0, "0.0");
        Test("0.0", true, 0, "0.0");
        Test("0.00", false, 0, "0.00");
        Test("0.00", true, 0, "0.00");

        Test("0.500", false, 0.5, "0.500");
        Test("0.500", true, 0.5, "0.500");

        Test("00.500", false, 0.5, "0.500");
        Test("00.500", true, double.NaN, "invalid");

        Test("0.", false, 0.0, "0");
        Test("0.", true, double.NaN, "invalid");

        Test(".25", false, 0.25, "0.25");
        Test(".25", true, double.NaN, "invalid");

        Test("-0.0000", false, 0.0, "0.0000");
        Test("-0.0000", true, double.NaN, "invalid");

        Test("4.25.", false, 4.25, "4.25");
        Test("4.25.", true, double.NaN, "invalid");

        Test("1000000000000000000000000000", false, double.NaN, "invalid");
        Test("1000000000000000000000000000", true, double.NaN, "invalid");
    }

    [Fact]
    public void TestToString()
    {
        Assert.Equal("0.01", DecimalType.ToString(DecimalType.Of(1, 2)));
        Assert.Equal("-0.003", DecimalType.ToString(DecimalType.Of(-3, 3)));
        Assert.Equal("0.0000", DecimalType.ToString(DecimalType.Of(0, 4)));
        Assert.Equal("33.000", DecimalType.ToString(DecimalType.Of(33000, 3)));
        Assert.Equal("2.1", DecimalType.ToString(DecimalType.Of(21, 1)));
        Assert.Equal("-55.22", DecimalType.ToString(DecimalType.Of(-5522, 2)));
        Assert.Equal("-1042.5799000", DecimalType.ToString(DecimalType.Of(-10425799000L, 7)));
        Assert.Equal("107", DecimalType.ToString(DecimalType.Of(107, 0)));
        Assert.Equal("-4455", DecimalType.ToString(DecimalType.Of(-4455, 0)));
        Assert.Equal("0", DecimalType.ToString(DecimalType.Of(0, 0)));
        Assert.Equal("345678901234567890", DecimalType.ToString(DecimalType.Of(345678901234567890L, 0)));
        Assert.Equal("-345678901234567890", DecimalType.ToString(DecimalType.Of(-345678901234567890L, 0)));
    }
}
