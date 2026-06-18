/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Math;

/// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal</c>.</remarks>
internal class Decimal
{

    public const long Invalid = long.MinValue;

    const long Overflow = unchecked((long)0xf800_0000_0000_0000L);

    readonly long _value;

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal(long)</c>.</remarks>
    Decimal(long value)
    {
        _value = value;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.fromString(String)</c>.</remarks>
    public static Decimal FromString(string s)
    {
        return new Decimal(Parse(s, false));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.parse(String, boolean)</c>.</remarks>
    public static long Parse(string s, bool strict)
    {
        long value = 0;
        var scale = 0;
        var seenZero = false;
        var seenNonZero = false;
        var leadingZeroes = false;
        var trailingNonNumeric = false;
        var seenDot = false;
        var negative = false;

        var len = s.Length;
        if (len == 0) return Invalid;

        var i = 0;
        var first = s[i];
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
            var ch = s[i++];
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

        var result = (negative ? -value : value) << 4;
        return result | scale;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.scale(long)</c>.</remarks>
    public static int Scale(long d)
    {
        return (int)d & 15;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.mantissa(long)</c>.</remarks>
    public static long Mantissa(long d)
    {
        return d >> 4;
    }

    // TODO: use LUT instead of loop
    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.toLong(long)</c>.</remarks>
    public static long ToLong(long d)
    {
        if (d == Invalid) return d;
        var scale = (int)d & 15;
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
    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.toDouble(long)</c>.</remarks>
    public static double ToDouble(long d)
    {
        if (d == Invalid) return double.NaN;
        var scale = (int)d & 15;
        var mantissa = d >> 4;
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

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.toFloat(long)</c>.</remarks>
    public static float ToFloat(long d)
    {
        return (float)ToDouble(d);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.toInt(long)</c>.</remarks>
    public static int ToInt(long d)
    {
        return (int)ToLong(d);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.of(long, int)</c>.</remarks>
    public static long Of(long mantissa, int scale)
    {
        System.Diagnostics.Debug.Assert(scale >= 0 && scale <= 15);
        return (mantissa << 4) | scale;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.toString(long)</c>.</remarks>
    public static string ToString(long d)
    {
        if (d == Invalid) return "invalid";
        var scale = (int)d & 15;
        var s = (d >> 4).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (scale == 0) return s;
        var len = s.Length;
        char[] chars;
        if (d < 0)
        {
            if (len <= scale + 1)
            {
                var n = scale - len + 4;
                chars = new char[scale + 3];
                s.CopyTo(1, chars, n, len - 1);
                for (var i = 1; i < n; i++) chars[i] = '0';
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
                var n = scale - len + 2;
                chars = new char[scale + 2];
                s.CopyTo(0, chars, n, len);
                for (var i = 0; i < n; i++) chars[i] = '0';
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

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.normalized(long)</c>.</remarks>
    public static long Normalized(long d)
    {
        if (d == Invalid) return Invalid;
        var scale = (int)d & 15;
        var v = d >> 4;
        while (scale > 0)
        {
            var x = v / 10;
            if (x * 10 != v) break;
            scale--;
            v = x;
        }
        return (v << 4) | scale;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.intValue()</c>.</remarks>
    public int IntValue => ToInt(_value);

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.longValue()</c>.</remarks>
    public long LongValue => ToLong(_value);

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.floatValue()</c>.</remarks>
    public float FloatValue => ToFloat(_value);

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.doubleValue()</c>.</remarks>
    public double DoubleValue => ToDouble(_value);

    /// <remarks>Ported from Java <c>com.clarisma.common.math.Decimal.toString()</c>.</remarks>
    public override string ToString()
    {
        return ToString(_value);
    }

}
