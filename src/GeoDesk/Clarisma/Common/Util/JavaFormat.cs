/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using System.Text;

namespace Clarisma.Common.Util;

/// <summary>
/// Minimal emulation of Java's <c>String.format</c> / <c>printf</c>-style formatting,
/// covering the conversion specifiers actually used by the GeoDesk codebase
/// (<c>%s %d %f %x %X %c %% </c> with optional flags, width and precision).
///
/// This is a port aid; it is not a complete implementation of
/// <c>java.util.Formatter</c>.
/// </summary>
/// <remarks>Port-only helper (no direct Java counterpart): a stand-in for <c>java.util.Formatter</c>.</remarks>
public static class JavaFormat
{
    public static string Format(string format, params object?[] args)
    {
        var sb = new StringBuilder(format.Length + 16);
        int argIndex = 0;
        int i = 0;
        int len = format.Length;
        while (i < len)
        {
            char ch = format[i++];
            if (ch != '%')
            {
                sb.Append(ch);
                continue;
            }

            // Parse: %[flags][width][.precision]conversion
            var flags = new StringBuilder();
            while (i < len && "-+ 0,#".IndexOf(format[i]) >= 0)
            {
                flags.Append(format[i++]);
            }
            int width = -1;
            int widthStart = i;
            while (i < len && char.IsDigit(format[i])) i++;
            if (i > widthStart) width = int.Parse(format.Substring(widthStart, i - widthStart), CultureInfo.InvariantCulture);

            int precision = -1;
            if (i < len && format[i] == '.')
            {
                i++;
                int precStart = i;
                while (i < len && char.IsDigit(format[i])) i++;
                precision = int.Parse(format.Substring(precStart, i - precStart), CultureInfo.InvariantCulture);
            }

            if (i >= len) break;
            char conv = format[i++];

            if (conv == '%')
            {
                sb.Append('%');
                continue;
            }
            if (conv == 'n')
            {
                sb.Append('\n');
                continue;
            }

            object? arg = argIndex < args.Length ? args[argIndex++] : null;
            string flagStr = flags.ToString();
            bool leftJustify = flagStr.IndexOf('-') >= 0;
            bool zeroPad = flagStr.IndexOf('0') >= 0;
            bool grouping = flagStr.IndexOf(',') >= 0;
            bool plus = flagStr.IndexOf('+') >= 0;

            string text;
            switch (conv)
            {
                case 's':
                case 'S':
                    text = arg?.ToString() ?? "null";
                    if (precision >= 0 && text.Length > precision) text = text.Substring(0, precision);
                    if (conv == 'S') text = text.ToUpperInvariant();
                    break;
                case 'd':
                {
                    long v = ToLong(arg);
                    text = grouping
                        ? v.ToString("#,##0", CultureInfo.InvariantCulture)
                        : v.ToString(CultureInfo.InvariantCulture);
                    if (plus && v >= 0) text = "+" + text;
                    break;
                }
                case 'f':
                {
                    double v = ToDouble(arg);
                    int p = precision >= 0 ? precision : 6;
                    text = v.ToString((grouping ? "#,##0." : "0.") + new string('0', p), CultureInfo.InvariantCulture);
                    if (plus && v >= 0) text = "+" + text;
                    break;
                }
                case 'x':
                {
                    long v = ToLong(arg);
                    text = v.ToString("x", CultureInfo.InvariantCulture);
                    break;
                }
                case 'X':
                {
                    long v = ToLong(arg);
                    text = v.ToString("X", CultureInfo.InvariantCulture);
                    break;
                }
                case 'c':
                    text = arg is char c ? c.ToString() : (arg is int ci ? ((char)ci).ToString() : arg?.ToString() ?? "");
                    break;
                case 'b':
                case 'B':
                    text = (arg is bool b ? b : arg != null).ToString().ToLowerInvariant();
                    if (conv == 'B') text = text.ToUpperInvariant();
                    break;
                default:
                    text = arg?.ToString() ?? "null";
                    break;
            }

            if (width > text.Length)
            {
                int pad = width - text.Length;
                if (leftJustify)
                {
                    sb.Append(text).Append(' ', pad);
                }
                else if (zeroPad && (conv == 'd' || conv == 'f' || conv == 'x' || conv == 'X'))
                {
                    // keep a leading sign in front of the zero padding
                    if (text.Length > 0 && (text[0] == '-' || text[0] == '+'))
                    {
                        sb.Append(text[0]).Append('0', pad).Append(text, 1, text.Length - 1);
                    }
                    else
                    {
                        sb.Append('0', pad).Append(text);
                    }
                }
                else
                {
                    sb.Append(' ', pad).Append(text);
                }
            }
            else
            {
                sb.Append(text);
            }
        }
        return sb.ToString();
    }

    private static long ToLong(object? arg)
    {
        return arg switch
        {
            null => 0,
            long l => l,
            int n => n,
            short s => s,
            byte by => by,
            uint u => u,
            _ => Convert.ToInt64(arg, CultureInfo.InvariantCulture)
        };
    }

    private static double ToDouble(object? arg)
    {
        return arg switch
        {
            null => 0,
            double d => d,
            float f => f,
            long l => l,
            int n => n,
            _ => Convert.ToDouble(arg, CultureInfo.InvariantCulture)
        };
    }
}
