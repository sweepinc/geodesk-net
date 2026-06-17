/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Math;

/// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal</c>.</remarks>
public class Decimal
{
    public const long Invalid = long.MinValue;

    private const long Overflow = unchecked((long)0xf800_0000_0000_0000L);

    private readonly long value;

    private Decimal(long value)
    {
        this.value = value;
    }

    public static Decimal FromString(string s)
    {
        return new Decimal(Parse(s, false));
    }

    public static long Parse(string s, bool strict)
    {
        long value = 0;
        int scale = 0;
        bool seenZero = false;
        bool seenNonZero = false;
        bool leadingZeroes = false;
        bool trailingNonNumeric = false;
        bool seenDot = false;
        bool negative = false;

        int len = s.Length;
        if (len == 0) return Invalid;

        int i = 0;
        char first = s[i];
        if (first == '-')
        {
            negative = true;
            i++;
            if (i == len) return Invalid;
        }
        else if (first == '+')
        {
            if (strict) return Invalid;
            i++;
            if (i == len) return Invalid;
        }

        while (i < len)
        {
            char ch = s[i++];
            if (ch == '0')
            {
                leadingZeroes |= seenZero && !seenNonZero;
                seenZero = true;
                value *= 10;
                if ((value & Overflow) != 0) return Invalid;
                continue;
            }
            if (ch == '.')
            {
                seenDot = true;
                while (i < len)
                {
                    ch = s[i++];
                    if (ch < '0' || ch > '9')
                    {
                        trailingNonNumeric = true;
                        break;
                    }
                    value = value * 10 + (ch - '0');
                    if ((value & Overflow) != 0) return Invalid;
                    scale++;
                }
                break;
            }
            if (ch < '0' || ch > '9')
            {
                trailingNonNumeric = true;
                break;
            }
            leadingZeroes |= seenZero && !seenNonZero;
            seenNonZero = true;
            value = value * 10 + (ch - '0');
            if ((value & Overflow) != 0) return Invalid;
        }

        if (strict)
        {
            if (trailingNonNumeric) return Invalid;
            if (seenDot && (scale == 0 || (!seenZero && !seenNonZero)))
            {
                return Invalid;
            }
            if (leadingZeroes) return Invalid;
            if (value == 0 && negative) return Invalid;
        }

        if (scale > 15) return Invalid;

        long result = (negative ? -value : value) << 4;
        return result | scale;
    }

    public static int Scale(long d)
    {
        return (int)d & 15;
    }

    public static long Mantissa(long d)
    {
        return d >> 4;
    }

    // TODO: use LUT instead of loop
    public static long ToLong(long d)
    {
        if (d == Invalid) return d;
        int scale = (int)d & 15;
        if (scale == 0) return d >> 4;
        long div = 10;
        for (;;)
        {
            scale--;
            if (scale == 0) break;
            div *= 10;
        }
        return (d >> 4) / div;
    }

    // TODO: use LUT instead of loop
    public static double ToDouble(long d)
    {
        if (d == Invalid) return double.NaN;
        int scale = (int)d & 15;
        long mantissa = d >> 4;
        if (scale == 0) return mantissa;
        long div = 10;
        for (;;)
        {
            scale--;
            if (scale == 0) break;
            div *= 10;
        }
        return (double)mantissa / div;
    }

    public static float ToFloat(long d)
    {
        return (float)ToDouble(d);
    }

    public static int ToInt(long d)
    {
        return (int)ToLong(d);
    }

    public static long Of(long mantissa, int scale)
    {
        System.Diagnostics.Debug.Assert(scale >= 0 && scale <= 15);
        return (mantissa << 4) | scale;
    }

    public static string ToString(long d)
    {
        if (d == Invalid) return "invalid";
        int scale = (int)d & 15;
        string s = (d >> 4).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (scale == 0) return s;
        int len = s.Length;
        char[] chars;
        if (d < 0)
        {
            if (len <= scale + 1)
            {
                int n = scale - len + 4;
                chars = new char[scale + 3];
                s.CopyTo(1, chars, n, len - 1);
                for (int i = 1; i < n; i++) chars[i] = '0';
            }
            else
            {
                chars = new char[len + 1];
                s.CopyTo(1, chars, 1, (len - scale) - 1);
                s.CopyTo(len - scale, chars, chars.Length - scale, len - (len - scale));
            }
            chars[0] = '-';
        }
        else
        {
            if (len <= scale)
            {
                int n = scale - len + 2;
                chars = new char[scale + 2];
                s.CopyTo(0, chars, n, len);
                for (int i = 0; i < n; i++) chars[i] = '0';
            }
            else
            {
                chars = new char[len + 1];
                s.CopyTo(0, chars, 0, len - scale);
                s.CopyTo(len - scale, chars, chars.Length - scale, len - (len - scale));
            }
        }
        chars[chars.Length - scale - 1] = '.';
        return new string(chars);
    }

    public static long Normalized(long d)
    {
        if (d == Invalid) return Invalid;
        int scale = (int)d & 15;
        long v = d >> 4;
        while (scale > 0)
        {
            long x = v / 10;
            if (x * 10 != v) break;
            scale--;
            v = x;
        }
        return (v << 4) | scale;
    }

    public int IntValue => ToInt(value);

    public long LongValue => ToLong(value);

    public float FloatValue => ToFloat(value);

    public double DoubleValue => ToDouble(value);

    public override string ToString()
    {
        return ToString(value);
    }
}
