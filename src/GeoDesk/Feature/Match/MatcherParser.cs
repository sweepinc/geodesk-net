/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using GeoDesk.Common.Ast;
using GeoDesk.Common.Math;
using GeoDesk.Common.Parser;
using GeoDesk.Feature.Store;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser</c>.</remarks>
internal class MatcherParser : Parser
{

    const string Comma = ",";
    const string Star = "*";
    const string Colon = ":";
    const string ExclamationMark = "!";
    const string LBracket = "[";
    const string RBracket = "]";

    // Java pattern \p{L}[[\p{L}\p{N}]:_]* — the nested char class is flattened for .NET.
    static readonly Regex KeyIdentifierPattern =
        new Regex(@"\p{L}[\p{L}\p{N}:_]*");

    public static readonly Operator STARTS_WITH =
        new Operator("startsWith", null, Operator.COMPARISON_LEVEL);
    public static readonly Operator ENDS_WITH =
        new Operator("endsWith", null, Operator.COMPARISON_LEVEL);

    const int OP_REQUIRES_KEY = 1;
    const int OP_NUMERIC = 2;
    const int OP_STRING = 4;
    const int OP_LIST = 8;
    const int OP_EQUAL = 16;
    const int OP_EXACT = 32;

    readonly IReadOnlyDictionary<string, int> _stringsToCodes;
    readonly IReadOnlyDictionary<int, int> _keysToCategories;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser(ObjectIntMap, IntIntMap)</c>.</remarks>
    public MatcherParser(IReadOnlyDictionary<string, int>? stringsToCodes, IReadOnlyDictionary<int, int>? keysToCategories)
    {
        _stringsToCodes = stringsToCodes ?? new Dictionary<string, int>();
        _keysToCategories = keysToCategories ?? new Dictionary<int, int>();
        AddTokens(Comma, Star, Colon, ExclamationMark, LBracket, RBracket,
            Operator.EQ, Operator.NE, Operator.GT, Operator.GE, Operator.LT,
            Operator.LE);
        AddToken("~", Operator.MATCH);
        AddToken("!~", Operator.NOT_MATCH);
    }

