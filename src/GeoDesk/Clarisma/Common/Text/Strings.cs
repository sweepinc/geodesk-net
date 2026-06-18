/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Clarisma.Common.Text;

/// <remarks>Ported from Java <c>com.clarisma.common.text.Strings</c>.</remarks>
internal static class Strings
{

    /// <summary>
    /// Checks if a character needs to be escaped, and if so,
    /// returns its corresponding escape character. When escaping
    /// entire strings, escape characters need to be preceded
    /// by a backslash.
    /// </summary>
    /// <param name="ch">the character to check</param>
    /// <returns>
    /// its corresponding escape character, or
    /// <c>char.MaxValue</c> if this character does not require escaping
    /// </returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.escape(char)</c>.</remarks>
    public static char Escape(char ch)
    {
        if (ch < 32)
        {
            switch (ch)
            {
                case '\b': return 'b';
                case '\n': return 'n';
                case '\t': return 't';
                case '\f': return 'f';
                case '\r': return 'r';
                case '\0': return '0';
            }
            return char.MaxValue; // TODO: check
        }
        switch (ch)
        {
            case '\'':
            case '\"':
            case '\\':
                return ch;
        }
        return char.MaxValue;
    }

    /// <summary>
    /// Turns an escape character into its true character.
    /// </summary>
    /// <param name="ch">the escape character</param>
    /// <returns>
    /// the actual character, or <c>char.MaxValue</c>
    /// if <paramref name="ch"/> does not represent a valid escape character.
    /// </returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.unescape(char)</c>.</remarks>
    public static char Unescape(char ch)
    {
        switch (ch)
        {
            case 'b': return '\b';
            case 'n': return '\n';
            case 't': return '\t';
            case 'f': return '\f';
            case 'r': return '\r';
            case '\'': return '\'';
            case '\"': return '\"';
            case '\\': return '\\';
            case '0': return '\0';
        }
        return char.MaxValue;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.escape(String)</c>.</remarks>
    public static string Escape(string s)
    {
        var buf = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            var chEscaped = Escape(ch);
            if (chEscaped != char.MaxValue)
            {
                buf.Append('\\');
                buf.Append(chEscaped);
            }
            else
            {
                buf.Append(ch);
            }
        }
        return buf.ToString();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.unescape(String, boolean)</c>.</remarks>
    public static string Unescape(string s, bool trimQuotes)
    {
        var buf = new StringBuilder();
        var len = s.Length - (trimQuotes ? 1 : 0);
        var i = trimQuotes ? 1 : 0;
        for (; i < len; i++)
        {
            var ch = s[i];
            if (ch == '\\')
            {
                i++;
                if (i >= len) break;
                buf.Append(Unescape(s[i]));
            }
            else
            {
                buf.Append(ch);
            }
        }
        return buf.ToString();
    }

    /// <summary>
    /// Checks if a string represents a valid number, and if so,
    /// whether it is integral or floating-point.
    /// </summary>
    /// <param name="s">the string to check</param>
    /// <returns>
    /// <c>typeof(double)</c> if the string is fractional,
    /// <c>typeof(long)</c> if integral, or <c>null</c> if it
    /// is not a valid number.
    /// </returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.numberType(CharSequence)</c>.</remarks>
    public static Type? NumberType(string s)
    {
        s = s.Trim();
        var minus = false;
        var decimalSeen = false;
        var digits = false;
        var n = 0;
        for (; n < s.Length; n++)
        {
            var ch = s[n];
            if (ch == '-')
            {
                if (minus || digits) return null;
                minus = true;
                continue;
            }
            if (ch == '.')
            {
                if (decimalSeen) return null;
                decimalSeen = true;
                continue;
            }
            if (char.IsDigit(ch))
            {
                digits = true;
                continue;
            }
            return null;
        }
        if (!digits) return null;
        return decimalSeen ? typeof(double) : typeof(long);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.isAsciiLetter(char)</c>.</remarks>
    public static bool IsAsciiLetter(char ch)
    {
        return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.isIdentifier(String)</c>.</remarks>
    public static bool IsIdentifier(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (i == 0)
            {
                if (!IsAsciiLetter(ch) && ch != '_') return false;
            }
            else
            {
                if (!IsAsciiLetter(ch) &&
                    !char.IsDigit(ch) &&
                    ch != '_') return false;
            }
        }
        return true;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.equals(String, String)</c>.</remarks>
    public static bool Equals(string? a, string? b)
    {
        if (a == null || b == null) return ReferenceEquals(a, b);
        return a.Equals(b);
    }

    /// <summary>
    /// Formats a floating-point number. Unlike Double.ToString,
    /// omits the decimal point if this number is not fractional.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.formatSimpleDouble(double)</c>.</remarks>
    public static string FormatSimpleDouble(double d)
    {
        var i = (int)d;
        return i == d
            ? i.ToString(CultureInfo.InvariantCulture)
            : d.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Turns a string into a conforming string key, which is
    /// always lowercase and only contains letters, numbers, and
    /// hyphens.
    ///
    /// "Kirchäckerstraße" ==&gt; "kirchaeckerstrasse"
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.makeKey(String)</c>.</remarks>
    public static string MakeKey(string s)
    {
        var buf = new StringBuilder();
        s = StripAccents(s);
        var needsHyphen = false;
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (char.IsLetter(ch))
            {
                if (needsHyphen)
                {
                    buf.Append('-');
                    needsHyphen = false;
                }
                ch = char.ToLowerInvariant(ch);
                buf.Append(ch);
                continue;
            }
            if (char.IsDigit(ch))
            {
                if (needsHyphen)
                {
                    buf.Append('-');
                    needsHyphen = false;
                }
                buf.Append(ch);
                continue;
            }
            if (buf.Length > 0) needsHyphen = true;
        }
        return buf.ToString();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.isAscii(String)</c>.</remarks>
    public static bool IsAscii(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] >= 128) return false;
        }
        return true;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.stripAccents(String)</c>.</remarks>
    public static string StripAccents(string original)
    {
        var s = original.Normalize(NormalizationForm.FormD);
        if (IsAscii(s)) return original;
        var buf = new StringBuilder();
        var uppercase = false;
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            switch (ch)
            {
                case '̈':
                    buf.Append(uppercase ? 'E' : 'e');
                    break;
                case 'æ':
                    buf.Append("ae");
                    break;
                case 'Æ':
                    buf.Append("AE");
                    break;
                case 'œ':
                    buf.Append("oe");
                    break;
                case 'Œ':
                    buf.Append("OE");
                    break;
                case 'Þ':
                    buf.Append("th");
                    break;
                case 'ð':
                    buf.Append("d");
                    break;
                case 'ß':
                    buf.Append("ss");
                    break;
                default:
                    if (ch < 128)
                    {
                        buf.Append(ch);
                        uppercase = char.IsUpper(ch);
                    }
                    break;
            }
        }
        // TODO: leading/trailing dash, d'
        return buf.ToString();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.longestCommonSubstring(String, String)</c>.</remarks>
    public static int LongestCommonSubstring(string a, string b)
    {
        var m = a.Length;
        var n = b.Length;

        var max = 0;

        var dp = new int[m, n];

        for (var i = 0; i < m; i++)
        {
            for (var j = 0; j < n; j++)
            {
                if (a[i] == b[j])
                {
                    if (i == 0 || j == 0)
                    {
                        dp[i, j] = 1;
                    }
                    else
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    }
                    if (max < dp[i, j]) max = dp[i, j];
                }
            }
        }
        return max;
    }

    // TODO: remove
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.join(String, Iterable)</c>.</remarks>
    public static string Join<T>(string delimiter, IEnumerable<T> items)
    {
        var joiner = new StringBuilder();
        var first = true;
        foreach (var item in items)
        {
            if (!first) joiner.Append(delimiter);
            joiner.Append(item == null ? "null" : item.ToString());
            first = false;
        }
        return joiner.ToString();
    }

    // TODO: remove
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.join(String, Iterable, Function)</c>.</remarks>
    public static string Join<T>(string delimiter, IEnumerable<T> items, Func<T, object?> formatter)
    {
        var joiner = new StringBuilder();
        var first = true;
        foreach (var item in items)
        {
            if (!first) joiner.Append(delimiter);
            var v = formatter(item);
            joiner.Append(v == null ? "null" : v.ToString());
            first = false;
        }
        return joiner.ToString();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.indexOfAny(String, String)</c>.</remarks>
    public static int IndexOfAny(string s, string any)
    {
        for (var i = 0; i < s.Length; i++)
        {
            if (any.IndexOf(s[i]) >= 0) return i;
        }
        return -1;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.countChar(String, char)</c>.</remarks>
    public static int CountChar(string s, char ch)
    {
        var count = 0;
        for (var i = 0; i < s.Length; i++)
        {
            if (s[i] == ch) count++;
        }
        return count;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.uppercaseFirst(String)</c>.</remarks>
    public static string UppercaseFirst(string s)
    {
        if (s.Length == 0) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    /// <summary>
    /// Escapes a string for JSON encoding.
    /// </summary>
    /// <param name="s">The input string to be escaped.</param>
    /// <returns>
    /// The escaped string. If no escaping was needed, returns the original string.
    /// </returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.escapeForJson(String)</c>.</remarks>
    public static string EscapeForJson(string s)
    {
        // Check if any character needs escaping.
        var needsEscaping = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '"' || c == '\\' || c < ' ')
            {
                needsEscaping = true;
                break;
            }
        }

        // Return original string if no escaping needed
        if (!needsEscaping) return s;

        // Escape necessary characters
        var escaped = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            switch (c)
            {
                case '"':
                    escaped.Append("\\\"");
                    break;
                case '\\':
                    escaped.Append("\\\\");
                    break;
                case '/':
                    escaped.Append("\\/");
                    break;
                case '\b':
                    escaped.Append("\\b");
                    break;
                case '\f':
                    escaped.Append("\\f");
                    break;
                case '\n':
                    escaped.Append("\\n");
                    break;
                case '\r':
                    escaped.Append("\\r");
                    break;
                case '\t':
                    escaped.Append("\\t");
                    break;
                default:
                    if (c < ' ') // For other unprintable characters
                    {
                        escaped.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        escaped.Append(c);
                    }
                    break;
            }
        }

        return escaped.ToString();
    }

    /// <summary>
    /// Replaces unprintable/control characters (tab, line feed, etc.)
    /// in the input string with a space.
    /// </summary>
    /// <param name="s">The input string to be processed.</param>
    /// <returns>
    /// The processed string with unprintable characters replaced by spaces.
    /// If no unprintable characters were found, returns the original string.
    /// </returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.text.Strings.cleanString(String)</c>.</remarks>
    public static string CleanString(string s)
    {
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c < ' ')
            {
                var processed = new StringBuilder(s.Length);
                for (i = 0; i < s.Length; i++)
                {
                    c = s[i];
                    processed.Append(c < ' ' ? ' ' : c);
                }
                return processed.ToString();
            }
        }
        return s;
    }

}
