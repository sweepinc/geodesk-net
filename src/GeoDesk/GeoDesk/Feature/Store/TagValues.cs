/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Globalization;
using Clarisma.Common.Math;
using DecimalType = Clarisma.Common.Math.Decimal;

namespace GeoDesk.Feature.Store;

public static class TagValues
{
    /// <summary>
    /// The last entry in the Global String Table that can serve as a Global-Key code.
    /// </summary>
    public const int MAX_COMMON_KEY = (1 << 13) - 2;
    public const int MAX_COMMON_ROLE = (1 << 15) - 1;
    public const int EMPTY_TABLE_MARKER = 0x8001;
    public const int MIN_NUMBER = -256;
    public const int MAX_WIDE_NUMBER = (1 << 30) - 1 + MIN_NUMBER;
    public const int MAX_NARROW_NUMBER = (1 << 16) - 1 + MIN_NUMBER;

    public const int NARROW_NUMBER = 0;
    public const int GLOBAL_STRING = 1;
    public const int WIDE_NUMBER = 2;
    public const int LOCAL_STRING = 3;

    /// <summary>
    /// Checks whether the given decimal can be represented as a narrow number.
    /// </summary>
    public static bool IsNarrowNumber(long decimalValue)
    {
        if (DecimalType.Scale(decimalValue) != 0) return false;
        long mantissa = DecimalType.Mantissa(decimalValue);
        return mantissa >= MIN_NUMBER && mantissa <= MAX_NARROW_NUMBER;
    }

    public static double WideNumberToDouble(int number)
    {
        double mantissa = (int)((uint)number >> 2) + MIN_NUMBER;
        switch (number & 3)
        {
            case 1: return mantissa / 10;
            case 2: return mantissa / 100;
            case 3: return mantissa / 1000;
            default: return mantissa;
        }
    }

    public static string? WideNumberToString(int number)
    {
        int mantissa = (int)((uint)number >> 2) + MIN_NUMBER;
        switch (number & 3)
        {
            case 0: return mantissa.ToString(CultureInfo.InvariantCulture);
            case 1: return ((double)mantissa / 10).ToString("F1", CultureInfo.InvariantCulture);
            case 2: return ((double)mantissa / 100).ToString("F2", CultureInfo.InvariantCulture);
            case 3: return ((double)mantissa / 1000).ToString("F3", CultureInfo.InvariantCulture);
        }
        return null; // cannot reach this
    }

    public static int ToInt(string s)
    {
        return (int)MathUtils.DoubleFromString(s);
    }

    public static long ToLong(string s)
    {
        return (long)MathUtils.DoubleFromString(s);
    }

    public static double ToDouble(string s)
    {
        return MathUtils.DoubleFromString(s);
    }
}
