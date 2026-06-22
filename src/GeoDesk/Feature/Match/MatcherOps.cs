/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using GeoDesk.Common.Math;
using GeoDesk.Common.Store;
using GeoDesk.Common.Util;
using GeoDesk.Feature.Store;
using GeoDesk.Feature.Store.Format;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A query literal in both forms a comparison may need: the original text (for comparing against the text of
/// a non-local value) and its UTF-8 bytes (for comparing against a local string byte-for-byte). For a literal
/// that is itself a global string, <see cref="GlobalCode"/> carries its code so equality against a
/// global-string value reduces to an integer compare. Built once (when a matcher is constructed / compiled)
/// so no encoding happens per feature.
/// </summary>
internal sealed class MatchLiteral
{
    public readonly string Text;
    public readonly byte[] Utf8;
    public readonly int GlobalCode; // the literal's global-string code, or -1 if it is not a global string

    public MatchLiteral(string text, int globalCode = -1)
    {
        Text = text;
        Utf8 = Encoding.UTF8.GetBytes(text);
        GlobalCode = globalCode;
    }
}

/// <summary>
/// A non-ref projection of a tag located by <see cref="MatcherOps.FindTagGlobal"/> /
/// <see cref="MatcherOps.FindTagLocal"/>. The local-string value rides along as a
/// <see cref="ReadOnlyMemory{Byte}"/> (a normal struct the expression tree can hold) rather than a span.
/// </summary>
internal readonly struct TagMatch
{
    public readonly bool Present;
    public readonly int Kind;                  // a TagValues kind
    public readonly int ValueCode;             // narrow number / global-string code / wide-number code
    public readonly ReadOnlyMemory<byte> ValueBytes; // the local-string value (length-prefixed), else empty

    public TagMatch(bool present, int kind, int valueCode, ReadOnlyMemory<byte> valueBytes)
    {
        Present = present;
        Kind = kind;
        ValueCode = valueCode;
        ValueBytes = valueBytes;
    }
}

/// <summary>
/// The concrete tag-table and value operations shared by both <see cref="Matcher"/> implementations: the
/// <see cref="ExpressionTagMatcher"/>'s compiled tree calls these from its generated code, and the
/// <see cref="AstTagMatcher"/> interpreter calls them while walking the query. Each takes the feature's
/// <see cref="Segment"/> (or a located value's <see cref="ReadOnlyMemory{Byte}"/>) and <c>.Span</c>s it where
/// it drives the span-based <see cref="TagTableReader"/> — the single source of truth for the binary layout.
/// String/number comparisons stay allocation-free: literals are pre-encoded (<see cref="MatchLiteral"/>),
/// local strings are compared as UTF-8 bytes, and numbers are formatted into a stack buffer.
/// </summary>
/// <remarks>Port-only: the .NET stand-in for the per-tag/value operations the Java MatcherCoder inlines as bytecode.</remarks>
internal static class MatcherOps
{

    /// <summary>Finds the tag with the given global key code, projecting its value to scalars + value memory.</summary>
    public static TagMatch FindTagGlobal(Segment segment, int pFeature, int keyCode)
    {
        var reader = new TagTableReader(segment.Memory.Span, pFeature);
        while (reader.MoveNext())
        {
            if (reader.KeyCode == keyCode)
            {
                var value = reader.Kind == TagValues.LOCAL_STRING ? segment.Memory.Slice(reader.ValueStringPos) : default;
                return new TagMatch(true, reader.Kind, reader.ValueCode, value);
            }
        }
        return default;
    }

    /// <summary>Finds the tag with the given local (uncommon) key name (precomputed UTF-8), projecting its value.</summary>
    public static TagMatch FindTagLocal(Segment segment, int pFeature, byte[] keyNameUtf8)
    {
        var reader = new TagTableReader(segment.Memory.Span, pFeature);
        while (reader.MoveNext())
        {
            if (reader.KeyCode == 0 && reader.KeyBytes.SequenceEqual(keyNameUtf8))
            {
                var value = reader.Kind == TagValues.LOCAL_STRING ? segment.Memory.Slice(reader.ValueStringPos) : default;
                return new TagMatch(true, reader.Kind, reader.ValueCode, value);
            }
        }
        return default;
    }

    /// <summary>Decodes a located tag value as a double (numeric kinds directly, strings leniently).</summary>
    public static double TagDouble(ReadOnlyMemory<byte> value, int kind, int valueCode, GlobalStringTable globalStrings)
    {
        switch (kind)
        {
            case TagValues.NARROW_NUMBER:
                return valueCode + TagValues.MIN_NUMBER;
            case TagValues.GLOBAL_STRING:
                return MathUtils.DoubleFromString(globalStrings.Utf8(valueCode).Span);
            case TagValues.WIDE_NUMBER:
                return TagValues.WideNumberToDouble(valueCode);
            default: // LOCAL_STRING
                return MathUtils.DoubleFromString(Bytes.ReadUtf8String(value.Span, 0));
        }
    }

    /// <summary>Tests a located tag value for ordinal equality with a literal.</summary>
    public static bool TagValueEquals(ReadOnlyMemory<byte> value, int kind, int valueCode, GlobalStringTable globalStrings, MatchLiteral literal)
    {
        switch (kind)
        {
            case TagValues.LOCAL_STRING:
                return Bytes.ReadUtf8String(value.Span, 0).SequenceEqual(literal.Utf8);
            case TagValues.GLOBAL_STRING:
                // global value: integer code compare when the literal is itself a global string, otherwise
                // compare the table's UTF-8 bytes against the literal's UTF-8 (no decode, no allocation).
                return literal.GlobalCode >= 0
                    ? valueCode == literal.GlobalCode
                    : globalStrings.Utf8(valueCode).Span.SequenceEqual(literal.Utf8);
            default: // number value vs string literal: compare its decimal text (rare).
                Span<char> nb = stackalloc char[32];
                return FormatNumber(kind, valueCode, nb).SequenceEqual(literal.Text.AsSpan());
        }
    }

    /// <summary>Tests whether a located tag value starts with a literal (ordinal).</summary>
    public static bool TagValueStartsWith(ReadOnlyMemory<byte> value, int kind, int valueCode, GlobalStringTable globalStrings, MatchLiteral literal)
    {
        switch (kind)
        {
            case TagValues.LOCAL_STRING:
                return Bytes.ReadUtf8String(value.Span, 0).StartsWith(literal.Utf8);
            case TagValues.GLOBAL_STRING:
                return globalStrings.Utf8(valueCode).Span.StartsWith(literal.Utf8);
            default:
                Span<char> nb = stackalloc char[32];
                return FormatNumber(kind, valueCode, nb).StartsWith(literal.Text.AsSpan());
        }
    }

    /// <summary>Tests whether a located tag value ends with a literal (ordinal).</summary>
    public static bool TagValueEndsWith(ReadOnlyMemory<byte> value, int kind, int valueCode, GlobalStringTable globalStrings, MatchLiteral literal)
    {
        switch (kind)
        {
            case TagValues.LOCAL_STRING:
                return Bytes.ReadUtf8String(value.Span, 0).EndsWith(literal.Utf8);
            case TagValues.GLOBAL_STRING:
                return globalStrings.Utf8(valueCode).Span.EndsWith(literal.Utf8);
            default:
                Span<char> nb = stackalloc char[32];
                return FormatNumber(kind, valueCode, nb).EndsWith(literal.Text.AsSpan());
        }
    }

    /// <summary>Tests whether a located tag value contains a literal (ordinal).</summary>
    public static bool TagValueContains(ReadOnlyMemory<byte> value, int kind, int valueCode, GlobalStringTable globalStrings, MatchLiteral literal)
    {
        switch (kind)
        {
            case TagValues.LOCAL_STRING:
                return Bytes.ReadUtf8String(value.Span, 0).IndexOf(literal.Utf8) >= 0;
            case TagValues.GLOBAL_STRING:
                return globalStrings.Utf8(valueCode).Span.IndexOf(literal.Utf8) >= 0;
            default:
                Span<char> nb = stackalloc char[32];
                return FormatNumber(kind, valueCode, nb).IndexOf(literal.Text.AsSpan()) >= 0;
        }
    }

    /// <summary>Tests a located tag value against a regex (Java <c>Pattern.matches()</c> full-string semantics).</summary>
    public static bool TagValueMatches(ReadOnlyMemory<byte> value, int kind, int valueCode, GlobalStringTable globalStrings, Regex regex)
    {
        if (kind == TagValues.GLOBAL_STRING)
            return RegexFull(regex, globalStrings.Text(valueCode));

        if (kind == TagValues.LOCAL_STRING)
        {
            var local = Bytes.ReadUtf8String(value.Span, 0);
            var max = Encoding.UTF8.GetMaxCharCount(local.Length);
            char[]? rented = null;
            Span<char> buf = max <= 256 ? stackalloc char[256] : (rented = ArrayPool<char>.Shared.Rent(max));
            try
            {
                var n = Encoding.UTF8.GetChars(local, buf);
                return RegexFull(regex, buf.Slice(0, n));
            }
            finally
            {
                if (rented != null)
                    ArrayPool<char>.Shared.Return(rented);
            }
        }

        Span<char> nb = stackalloc char[32];
        return RegexFull(regex, FormatNumber(kind, valueCode, nb));
    }

    /// <summary>Formats a narrow/wide number into the supplied span (mirrors the TagValues string forms).</summary>
    static ReadOnlySpan<char> FormatNumber(int kind, int valueCode, Span<char> buf)
    {
        if (kind == TagValues.NARROW_NUMBER)
        {
            (valueCode + TagValues.MIN_NUMBER).TryFormat(buf, out var n, default, CultureInfo.InvariantCulture);
            return buf.Slice(0, n);
        }

        // WIDE_NUMBER
        var scale = valueCode & 3;
        if (scale == 0)
        {
            var mantissa = (int)((uint)valueCode >> 2) + TagValues.MIN_NUMBER;
            mantissa.TryFormat(buf, out var n, default, CultureInfo.InvariantCulture);
            return buf.Slice(0, n);
        }

        var format = scale == 1 ? "F1" : scale == 2 ? "F2" : "F3";
        TagValues.WideNumberToDouble(valueCode).TryFormat(buf, out var nn, format, CultureInfo.InvariantCulture);
        return buf.Slice(0, nn);
    }

    /// <summary>Full-string regex match (Java's <c>Pattern.matches()</c>) over a char span.</summary>
    static bool RegexFull(Regex rx, ReadOnlySpan<char> input)
    {
        foreach (var m in rx.EnumerateMatches(input))
            return m.Index == 0 && m.Length == input.Length;
        return false;
    }

}
