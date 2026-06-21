/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using GeoDesk.Common.Text;
using GeoDesk.Common.Util;

namespace GeoDesk.Common.Parser;

/// <summary>
/// An extendable general-purpose lexer/parser, especially suited
/// for generating abstract syntax trees.
/// </summary>
/// <remarks>
/// Ported from Java <c>com.clarisma.common.parser.Parser</c>. In Java this implements CharSequence and
/// FileLocation; the straight port keeps FileLocation (IFileLocation) and provides
/// Length/CharAt/SubSequence directly rather than implementing a CharSequence interface (which .NET lacks).
/// </remarks>
internal class Parser : IFileLocation
{

    /// <summary>
    /// The file from which the source content originated (used solely for error reporting)
    /// </summary>
    protected string? fileName;
    protected int tabSize = 4;
    /// <summary>The source content</summary>
    protected string buf = "";
    /// <summary>The position where the current token starts</summary>
    protected int pos;
    /// <summary>The line of the current token (1-based)</summary>
    protected int line;
    /// <summary>The column where the current token starts (1-based)</summary>
    protected int column;
    /// <summary>The type of the current token</summary>
    protected object? tokenType;
    /// <summary>The literal value of the current token</summary>
    protected string tokenValue = "";
    /// <summary>A sorted list of all strings that represent tokens...</summary>
    protected List<string> tokenStrings;
    /// <summary>... and a list of the actual tokens these strings represent.</summary>
    protected List<object?> tokens;
    /// <summary>
    /// A mapping of keywords, i.e. strings that might otherwise be considered
    /// identifiers, to the tokens they represent.
    /// </summary>
    protected Dictionary<string, object?> keywordTokens;
    /// <summary>
    /// An index to speed up token matching: each array position represents an ASCII
    /// character (0-127) and contains the index of the last string in tokenStrings
    /// that begins with this character.
    /// </summary>
    protected short[]? initialCharTokens;
    /// <summary>A regex pattern used to match identifiers</summary>
    protected Regex identifierPattern;

    protected const string START = "<start>";
    protected const string END = "<end>";
    protected const string WHITESPACE = "<ws>";
    protected const string QUOTATION_MARK = "\"";
    protected const string STRING = "<string>";
    protected const string INVALID = "<invalid>";
    protected const string INVALID_STRING = "<invalid-string>";
    protected const string COMMENT = "<comment>";
    protected const string NUMBER = "<number>";
    protected const string IDENTIFIER = "<id>";
    protected const string MULTILINE_COMMENT_START = "<multiline-comment-start>";
    protected const string MULTILINE_COMMENT_END = "<multiline-comment-end>";

    /// <summary>
    /// The default identifier pattern: an identifier must contain only letters,
    /// numbers, or underscore characters, and must start with a letter or underscore.
    /// </summary>
    public static readonly Regex DEFAULT_IDENTIFIER_PATTERN =
        new Regex(@"[a-zA-Z_]\w*", RegexOptions.ECMAScript);

    /// <summary>
    /// Initializes the parser with the default identifier pattern and token tables and registers the
    /// default whitespace and quotation tokens.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser()</c>.</remarks>
    public Parser()
    {
        identifierPattern = DEFAULT_IDENTIFIER_PATTERN;
        tokenStrings = new List<string>();
        tokens = new List<object?>();
        keywordTokens = new Dictionary<string, object?>();
        // index 0 is treated specially, so insert a dummy entry
        tokenStrings.Add("");
        tokens.Add(null);
        AddDefaultTokens();
    }

