/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

// WIP: simplified scannerless parser

using System.Globalization;

using GeoDesk.Common.Math;

namespace GeoDesk.Common.Parser;

/// <summary>
/// A minimal scannerless parser that reads tokens (identifiers, quoted strings, numeric literals,
/// and character/string literals) directly off a string, skipping whitespace between them and
/// reporting line/column positions for errors. Identifier character sets are described by a bitmask
/// <see cref="Schema"/>.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser</c>.</remarks>
internal class SimpleParser
{

    readonly string _buf;
    int _pos;
    char _nextChar;

    /// <summary>
    /// Bitmask description of which ASCII characters are valid as the first and as subsequent
    /// characters of an identifier, split into lower (0-63) and upper (64-127) halves.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.Schema</c>.</remarks>
    public record Schema(long FirstLower, long FirstUpper, long SubsequentLower, long SubsequentUpper);

    /// <summary>
    /// Creates a parser over the given source string and positions it at the first non-whitespace
    /// character.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser(String)</c>.</remarks>
    public SimpleParser(string s)
    {
        _buf = s;
        SkipWhitespace();
    }

    /// <summary>
    /// Skips whitespace starting from the current position, updating the lookahead character.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.skipWhitespace()</c>.</remarks>
    void SkipWhitespace()
    {
        SkipWhitespace(_pos >= _buf.Length ? (char)0xFFFF : _buf[_pos]);
    }

    /// <summary>
    /// Skips whitespace beginning from the given character and stores the first non-whitespace
    /// character as the lookahead.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.skipWhitespace(char)</c>.</remarks>
    void SkipWhitespace(char ch)
    {
        while (ch <= ' ')
        {
            ch = Advance();
        }
        _nextChar = ch;
    }

    /// <summary>
    /// Advances the position by one and returns the next character, or 0xFFFF at end of input.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.advance()</c>.</remarks>
    char Advance()
    {
        _pos++;
        return _pos < _buf.Length ? _buf[_pos] : (char)0xFFFF;
    }

    /// <summary>
    /// The current lookahead character (the next non-whitespace character to be parsed).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.nextChar()</c>.</remarks>
    public char NextChar => _nextChar;

