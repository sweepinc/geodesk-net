/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Clarisma.Common.Ast;
using Clarisma.Common.Math;
using Clarisma.Common.Parser;
using GeoDesk.Feature.Store;

namespace GeoDesk.Feature.Match;

/// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherParser</c>.</remarks>
public class MatcherParser : Parser
{
    private const string COMMA = ",";
    private const string STAR = "*";
    private const string COLON = ":";
    private const string EXCLAMATION_MARK = "!";
    private const string LBRACKET = "[";
    private const string RBRACKET = "]";

    // Java pattern \p{L}[[\p{L}\p{N}]:_]* — the nested char class is flattened for .NET.
    private static readonly Regex KEY_IDENTIFIER_PATTERN =
        new Regex(@"\p{L}[\p{L}\p{N}:_]*");

    public static readonly Operator STARTS_WITH =
        new Operator("startsWith", null, Operator.COMPARISON_LEVEL);
    public static readonly Operator ENDS_WITH =
        new Operator("endsWith", null, Operator.COMPARISON_LEVEL);

    private const int OP_REQUIRES_KEY = 1;
    private const int OP_NUMERIC = 2;
    private const int OP_STRING = 4;
    private const int OP_LIST = 8;
    private const int OP_EQUAL = 16;
    private const int OP_EXACT = 32;

    private readonly IReadOnlyDictionary<string, int> stringsToCodes;
    private readonly IReadOnlyDictionary<int, int> keysToCategories;

    public MatcherParser(IReadOnlyDictionary<string, int>? stringsToCodes, IReadOnlyDictionary<int, int>? keysToCategories)
    {
        this.stringsToCodes = stringsToCodes ?? new Dictionary<string, int>();
        this.keysToCategories = keysToCategories ?? new Dictionary<int, int>();
        AddTokens(COMMA, STAR, COLON, EXCLAMATION_MARK, LBRACKET, RBRACKET,
            Operator.EQ, Operator.NE, Operator.GT, Operator.GE, Operator.LT,
            Operator.LE);
        AddToken("~", Operator.MATCH);
        AddToken("!~", Operator.NOT_MATCH);
    }

    /// <summary>
    /// Matches an identifier string and returns a bit field with the bits representing
    /// the types accepted by the current selector.
    /// </summary>
    /// <returns>type mask, or 0 if the type specifier is not valid</returns>
    private int FeatureTypes()
    {
        int types = 0;
        Expect(IDENTIFIER);
        string s = StringValue();
        NextToken();
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
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

    private int KeyCode(string key)
    {
        int keyCode = StringCode(key);
        return keyCode <= TagValues.MAX_COMMON_KEY ? keyCode : 0;
    }

    private int StringCode(string key)
    {
        return stringsToCodes.TryGetValue(key, out int v) ? v : 0;
    }

    private string? Key()
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

    private string? ExpectKey()
    {
        string? key = Key();
        if (key != null) return key;
        ErrorExpected("key");
        return null;
    }

    private Operator? OperatorTok()
    {
        if (tokenType is Operator op)
        {
            NextToken();
            return op;
        }
        return null;
    }

    private object? ComparisonValue(int opFlags)
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
            int typeFlags = opFlags & (OP_NUMERIC | OP_STRING);
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

    private void ErrorExpected(string what)
    {
        Error(string.Format(CultureInfo.InvariantCulture, "Expected {0} instead of {1}", what, tokenValue));
    }

    private int OperatorFlags(Operator op)
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

    private static bool IsNumericString(string s)
    {
        int len = s.Length;
        if (len == 0) return false;
        char ch = s[0];
        if ((ch < '0' || ch > '9') && ch != '-') return false;
        return MathUtils.CountNumberChars(s) == len;
    }

    private TagClause? TagClause()
    {
        if (!Accept(LBRACKET)) return null;
        // change the identifier pattern to support colons in keys
        SetIdentifierPattern(KEY_IDENTIFIER_PATTERN);
        NextToken();

        string? key;
        int flags = 0;
        Expression? exp = null;

        if (AcceptAndConsume(EXCLAMATION_MARK))
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
            Operator? op = OperatorTok();
            if (op == null)
            {
                flags = Match.TagClause.KEY_REQUIRED_EXPLICITLY |
                    Match.TagClause.VALUE_GLOBAL_STRING;
                // [k] requires global key because we need to check for "no"
            }
            else
            {
                int opFlags = OperatorFlags(op);
                if ((opFlags & OP_REQUIRES_KEY) != 0)
                {
                    flags |= Match.TagClause.KEY_REQUIRED_IMPLICITLY;
                }
                for (; ; )
                {
                    Expression term;
                    bool negate = false;
                    object? val = ComparisonValue(opFlags);
                    if (val == null) return null;

                    Operator effectiveOp = op;
                    if (val is double)
                    {
                        flags |= Match.TagClause.VALUE_DOUBLE;
                    }
                    else
                    {
                        string s = (string)val;
                        if ((opFlags & OP_EXACT) != 0)
                        {
                            int len = s.Length;
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
                            int code = StringCode(s);
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
                    if (!AcceptAndConsume(COMMA)) break;
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
        Expect(RBRACKET);
        SetIdentifierPattern(DEFAULT_IDENTIFIER_PATTERN);
        NextToken();
        int keyCode = KeyCode(key);
        int category = keysToCategories.TryGetValue(keyCode, out int cat) ? cat : 0;
        return new TagClause(flags, key, keyCode, category, exp);
    }

    private Selector? SelectorTok()
    {
        int types;
        if (Accept(LBRACKET) || AcceptAndConsume(STAR))
        {
            // Type indicator can be omitted (implies '*')
            types = TypeBits.ALL;
        }
        else
        {
            types = FeatureTypes();
            if (types == 0) return null;
        }
        Selector sel = new Selector(types);
        for (; ; )
        {
            TagClause? clause = TagClause();
            if (clause == null) break;
            sel.Add(clause);
        }
        return sel;
    }

    public Selector? Query()
    {
        Selector? first = null;
        Selector? prev = null;
        for (; ; )
        {
            Selector? sel = SelectorTok();
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
            if (!AcceptAndConsume(COMMA)) break;
        }
        Expect(END);
        return first;
    }

    public override void Parse(string s)
    {
        SetIdentifierPattern(DEFAULT_IDENTIFIER_PATTERN);
        base.Parse(s);
    }

    protected override void Error(string msg)
    {
        msg = string.Format(CultureInfo.InvariantCulture, "[{0}:{1}]: {2}", line, column, msg);
        throw new QueryException(msg);
    }
}
