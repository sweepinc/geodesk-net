/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GeoDesk.Common.Parser;

/// <summary>
/// A parser that reads comma-separated <c>key=value</c> tags. <c>value</c>
/// can be a quoted string, an identifier (treated as a string),
/// a number, <c>true</c> or <c>false</c>.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.parser.TagsParser</c>.</remarks>
internal class TagsParser : Parser
{

    public const string COMMA = ",";
    public const string EQUALS = "=";
    public const string TRUE = "true";
    public const string FALSE = "false";

    protected static readonly object[] TOKENS =
    {
        COMMA, EQUALS, TRUE, FALSE
    };

    // TODO: fix!!!
    public static readonly Regex KEY_PATTERN =
        new Regex(@"[a-zA-Z0-9_][a-zA-Z0-9_\-:\.]*");

    /// <summary>
    /// Creates a tags parser, registering the comma, equals, and boolean keyword tokens and the
    /// identifier (key) pattern.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.TagsParser()</c>.</remarks>
    public TagsParser()
    {
        foreach (var tok in TOKENS)
        {
            AddToken(tok.ToString()!, tok);
        }
        SetIdentifierPattern(KEY_PATTERN);
    }

    /// <summary>
    /// Parses a tag key (a quoted string or an identifier) at the current token and advances. Reports
    /// an error and returns null if neither is present.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.TagsParser.key()</c>.</remarks>
    public string? Key()
    {
        if (ReferenceEquals(tokenType, STRING))
        {
            var key = UnquotedStringValue();
            NextToken();
            return key;
        }
        if (ReferenceEquals(tokenType, IDENTIFIER))
        {
            var key = StringValue();
            NextToken();
            return key;
        }
        Error("Expected <key>, but got %s", tokenType);
        return null;
    }

    /// <summary>
    /// Parses a tag value at the current token and advances: a string, identifier (as string), number
    /// (long or double depending on a decimal point), or the boolean keywords. Reports an error and
    /// returns null otherwise.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.TagsParser.value()</c>.</remarks>
    public object? Value()
    {
        if (ReferenceEquals(tokenType, STRING))
        {
            object value = UnquotedStringValue();
            NextToken();
            return value;
        }
        if (ReferenceEquals(tokenType, IDENTIFIER))
        {
            object value = StringValue();
            NextToken();
            return value;
        }
        if (ReferenceEquals(tokenType, NUMBER))
        {
            object value;
            if (StringValue().IndexOf('.') >= 0)
            {
                value = DoubleValue();
            }
            else
            {
                value = LongValue();
            }
            NextToken();
            return value;
        }
        if (ReferenceEquals(tokenType, TRUE))
        {
            NextToken();
            return true;
        }
        if (ReferenceEquals(tokenType, FALSE))
        {
            NextToken();
            return false;
        }
        Error("Expected <value>, but got %s", tokenType);
        return null;
    }

    /// <summary>
    /// Parses the full comma-separated sequence of <c>key=value</c> tags into a dictionary mapping
    /// each key to its parsed value.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.TagsParser.tags()</c>.</remarks>
    public Dictionary<string, object?> Tags()
    {
        var map = new Dictionary<string, object?>();

        for (; ; )
        {
            var key = Key();
            Expect(EQUALS);
            NextToken();
            map[key!] = Value();
            if (!HasMore()) break;
            AcceptAndConsume(COMMA);
                // TODO: should accept only comma or newline
        }
        return map;
    }

    /// <summary>
    /// Parses the comma-separated <c>key=value</c> tags into a flat list alternating keys and their
    /// string-rendered values.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.TagsParser.tagsAsList()</c>.</remarks>
    public List<string> TagsAsList()
    {
        var list = new List<string>();

        for (; ; )
        {
            list.Add(Key()!);
            Expect(EQUALS);
            NextToken();
            list.Add(Value()!.ToString()!);
            if (!HasMore()) break;
            Expect(COMMA);
            NextToken();
        }
        return list;
    }

    /// <summary>
    /// Parses the tags into a flat array alternating keys and string-rendered values.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.TagsParser.tagsAsArray()</c>.</remarks>
    public string[] TagsAsArray()
    {
        return TagsAsList().ToArray();
    }

}