    /// <summary>
    /// Matches an identifier string and returns a bit field with the bits representing the types
    /// accepted by the current selector.
    /// </summary>
    /// <returns>type mask, or 0 if the type specifier is not valid</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.featureTypes()</c>.</remarks>
    int FeatureTypes()
    {
        var types = 0;
        Expect(IDENTIFIER);
        var s = StringValue();
        NextToken();
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            switch (ch)
            {
                case 'n':
                    if ((types & TypeBits.NODES) == TypeBits.NODES) return 0;
                    types |= TypeBits.NODES;
                    break;
                case 'w':
                    if ((types & TypeBits.NONAREA_WAYS) == TypeBits.NONAREA_WAYS) return 0;
                    types |= TypeBits.NONAREA_WAYS;
                    break;
                case 'a':
                    if ((types & TypeBits.AREAS) == TypeBits.AREAS) return 0;
                    types |= TypeBits.AREAS;
                    break;
                case 'r':
                    if ((types & TypeBits.NONAREA_RELATIONS) == TypeBits.NONAREA_RELATIONS) return 0;
                    types |= TypeBits.NONAREA_RELATIONS;
                    break;
                default:
                    Error(string.Format(CultureInfo.InvariantCulture,
                        "Unknown feature type '{0}', should be 'n','w','a', or 'r'", ch));
                    return 0;
            }
        }
        return types;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.keyCode(String)</c>.</remarks>
    int KeyCode(string key)
    {
        var keyCode = StringCode(key);
        return keyCode <= TagValues.MAX_COMMON_KEY ? keyCode : 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.stringCode(String)</c>.</remarks>
    int StringCode(string key)
    {
        return _stringsToCodes.TryGetValue(key, out var v) ? v : 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.key()</c>.</remarks>
    string? Key()
    {
        string key;
        if (Accept(IDENTIFIER))
        {
            key = StringValue();
        }
        else
        {
            if (!Accept(STRING)) return null;
            key = UnquotedStringValue();
        }
        NextToken();
        return key;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.expectKey()</c>.</remarks>
    string? ExpectKey()
    {
        var key = Key();
        if (key != null) return key;
        ErrorExpected("key");
        return null;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.operator()</c> (renamed: <c>operator</c> is a C# keyword).</remarks>
    Operator? OperatorTok()
    {
        if (tokenType is Operator op)
        {
            NextToken();
            return op;
        }
        return null;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.comparisonValue(int)</c>.</remarks>
    object? ComparisonValue(int opFlags)
    {
        object? val;
        int type;
        if (Accept(NUMBER))
        {
            val = DoubleValue();
            type = OP_NUMERIC;
        }
        else if (Accept(IDENTIFIER))
        {
            val = StringValue();
            type = OP_STRING;
        }
        else if (Accept(STRING))
        {
            val = UnquotedStringValue();
            type = OP_STRING;
        }
        else
        {
            type = 0;
            val = null;
        }
        if ((opFlags & type) == 0)
        {
            var typeFlags = opFlags & (OP_NUMERIC | OP_STRING);
            switch (typeFlags)
            {
                case OP_NUMERIC:
                    ErrorExpected("number");
                    return null;
                case OP_STRING:
                    ErrorExpected("string");
                    return null;
                default:
                    ErrorExpected("string or number");
                    return null;
            }
        }
        NextToken();
        return val;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.errorExpected(String)</c>.</remarks>
    void ErrorExpected(string what)
    {
        Error(string.Format(CultureInfo.InvariantCulture, "Expected {0} instead of {1}", what, tokenValue));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.operatorFlags(Operator)</c>.</remarks>
    int OperatorFlags(Operator op)
    {
        if (op == Operator.EQ)
        {
            return OP_REQUIRES_KEY | OP_NUMERIC | OP_STRING | OP_LIST | OP_EQUAL | OP_EXACT;
        }
        if (op == Operator.NE)
        {
            return OP_NUMERIC | OP_STRING | OP_LIST | OP_EXACT;
        }
        if (op == Operator.MATCH)
        {
            return OP_REQUIRES_KEY | OP_STRING | OP_LIST | OP_EQUAL;
        }
        if (op == Operator.NOT_MATCH)
        {
            return OP_STRING | OP_LIST;
        }
        return OP_REQUIRES_KEY | OP_NUMERIC;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.isNumericString(String)</c>.</remarks>
    static bool IsNumericString(string s)
    {
        var len = s.Length;
        if (len == 0) return false;
        var ch = s[0];
        if ((ch < '0' || ch > '9') && ch != '-') return false;
        return MathUtils.CountNumberChars(s) == len;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.tagClause()</c>.</remarks>
    TagClause? TagClause()
    {
        if (!Accept(LBracket)) return null;
        // change the identifier pattern to support colons in keys
        SetIdentifierPattern(KeyIdentifierPattern);
        NextToken();

        string? key;
        var flags = 0;
        Expression? exp = null;

        if (AcceptAndConsume(ExclamationMark))
        {
            key = ExpectKey();
            if (key == null) return null;
            flags = Match.TagClause.VALUE_GLOBAL_STRING;
            // [!k] requires global key because we need to check for "no"
        }
        else
        {
            key = ExpectKey();
            if (key == null) return null;
            var op = OperatorTok();
            if (op == null)
            {
                flags = Match.TagClause.KEY_REQUIRED_EXPLICITLY |
                    Match.TagClause.VALUE_GLOBAL_STRING;
                // [k] requires global key because we need to check for "no"
            }
            else
            {
                var opFlags = OperatorFlags(op);
                if ((opFlags & OP_REQUIRES_KEY) != 0)
                {
                    flags |= Match.TagClause.KEY_REQUIRED_IMPLICITLY;
                }
                for (; ; )
                {
                    Expression term;
                    var negate = false;
                    var val = ComparisonValue(opFlags);
                    if (val == null) return null;

                    var effectiveOp = op;
                    if (val is double)
                    {
                        flags |= Match.TagClause.VALUE_DOUBLE;
                    }
                    else
                    {
                        var s = (string)val;
                        if ((opFlags & OP_EXACT) != 0)
                        {
                            var len = s.Length;
                            if (len > 0)
                            {
                                if (s[0] == '*')
                                {
                                    negate = (opFlags & OP_EQUAL) == 0;
                                    if (s[len - 1] == '*')
                                    {
                                        effectiveOp = Operator.IN;
                                        val = len == 1 ? "" : s.Substring(1, len - 2);
                                    }
                                    else
                                    {
                                        effectiveOp = ENDS_WITH;
                                        val = s.Substring(1);
                                    }
                                    flags |= Match.TagClause.VALUE_LOCAL_STRING |
                                        Match.TagClause.VALUE_ANY_STRING;
                                }
                                else if (s[len - 1] == '*')
                                {
                                    negate = (opFlags & OP_EQUAL) == 0;
                                    effectiveOp = STARTS_WITH;
                                    val = s.Substring(0, len - 1);
                                    flags |= Match.TagClause.VALUE_LOCAL_STRING |
                                        Match.TagClause.VALUE_ANY_STRING;
                                }
                            }
                        }
                        if (effectiveOp == Operator.EQ || effectiveOp == Operator.NE)
                        {
                            var code = StringCode(s);
                            if (code == 0)
                            {
                                val = s;
                                flags |= Match.TagClause.VALUE_LOCAL_STRING;
                                if (IsNumericString(s))
                                {
                                    flags |= Match.TagClause.VALUE_ANY_STRING;
                                }
                            }
                            else
                            {
                                val = new GlobalString(s, code);
                                flags |= Match.TagClause.VALUE_GLOBAL_STRING;
                            }
                        }
                    }
                    term = new BinaryExpression(effectiveOp, new Variable(key), new Literal(val));
                    if (negate) term = new UnaryExpression(Operator.NOT, term);
                    exp = exp == null ? term : new BinaryExpression(
                        (opFlags & OP_EQUAL) == 0 ? Operator.AND : Operator.OR, exp, term);
                    if (!AcceptAndConsume(Comma)) break;
                    if ((opFlags & OP_LIST) == 0)
                    {
                        Error(string.Format(CultureInfo.InvariantCulture,
                            "Multiple values are not allowed for {0}", op));
                        return null;
                    }
                }
                if (op == Operator.MATCH || op == Operator.NOT_MATCH)
                {
                    flags |= Match.TagClause.VALUE_ANY_STRING | Match.TagClause.VALUE_LOCAL_STRING;
                }
            }
        }
        Expect(RBracket);
        SetIdentifierPattern(DEFAULT_IDENTIFIER_PATTERN);
        NextToken();
        var keyCode = KeyCode(key);
        var category = _keysToCategories.TryGetValue(keyCode, out var cat) ? cat : 0;
        return new TagClause(flags, key, keyCode, category, exp);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.selector()</c> (renamed: avoids clash with the <c>Selector</c> type).</remarks>
    Selector? SelectorTok()
    {
        int types;
        if (Accept(LBracket) || AcceptAndConsume(Star))
        {
            // Type indicator can be omitted (implies '*')
            types = TypeBits.ALL;
        }
        else
        {
            types = FeatureTypes();
            if (types == 0) return null;
        }
        var sel = new Selector(types);
        for (; ; )
        {
            var clause = TagClause();
            if (clause == null) break;
            sel.Add(clause);
        }
        return sel;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.query()</c>.</remarks>
    public Selector? Query()
    {
        Selector? first = null;
        Selector? prev = null;
        for (; ; )
        {
            var sel = SelectorTok();
            if (sel == null) break;
            if (prev == null)
            {
                first = sel;
            }
            else
            {
                prev.SetNext(sel);
            }
            prev = sel;
            if (!AcceptAndConsume(Comma)) break;
        }
        Expect(END);
        return first;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.parse(String)</c>.</remarks>
    public override void Parse(string s)
    {
        SetIdentifierPattern(DEFAULT_IDENTIFIER_PATTERN);
        base.Parse(s);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser.error(String)</c>.</remarks>
    protected override void Error(string msg)
    {
        msg = string.Format(CultureInfo.InvariantCulture, "[{0}:{1}]: {2}", line, column, msg);
        throw new QueryException(msg);
    }

}
