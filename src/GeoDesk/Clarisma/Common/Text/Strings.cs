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

public static class Strings
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

    public static string Escape(string s)
    {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            char chEscaped = Escape(ch);
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

    public static string Unescape(string s, bool trimQuotes)
    {
        StringBuilder buf = new StringBuilder();
        int len = s.Length - (trimQuotes ? 1 : 0);
        int i = trimQuotes ? 1 : 0;
        for (; i < len; i++)
        {
            char ch = s[i];
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
    public static Type? NumberType(string s)
    {
        s = s.Trim();
        bool minus = false;
        bool decimalSeen = false;
        bool digits = false;
        int n = 0;
        for (; n < s.Length; n++)
        {
            char ch = s[n];
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

    public static bool IsAsciiLetter(char ch)
    {
        return (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');
    }

    public static bool IsIdentifier(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
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

    public static bool Equals(string? a, string? b)
    {
        if (a == null || b == null) return ReferenceEquals(a, b);
        return a.Equals(b);
    }

    /// <summary>
    /// Formats a floating-point number. Unlike Double.ToString,
    /// omits the decimal point if this number is not fractional.
    /// </summary>
    public static string FormatSimpleDouble(double d)
    {
        int i = (int)d;
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
    public static string MakeKey(string s)
    {
        StringBuilder buf = new StringBuilder();
        s = StripAccents(s);
        bool needsHyphen = false;
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
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

    public static bool IsAscii(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] >= 128) return false;
        }
        return true;
    }

    public static string StripAccents(string original)
    {
        string s = original.Normalize(NormalizationForm.FormD);
        if (IsAscii(s)) return original;
        StringBuilder buf = new StringBuilder();
        bool uppercase = false;
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
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

    public static int LongestCommonSubstring(string a, string b)
    {
        int m = a.Length;
        int n = b.Length;

        int max = 0;

        int[,] dp = new int[m, n];

        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
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
    public static string Join<T>(string delimiter, IEnumerable<T> items)
    {
        StringBuilder joiner = new StringBuilder();
        bool first = true;
        foreach (T item in items)
        {
            if (!first) joiner.Append(delimiter);
            joiner.Append(item == null ? "null" : item.ToString());
            first = false;
        }
        return joiner.ToString();
    }

    // TODO: remove
    public static string Join<T>(string delimiter, IEnumerable<T> items, Func<T, object?> formatter)
    {
        StringBuilder joiner = new StringBuilder();
        bool first = true;
        foreach (T item in items)
        {
            if (!first) joiner.Append(delimiter);
            object? v = formatter(item);
            joiner.Append(v == null ? "null" : v.ToString());
            first = false;
        }
        return joiner.ToString();
    }

    public static int IndexOfAny(string s, string any)
    {
        for (int i = 0; i < s.Length; i++)
        {
            if (any.IndexOf(s[i]) >= 0) return i;
        }
        return -1;
    }

    public static int CountChar(string s, char ch)
    {
        int count = 0;
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == ch) count++;
        }
        return count;
    }

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
    public static string EscapeForJson(string s)
    {
        // Check if any character needs escaping.
        bool needsEscaping = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"' || c == '\\' || c < ' ')
            {
                needsEscaping = true;
                break;
            }
        }

        // Return original string if no escaping needed
        if (!needsEscaping) return s;

        // Escape necessary characters
        StringBuilder escaped = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
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
    public static string CleanString(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c < ' ')
            {
                StringBuilder processed = new StringBuilder(s.Length);
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
