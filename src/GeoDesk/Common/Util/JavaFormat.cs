/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using System.Text;

namespace GeoDesk.Common.Util;

/// <summary>
/// Minimal emulation of Java's <c>String.format</c> / <c>printf</c>-style formatting,
/// covering the conversion specifiers actually used by the GeoDesk codebase
/// (<c>%s %d %f %x %X %c %% </c> with optional flags, width and precision).
///
/// This is a port aid; it is not a complete implementation of
/// <c>java.util.Formatter</c>.
/// </summary>
/// <remarks>Port-only helper (no direct Java counterpart): a stand-in for <c>java.util.Formatter</c>.</remarks>
internal static class JavaFormat
{

    /// <summary>
    /// Formats a string using Java-style <c>printf</c> conversion specifiers, parsing optional flags,
    /// width, and precision and substituting the supplied arguments. Supports the
    /// <c>%s %d %f %x %X %c %b %% %n</c> specifiers used by the codebase.
    /// </summary>
    /// <remarks>Port-only: emulates Java's <c>String.format(String, Object...)</c> for the used specifiers.</remarks>
    public static string Format(string format, params object?[] args)
    {
        var sb = new StringBuilder(format.Length + 16);
        var argIndex = 0;
        var i = 0;
        var len = format.Length;
        while (i < len)
        {
            var ch = format[i++];
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
            var width = -1;
            var widthStart = i;
            while (i < len && char.IsDigit(format[i])) i++;
            if (i > widthStart) width = int.Parse(format.Substring(widthStart, i - widthStart), CultureInfo.InvariantCulture);

            var precision = -1;
            if (i < len && format[i] == '.')
            {
                i++;
                var precStart = i;
                while (i < len && char.IsDigit(format[i])) i++;
                precision = int.Parse(format.Substring(precStart, i - precStart), CultureInfo.InvariantCulture);
            }

            if (i >= len) break;
            var conv = format[i++];

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

            var arg = argIndex < args.Length ? args[argIndex++] : null;
            var flagStr = flags.ToString();
            var leftJustify = flagStr.IndexOf('-') >= 0;
            var zeroPad = flagStr.IndexOf('0') >= 0;
            var grouping = flagStr.IndexOf(',') >= 0;
            var plus = flagStr.IndexOf('+') >= 0;

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
                    var v = ToLong(arg);
                    text = grouping
                        ? v.ToString("#,##0", CultureInfo.InvariantCulture)
                        : v.ToString(CultureInfo.InvariantCulture);
                    if (plus && v >= 0) text = "+" + text;
                    break;
                }
                case 'f':
                {
                    var v = ToDouble(arg);
                    var p = precision >= 0 ? precision : 6;
                    text = v.ToString((grouping ? "#,##0." : "0.") + new string('0', p), CultureInfo.InvariantCulture);
                    if (plus && v >= 0) text = "+" + text;
                    break;
                }
                case 'x':
                {
                    var v = ToLong(arg);
                    text = v.ToString("x", CultureInfo.InvariantCulture);
                    break;
                }
                case 'X':
                {
                    var v = ToLong(arg);
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
                var pad = width - text.Length;
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

    /// <summary>
    /// Coerces a boxed numeric (or null) argument to a <see cref="long"/> for the integer and
    /// hexadecimal specifiers.
    /// </summary>
    /// <remarks>Port-only: coerces a boxed numeric argument to <c>long</c> for integer specifiers.</remarks>
    static long ToLong(object? arg)
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

    /// <summary>
    /// Coerces a boxed numeric (or null) argument to a <see cref="double"/> for the <c>%f</c> specifier.
    /// </summary>
    /// <remarks>Port-only: coerces a boxed numeric argument to <c>double</c> for the <c>%f</c> specifier.</remarks>
    static double ToDouble(object? arg)
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
