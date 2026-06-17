/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

// WIP: simplified scannerless parser

using System.Globalization;
using Clarisma.Common.Math;

namespace Clarisma.Common.Parser;

public class SimpleParser
{
    private readonly string buf;
    private int pos;
    private char nextChar;

    public record Schema(long FirstLower, long FirstUpper, long SubsequentLower, long SubsequentUpper);

    public SimpleParser(string s)
    {
        buf = s;
        SkipWhitespace();
    }

    private void SkipWhitespace()
    {
        SkipWhitespace(pos >= buf.Length ? (char)0xFFFF : buf[pos]);
    }

    private void SkipWhitespace(char ch)
    {
        while (ch <= ' ')
        {
            ch = Advance();
        }
        nextChar = ch;
    }

    private char Advance()
    {
        pos++;
        return pos < buf.Length ? buf[pos] : (char)0xFFFF;
    }

    public char NextChar => nextChar;

    /// <summary>
    /// Attempts to match an identifier that conforms to the given schema.
    /// </summary>
    /// <param name="schema">the identifier schema</param>
    /// <returns>the identifier, or null if the current token does not conform</returns>
    public string? Identifier(Schema schema)
    {
        char ch = nextChar;
        long lowerBits = schema.FirstLower;
        long upperBits = schema.FirstUpper;
        if (ch < 128)
        {
            long bits = ch < 64 ? lowerBits : upperBits;
            if ((bits & (1L << ch)) == 0) return null;
            // Important to use long constant, so the lower 6 bits of ch
            // will be used for shift
        }
        else
        {
            if (ch == 0xFFFF) return null;
            if (!char.IsLetter(ch)) return null;
        }
        lowerBits = schema.SubsequentLower;
        upperBits = schema.SubsequentUpper;
        int start = pos;

        for (; ; )
        {
            ch = Advance();
            if (ch < 128)
            {
                long bits = ch < 64 ? lowerBits : upperBits;
                if ((bits & (1L << ch)) == 0) break;
            }
            else
            {
                if (ch == 0xFFFF) break;
                if (!char.IsLetter(ch)) break;
            }
        }
        string s = buf.Substring(start, pos - start);
        SkipWhitespace(ch);
        return s;
    }

    protected void Error(string msg)
    {
        throw new ParserException(LineColString() + msg);
    }

    protected long LineCol()
    {
        int line = 1;
        int col = 1;
        for (int n = 0; n < pos; n++)
        {
            switch (buf[n])
            {
                case '\n':
                    col = 1;
                    line++;
                    break;
                case '\r':
                    break;
                default:
                    col++;
                    break;
            }
        }
        return (long)line | (((long)col) << 32);
    }

    protected string LineColString()
    {
        long lineCol = LineCol();
        return string.Format(CultureInfo.InvariantCulture, "[{0}:{1}] ", (int)lineCol, (int)((ulong)lineCol >> 32));
    }

    protected long MatchQuoted()
    {
        char quoteChar = nextChar;
        if (quoteChar != '\'' && quoteChar != '\"') return 0;
        pos++;
        int start = pos;
        for (; ; )
        {
            if (pos >= buf.Length)
            {
                Error("Unterminated string literal");
                return -1;
            }
            char ch = buf[pos];
            if (ch == quoteChar)
            {
                nextChar = ch;
                break;
            }
            if (ch == '\\') pos++;
            pos++;
        }
        long range = (((long)pos) << 32) | (uint)start;
        SkipWhitespace();
        return range;
    }

    /// <summary>
    /// Attempts to match a string value (in single or double quotes).
    /// </summary>
    /// <returns>
    /// the raw, unescaped string value (without the enclosing quotes),
    /// or null if the current token is not a quote-enclosed string
    /// </returns>
    public string? RawString()
    {
        long range = MatchQuoted();
        if (range <= 0) return null;
        int start = (int)range;
        int end = (int)((ulong)range >> 32);
        return buf.Substring(start, end - start);
    }

    /// <summary>
    /// Attempts to match the given character. If match is successful, advances
    /// to the next token.
    /// </summary>
    public bool Literal(char ch)
    {
        if (nextChar != ch) return false;
        nextChar = Advance();
        SkipWhitespace();
        return true;
    }

    /// <summary>
    /// Attempts to match the given string. If match is successful, advances
    /// to the next token.
    /// </summary>
    public bool Literal(string s)
    {
        int len = s.Length;
        if (pos + len >= buf.Length) return false;
        if (string.CompareOrdinal(buf, pos, s, 0, len) == 0)
        {
            pos += len;
            SkipWhitespace();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to match a number. If successful, advances to next token.
    /// </summary>
    /// <returns>the number value, or NaN if the current token is not a number</returns>
    public double Number()
    {
        // first char must be - or . or 0-9
        char first = (char)(nextChar - 43);
        if (first > 14) return double.NaN;
        if ((0b111111111101100 & (1 << first)) == 0) return double.NaN;
        char ch = nextChar;
        double value = 0;
        bool negative = false;
        int decimalPos = -1;
        bool seenDigit = false;
        int n = pos;
        if (ch == '-')
        {
            negative = true;
            n++;
            if (n >= buf.Length) return double.NaN;
            ch = buf[n];
        }
        for (; ; )
        {
            if (ch == '.')
            {
                if (decimalPos >= 0) break;
                decimalPos = n;
            }
            else
            {
                value = value * 10 + (ch - '0');
                seenDigit = true;
            }
            n++;
            ch = (n < buf.Length) ? buf[n] : (char)0xFFFF;
            char next = (char)(ch - 43);
            if (next > 14) break;
            if ((0b111111111101000 & (1 << next)) == 0) break;

            // TODO: exponent
        }
        if (!seenDigit) return double.NaN;
        if (negative) value = -value;
        if (decimalPos >= 0) value /= MathUtils.Pow10(n - decimalPos - 1);
        pos = n;
        SkipWhitespace(ch);
        return value;
    }
}
