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
using Clarisma.Common.Text;
using Clarisma.Common.Util;

namespace Clarisma.Common.Parser;

/// <summary>
/// An extendable general-purpose lexer/parser, especially suited
/// for generating abstract syntax trees.
/// </summary>
/// <remarks>
/// Ported from Java <c>com.clarisma.common.parser.Parser</c>. In Java this implements CharSequence and
/// FileLocation; the straight port keeps FileLocation (IFileLocation) and provides
/// Length/CharAt/SubSequence directly rather than implementing a CharSequence interface (which .NET lacks).
/// </remarks>
public class Parser : IFileLocation
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
    public void AddToken(string str, object? token, bool replace)
    {
        int n = tokenStrings.BinarySearch(str, StringComparer.Ordinal);
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

    public void AddToken(string str, object? token)
    {
        AddToken(str, token, false);
    }

    public void ReplaceToken(string str, object? token)
    {
        AddToken(str, token, true);
    }

    public void AddToken(object token)
    {
        AddToken(token.ToString()!, token, false);
    }

    public void AddTokens(params object[] tokens)
    {
        foreach (object t in tokens) AddToken(t.ToString()!, t, false);
    }

    protected void AddDefaultTokens()
    {
        AddToken(" ", WHITESPACE);
        AddToken("\t", WHITESPACE);
        AddToken("\n", WHITESPACE);
        AddToken("\r", WHITESPACE);
        AddToken("\"", QUOTATION_MARK);
        AddToken("\'", QUOTATION_MARK);
    }

    protected void InitLexer()
    {
        if (initialCharTokens != null) return;

        initialCharTokens = new short[128];
        char prevInitialChar = (char)0;
        for (int n = tokens.Count - 1; n > 0; n--)
        {
            string tokenString = tokenStrings[n];
            if (FullMatch(identifierPattern, tokenString))
            {
                keywordTokens[tokenString] = tokens[n];
                continue;
            }
            char initialChar = tokenString[0];
            if (initialChar == prevInitialChar) continue;
            if (initialChar < 128)
            {
                initialCharTokens[initialChar] = (short)n;
            }
            prevInitialChar = initialChar;
        }
    }

    public Regex SetIdentifierPattern(Regex pattern)
    {
        Regex oldPattern = identifierPattern;
        identifierPattern = pattern;
        return oldPattern;
    }

    /// <summary>
    /// Starts the parsing process for the given character sequence.
    /// </summary>
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
    public bool HasMore()
    {
        return !ReferenceEquals(tokenType, END);
    }

    protected int MatchString(string s)
    {
        int len = s.Length;
        int charsRemaining = Length();
        if (charsRemaining < len) len = charsRemaining;
        int i = 0;
        for (; i < len; i++)
        {
            if (CharAt(i) != s[i]) break;
        }
        return i;
    }

    private void ConsumeToken()
    {
        int len = tokenValue.Length;
        for (int i = 0; i < len; i++)
        {
            char ch = buf[pos];
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
            char ch = buf[pos];
            bool continueMain = false;
            if (ch < 128)
            {
                for (int tokenIndex = initialCharTokens![ch];
                    tokenIndex > 0; tokenIndex--)
                {
                    string tokenString = tokenStrings[tokenIndex];
                    int charsMatched = MatchString(tokenString);
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
                            int n = 0;
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

    protected bool Accept(object? tok)
    {
        return ReferenceEquals(tokenType, tok);
    }

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
    protected void Expect(object? tok)
    {
        if (!ReferenceEquals(tokenType, tok))
        {
            Error(JavaFormat.Format("Expected %s, but got %s",
                tok, tokenType));
        }
    }

    // TODO: rename?
    protected void ResetLexer()
    {
        tokenType = null;
        tokenValue = "";
        NextToken();
    }

    protected virtual void Error(string msg)
    {
        msg = JavaFormat.Format("%s [%d:%d]: %s",
            fileName == null ? "<none>" : fileName,
            line, column, msg);
        throw new ParserException(msg);
    }

    protected void Error(string msg, params object?[] args)
    {
        Error(JavaFormat.Format(msg, args));
    }

    protected string StringValue()
    {
        return tokenValue;
    }

    /// <summary>
    /// Returns the actual string value of a literal string token, chopping off
    /// the quotation marks and turning escape sequences into actual characters.
    /// </summary>
    protected string UnquotedStringValue()
    {
        return Strings.Unescape(tokenValue, true);
    }

    protected int IntValue()
    {
        return int.Parse(tokenValue, CultureInfo.InvariantCulture);
    }

    protected long LongValue()
    {
        return long.Parse(tokenValue, CultureInfo.InvariantCulture);
    }

    protected double DoubleValue()
    {
        return double.Parse(tokenValue, CultureInfo.InvariantCulture);
    }

    public object? Identifier()
    {
        Match matcher = identifierPattern.Match(buf, pos);
        if (matcher.Success && matcher.Index == pos)
        {
            tokenValue = SubSequence(0, matcher.Length);
            tokenType = keywordTokens.TryGetValue(tokenValue, out object? v) ? v : IDENTIFIER;
            return tokenType;
        }
        return null;
    }

    // TODO: should accept whitespace between minus sign and first number
    public object? Number()
    {
        bool minus = false;
        bool decimalSeen = false;
        bool digits = false;
        int n = 0;
        for (; n < Length(); n++)
        {
            char ch = CharAt(n);
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

    public void MatchQuoted()
    {
        char chQuote = CharAt(0);
        int n = 1;
        for (; n < Length(); n++)
        {
            char ch = CharAt(n);
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

    public int Length()
    {
        return buf.Length - pos;
    }

    public char CharAt(int index)
    {
        return buf[pos + index];
    }

    public string SubSequence(int start, int end)
    {
        return buf.Substring(pos + start, end - start);
    }

    public string GetFile()
    {
        return fileName!;
    }

    public int GetLine()
    {
        return line;
    }

    public int GetColumn()
    {
        return column;
    }

    private static bool FullMatch(Regex pattern, string s)
    {
        Match m = pattern.Match(s);
        return m.Success && m.Index == 0 && m.Length == s.Length;
    }
}