    /// <summary>
    /// Attempts to match an identifier that conforms to the given schema
    /// (The schema describes which characters that are valid for the first
    /// and subsequent identifier characters).
    /// </summary>
    /// <param name="schema">the identifier schema</param>
    /// <returns>the identifier, or null if the current token does not conform to the given schema</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.identifier(Schema)</c>.</remarks>
    public string? Identifier(Schema schema)
    {
        var ch = _nextChar;
        var lowerBits = schema.FirstLower;
        var upperBits = schema.FirstUpper;
        if (ch < 128)
        {
            var bits = ch < 64 ? lowerBits : upperBits;
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
        var start = _pos;

        for (; ; )
        {
            ch = Advance();
            if (ch < 128)
            {
                var bits = ch < 64 ? lowerBits : upperBits;
                if ((bits & (1L << ch)) == 0) break;
            }
            else
            {
                if (ch == 0xFFFF) break;
                if (!char.IsLetter(ch)) break;
            }
        }
        var s = _buf.Substring(start, _pos - start);
        SkipWhitespace(ch);
        return s;
    }

    /// <summary>
    /// Reports a parse error at the current position by throwing a <see cref="ParserException"/>
    /// prefixed with the line and column.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.error(String)</c>.</remarks>
    protected void Error(string msg)
    {
        throw new ParserException(LineColString() + msg);
    }

    /// <summary>
    /// Computes the current 1-based line and column from the buffer position, packing the line into
    /// the low 32 bits and the column into the high 32 bits.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.lineCol()</c>.</remarks>
    protected long LineCol()
    {
        var line = 1;
        var col = 1;
        for (var n = 0; n < _pos; n++)
        {
            switch (_buf[n])
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

    /// <summary>
    /// Returns the current position as a bracketed <c>[line:column]</c> prefix string for error messages.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.lineColString()</c>.</remarks>
    protected string LineColString()
    {
        var lineCol = LineCol();
        return string.Format(CultureInfo.InvariantCulture, "[{0}:{1}] ", (int)lineCol, (int)((ulong)lineCol >> 32));
    }

    /// <summary>
    /// Matches a single- or double-quoted string at the current position, returning the start and end
    /// offsets of the content packed into a long (start in the low bits, end in the high bits), or 0
    /// if the current token is not a quote. Reports an error on an unterminated literal.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.matchQuoted()</c>.</remarks>
    protected long MatchQuoted()
    {
        var quoteChar = _nextChar;
        if (quoteChar != '\'' && quoteChar != '\"') return 0;
        _pos++;
        var start = _pos;
        for (; ; )
        {
            if (_pos >= _buf.Length)
            {
                Error("Unterminated string literal");
                return -1;
            }
            var ch = _buf[_pos];
            if (ch == quoteChar)
            {
                _nextChar = ch;
                break;
            }
            if (ch == '\\') _pos++;
            _pos++;
        }
        var range = (((long)_pos) << 32) | (uint)start;
        SkipWhitespace();
        return range;
    }

    /// <summary>
    /// Attempts to match a string value (in single or double quotes).
    /// If successful, advances to the next token, If the string is unclosed,
    /// generates an error.
    /// </summary>
    /// <returns>
    /// the raw, unescaped string value (without the enclosing quotes),
    /// or null if the current token is not a quote-enclosed string
    /// </returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.rawString()</c>.</remarks>
    public string? RawString()
    {
        var range = MatchQuoted();
        if (range <= 0) return null;
        var start = (int)range;
        var end = (int)((ulong)range >> 32);
        return _buf.Substring(start, end - start);
    }

    /// <summary>
    /// Attempts to match the given character. If match is successful, advances
    /// to the next token.
    /// </summary>
    /// <param name="ch">the character to match</param>
    /// <returns>true if matched</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.literal(char)</c>.</remarks>
    public bool Literal(char ch)
    {
        if (_nextChar != ch) return false;
        _nextChar = Advance();
        SkipWhitespace();
        return true;
    }

    /// <summary>
    /// Attempts to match the given string. If match is successful, advances
    /// to the next token.
    /// </summary>
    /// <param name="s">the string to match</param>
    /// <returns>true if matched</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.literal(String)</c>.</remarks>
    public bool Literal(string s)
    {
        var len = s.Length;
        if (_pos + len >= _buf.Length) return false;
        if (string.CompareOrdinal(_buf, _pos, s, 0, len) == 0)
        {
            _pos += len;
            SkipWhitespace();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to match a number. If successful, advances to next token.
    /// </summary>
    /// <returns>the number value, or NaN if the current token is not a number</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.SimpleParser.number()</c>.</remarks>
    public double Number()
    {
        // first char must be - or . or 0-9
        var first = (char)(_nextChar - 43);
        if (first > 14) return double.NaN;
        if ((0b111111111101100 & (1 << first)) == 0) return double.NaN;
        var ch = _nextChar;
        double value = 0;
        var negative = false;
        var decimalPos = -1;
        var seenDigit = false;
        var n = _pos;
        if (ch == '-')
        {
            negative = true;
            n++;
            if (n >= _buf.Length) return double.NaN;
            ch = _buf[n];
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
            ch = (n < _buf.Length) ? _buf[n] : (char)0xFFFF;
            var next = (char)(ch - 43);
            if (next > 14) break;
            if ((0b111111111101000 & (1 << next)) == 0) break;

            // TODO: exponent
        }
        if (!seenDigit) return double.NaN;
        if (negative) value = -value;
        if (decimalPos >= 0) value /= MathUtils.Pow10(n - decimalPos - 1);
        _pos = n;
        SkipWhitespace(ch);
        return value;
    }

}