    /// <summary>
    /// Adds a string and its corresponding token to the lexer table.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.addToken(String, Object, boolean)</c>.</remarks>
    public void AddToken(string str, object? token, bool replace)
    {
        var n = tokenStrings.BinarySearch(str, StringComparer.Ordinal);
        if (n < 0)
        {
            n = -n - 1;
            tokenStrings.Insert(n, str);
            tokens.Insert(n, token);
        }
        else
        {
            if (!replace)
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture,
                        "A token has already been assigned " +
                        "for \"{0}\". Use replaceToken() if you " +
                        "want to override it.", str));
            }
            tokens[n] = token;
        }
    }

    /// <summary>
    /// Registers a string and its token, throwing if the string is already mapped (use
    /// <see cref="ReplaceToken"/> to override).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.addToken(String, Object)</c>.</remarks>
    public void AddToken(string str, object? token)
    {
        AddToken(str, token, false);
    }

    /// <summary>
    /// Registers a string/token mapping, replacing any existing mapping for the same string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.replaceToken(String, Object)</c>.</remarks>
    public void ReplaceToken(string str, object? token)
    {
        AddToken(str, token, true);
    }

    /// <summary>
    /// Registers a token using its own string representation as the matching string.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.addToken(Object)</c>.</remarks>
    public void AddToken(object token)
    {
        AddToken(token.ToString()!, token, false);
    }

    /// <summary>
    /// Registers several tokens at once, each keyed by its own string representation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.addTokens(Object...)</c>.</remarks>
    public void AddTokens(params object[] tokens)
    {
        foreach (var t in tokens) AddToken(t.ToString()!, t, false);
    }

    /// <summary>
    /// Registers the built-in whitespace and quotation-mark tokens recognized by every parser.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.addDefaultTokens()</c>.</remarks>
    protected void AddDefaultTokens()
    {
        AddToken(" ", WHITESPACE);
        AddToken("\t", WHITESPACE);
        AddToken("\n", WHITESPACE);
        AddToken("\r", WHITESPACE);
        AddToken("\"", QUOTATION_MARK);
        AddToken("\'", QUOTATION_MARK);
    }

    /// <summary>
    /// Builds the lexer acceleration tables: keyword tokens (those matching the identifier pattern)
    /// are routed into a keyword map, and other tokens are indexed by their initial ASCII character.
    /// Runs once; subsequent calls are no-ops.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.initLexer()</c>.</remarks>
    protected void InitLexer()
    {
        if (initialCharTokens != null) return;

        initialCharTokens = new short[128];
        var prevInitialChar = (char)0;
        for (var n = tokens.Count - 1; n > 0; n--)
        {
            var tokenString = tokenStrings[n];
            if (FullMatch(identifierPattern, tokenString))
            {
                keywordTokens[tokenString] = tokens[n];
                continue;
            }
            var initialChar = tokenString[0];
            if (initialChar == prevInitialChar) continue;
            if (initialChar < 128)
            {
                initialCharTokens[initialChar] = (short)n;
            }
            prevInitialChar = initialChar;
        }
    }

    /// <summary>
    /// Replaces the regex used to recognize identifiers and returns the previous pattern.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.setIdentifierPattern(Pattern)</c>.</remarks>
    public Regex SetIdentifierPattern(Regex pattern)
    {
        var oldPattern = identifierPattern;
        identifierPattern = pattern;
        return oldPattern;
    }

    /// <summary>
    /// Starts the parsing process for the given character sequence.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.parse(CharSequence)</c>.</remarks>
    public virtual void Parse(string s)
    {
        buf = s;
        pos = 0;
        line = 1;
        column = 1;
        tokenType = START;
        tokenValue = "";
        InitLexer();
        NextToken();
    }

    /// <summary>
    /// Checks whether more tokens are available to be consumed.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.hasMore()</c>.</remarks>
    public bool HasMore()
    {
        return !ReferenceEquals(tokenType, END);
    }

    /// <summary>
    /// Returns the number of leading characters at the current position that match the given string
    /// (a full match equals the string's length).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.matchString(String)</c>.</remarks>
    protected int MatchString(string s)
    {
        var len = s.Length;
        var charsRemaining = Length();
        if (charsRemaining < len) len = charsRemaining;
        var i = 0;
        for (; i < len; i++)
        {
            if (CharAt(i) != s[i]) break;
        }
        return i;
    }

    /// <summary>
    /// Advances the buffer position past the current token's characters, updating the line and column
    /// counters (handling newlines and tab stops).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.consumeToken()</c>.</remarks>
    void ConsumeToken()
    {
        var len = tokenValue.Length;
        for (var i = 0; i < len; i++)
        {
            var ch = buf[pos];
            if (ch == '\n')
            {
                line++;
                column = 1;
            }
            else if (ch == '\t')
            {
                column += tabSize - ((column - 1) % tabSize);
            }
            else
            {
                column++;
            }
            pos++;
        }
    }

    /// <summary>
    /// Matches the next token. Tokens of type WHITESPACE are skipped.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.nextToken()</c>.</remarks>
    protected void NextToken()
    {
        while (true)
        {
            ConsumeToken();
            if (pos >= buf.Length)
            {
                tokenType = END;
                tokenValue = "";
                return;
            }
            var ch = buf[pos];
            var continueMain = false;
            if (ch < 128)
            {
                for (var tokenIndex = initialCharTokens![ch];
                    tokenIndex > 0; tokenIndex--)
                {
                    var tokenString = tokenStrings[tokenIndex];
                    var charsMatched = MatchString(tokenString);
                    if (charsMatched == tokenString.Length)
                    {
                        tokenType = tokens[tokenIndex];
                        tokenValue = tokenString;
                        if (ReferenceEquals(tokenType, WHITESPACE))
                        {
                            continueMain = true;
                            break;
                        }
                        if (ReferenceEquals(tokenType, COMMENT))
                        {
                            var n = 0;
                            for (; n < Length(); n++)
                            {
                                if (CharAt(n) == '\n') break;
                            }
                            tokenValue = SubSequence(0, n);
                            continueMain = true;
                            break;
                        }
                        if (ReferenceEquals(tokenType, QUOTATION_MARK))
                        {
                            MatchQuoted();
                            return;
                        }
                        return;
                    }
                    if (charsMatched == 0) break;
                }
                if (continueMain) continue;
            }

            // TODO: swapped the sequence of "identifier" and "number"
            // to allow ids that start with a number, check!!!
            if (Identifier() != null) return;
            if (Number() != null) return;
            tokenType = INVALID;
            tokenValue = SubSequence(0, 1);
            return;
        }
    }

    /// <summary>
    /// Returns true if the current token equals the given token, without consuming it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.accept(Object)</c>.</remarks>
    protected bool Accept(object? tok)
    {
        return ReferenceEquals(tokenType, tok);
    }

    /// <summary>
    /// If the current token equals the given token, advances past it and returns true; otherwise
    /// returns false.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.acceptAndConsume(Object)</c>.</remarks>
    protected bool AcceptAndConsume(object? tok)
    {
        if (ReferenceEquals(tokenType, tok))
        {
            NextToken();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks whether the current token equals the specified token.
    /// If not, throws a parse exception.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.expect(Object)</c>.</remarks>
    protected void Expect(object? tok)
    {
        if (!ReferenceEquals(tokenType, tok))
        {
            Error(JavaFormat.Format("Expected %s, but got %s",
                tok, tokenType));
        }
    }

    // TODO: rename?
    /// <summary>
    /// Clears the current token state and re-reads the next token from the current position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.resetLexer()</c>.</remarks>
    protected void ResetLexer()
    {
        tokenType = null;
        tokenValue = "";
        NextToken();
    }

    /// <summary>
    /// Reports a parse error at the current file, line, and column by throwing a
    /// <see cref="ParserException"/>. Subclasses may override.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.error(String)</c>.</remarks>
    protected virtual void Error(string msg)
    {
        msg = JavaFormat.Format("%s [%d:%d]: %s",
            fileName == null ? "<none>" : fileName,
            line, column, msg);
        throw new ParserException(msg);
    }

    /// <summary>
    /// Formats the message with the given arguments and reports it via <see cref="Error(string)"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.error(String, Object...)</c>.</remarks>
    protected void Error(string msg, params object?[] args)
    {
        Error(JavaFormat.Format(msg, args));
    }

    /// <summary>
    /// Returns the raw literal text of the current token.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.stringValue()</c>.</remarks>
    protected string StringValue()
    {
        return tokenValue;
    }

    /// <summary>
    /// Returns the actual string value of a literal string token, chopping off
    /// the quotation marks and turning escape sequences into actual characters.
    /// Example: "a\\b" becomes a\b
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.unquotedStringValue()</c>.</remarks>
    protected string UnquotedStringValue()
    {
        return Strings.Unescape(tokenValue, true);
    }

    /// <summary>
    /// Parses the current token's text as an <see cref="int"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.intValue()</c>.</remarks>
    protected int IntValue()
    {
        return int.Parse(tokenValue, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses the current token's text as a <see cref="long"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.longValue()</c>.</remarks>
    protected long LongValue()
    {
        return long.Parse(tokenValue, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses the current token's text as a <see cref="double"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.doubleValue()</c>.</remarks>
    protected double DoubleValue()
    {
        return double.Parse(tokenValue, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Attempts to match an identifier at the current position. On success sets the token to the
    /// matching keyword token or to the identifier token and returns it; otherwise returns null.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.identifier()</c>.</remarks>
    public object? Identifier()
    {
        var matcher = identifierPattern.Match(buf, pos);
        if (matcher.Success && matcher.Index == pos)
        {
            tokenValue = SubSequence(0, matcher.Length);
            tokenType = keywordTokens.TryGetValue(tokenValue, out var v) ? v : IDENTIFIER;
            return tokenType;
        }
        return null;
    }

    // TODO: should accept whitespace between minus sign and first number
    /// <summary>
    /// Attempts to match a numeric literal (optionally signed, optionally fractional) at the current
    /// position. On success sets the token to the number token and returns it; otherwise returns null.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.number()</c>.</remarks>
    public object? Number()
    {
        var minus = false;
        var decimalSeen = false;
        var digits = false;
        var n = 0;
        for (; n < Length(); n++)
        {
            var ch = CharAt(n);
            if (ch == '-')
            {
                if (minus || digits || decimalSeen) break;
                minus = true;
                continue;
            }
            if (ch == '.')
            {
                if (decimalSeen) break;
                decimalSeen = true;
                continue;
            }
            if (char.IsDigit(ch))
            {
                digits = true;
                continue;
            }
            break;
        }
        if (!digits) return null;
        tokenType = NUMBER;
        tokenValue = SubSequence(0, n);
        return tokenType;
    }

    /// <summary>
    /// Matches a quoted string literal at the current position, handling escape sequences and the
    /// matching closing quote. Sets the token to the string token, or to an invalid-string token if
    /// the literal is malformed or unterminated.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.matchQuoted()</c>.</remarks>
    public void MatchQuoted()
    {
        var chQuote = CharAt(0);
        var n = 1;
        for (; n < Length(); n++)
        {
            var ch = CharAt(n);
            if (ch == '\"' || ch == '\'')
            {
                if (ch != chQuote) continue;
                chQuote = (char)0;
                n++;
                break;
            }
            if (ch == '\\') // escape
            {
                n++;
                if (n >= Length()) break;
                if (Strings.Unescape(ch) == char.MaxValue) break;
            }
            else if (Strings.Escape(ch) != char.MaxValue)
            {
                break;
            }
        }
        tokenType = (chQuote == 0) ? STRING : INVALID_STRING;
        tokenValue = SubSequence(0, n);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.length()</c>.</remarks>
    public int Length()
    {
        return buf.Length - pos;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.charAt(int)</c>.</remarks>
    public char CharAt(int index)
    {
        return buf[pos + index];
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.subSequence(int, int)</c>.</remarks>
    public string SubSequence(int start, int end)
    {
        return buf.Substring(pos + start, end - start);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.getFile()</c>.</remarks>
    public string GetFile()
    {
        return fileName!;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.getLine()</c>.</remarks>
    public int GetLine()
    {
        return line;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.parser.Parser.getColumn()</c>.</remarks>
    public int GetColumn()
    {
        return column;
    }

    /// <remarks>Port-only helper for Java's <c>Matcher.matches()</c> (full-string regex match).</remarks>
    static bool FullMatch(Regex pattern, string s)
    {
        var m = pattern.Match(s);
        return m.Success && m.Index == 0 && m.Length == s.Length;
    }

}
