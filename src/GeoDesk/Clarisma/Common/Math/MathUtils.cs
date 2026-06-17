/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Math;

// TODO: these are really parsing methods rather than "math"

/// <remarks>Ported from Java <c>com.clarisma.common.math.MathUtils</c>.</remarks>
public static class MathUtils
{
    private static readonly double[] Pow10Table =
    {
        1, 10, 100, 1000, 10_000, 100_000, 1_000_000, 10_000_000, 100_000_000, 1_000_000_000
    };

    public static double Pow10(int exp)
    {
        return exp >= 0 && exp < Pow10Table.Length ? Pow10Table[exp] : System.Math.Pow(10, exp);
    }

    public static double DoubleFromString(string s)
    {
        int len = s.Length;
        int i = 0;
        for (; i < len; i++) if (s[i] > 32) break;
        if (i >= len) return double.NaN;
        bool negative = false;
        bool seenDigit = false;
        int decimalPos = -1;
        double value = 0;
        if (s[i] == '-')
        {
            negative = true;
            i++;
        }
        for (; i < len; i++)
        {
            char ch = s[i];
            if (ch >= '0' && ch <= '9')
            {
                value = value * 10 + (ch - '0');
                seenDigit = true;
                continue;
            }
            if (ch == '.')
            {
                if (decimalPos >= 0) break;
                decimalPos = i;
                continue;
            }
            break;
        }
        if (!seenDigit) return double.NaN;
        if (negative) value = -value;
        return decimalPos < 0 ? value : (value / Pow10(i - decimalPos - 1));
    }

    /// <summary>
    /// Counts the number of characters in the given string that
    /// represent a valid number.
    ///
    /// Valid characters are:
    /// - leading whitespace
    /// - a leading minus sign
    /// - digits and a single decimal point
    /// </summary>
    /// <param name="s">the string to examine</param>
    /// <returns>the first position that contains a character that is not part of a number</returns>
    public static int CountNumberChars(string s)
    {
        int n = 0;
        int len = s.Length;
        for (; n < len; n++) if (s[n] > 32) break;
        if (n >= len) return 0;
        if (s[n] == '-') n++;
        bool seenDecimalPoint = false;
        for (; n < len; n++)
        {
            char ch = s[n];
            if (ch >= '0' && ch <= '9') continue;
            if (ch == '.')
            {
                if (seenDecimalPoint) break;
                seenDecimalPoint = true;
                continue;
            }
            break;
        }
        return n;
    }
}
