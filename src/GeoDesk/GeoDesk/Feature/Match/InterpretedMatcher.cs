/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using Clarisma.Common.Ast;
using Clarisma.Common.Math;
using Clarisma.Common.Util;
using GeoDesk.Feature.Store;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Match;

/// <summary>
/// An AST-interpreting <see cref="Matcher"/>: instead of compiling a query's
/// <see cref="Selector"/>/<see cref="TagClause"/>/Expression to bytecode (as the Java original
/// does), it walks the parsed query at runtime against a feature's tag table.
///
/// This is the functional, reference implementation (see PORT.md). A runtime compiler
/// (System.Linq.Expressions or Reflection.Emit — undecided) can later be validated against it.
/// </summary>
public class InterpretedMatcher : TagMatcher
{
    private readonly Selector first;
    private readonly int valueNo;

    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new ConcurrentDictionary<string, Regex>();

    public InterpretedMatcher(Selector first, string[] globalStrings, int valueNo)
        : base(AcceptedTypesOf(first), globalStrings, KeyMaskOf(first), KeyMinOf(first))
    {
        this.first = first;
        this.valueNo = valueNo;
    }

    private static int AcceptedTypesOf(Selector first)
    {
        int t = 0;
        for (Selector? s = first; s != null; s = s.Next()) t |= s.MatchTypes();
        return t;
    }

    private static int KeyMaskOf(Selector first)
    {
        int m = 0;
        for (Selector? s = first; s != null; s = s.Next()) m |= s.IndexBitsValue();
        return m;
    }

    private static int KeyMinOf(Selector first)
    {
        int min = int.MaxValue;
        for (Selector? s = first; s != null; s = s.Next())
        {
            if (s.IndexBitsValue() < min) min = s.IndexBitsValue();
        }
        return min;
    }

    public override bool Accept(NioBuffer buf, int pos)
    {
        List<TagEntry> tags = ReadTags(buf, pos);
        for (Selector? sel = first; sel != null; sel = sel.Next())
        {
            if (MatchSelector(sel, tags)) return true;
        }
        return false;
    }

    private bool MatchSelector(Selector sel, List<TagEntry> tags)
    {
        for (TagClause? clause = sel.FirstClause(); clause != null; clause = clause.Next())
        {
            if (!MatchClause(clause, tags)) return false;
        }
        return true;
    }

    private bool MatchClause(TagClause clause, List<TagEntry> tags)
    {
        TagEntry? t = FindTag(clause, tags);
        bool present = t != null;
        bool isNo = present && t!.Kind == TagValues.GLOBAL_STRING && t.ValueCode == valueNo;

        Expression? exp = clause.Expression();
        if (exp == null)
        {
            if (clause.IsKeyRequired())
            {
                // [k]: key present and not "no"
                return present && !isNo;
            }
            // [!k]: key absent or "no"
            return !present || isNo;
        }

        if (clause.IsKeyRequired())
        {
            // [k=v], [k>v], lists, etc.
            // If the key is required *explicitly only* (e.g. [k][k!=v]), the value must
            // also not be "no", in addition to the expression holding.
            bool requiredExplicitlyOnly =
                (clause.Flags() & (TagClause.KEY_REQUIRED_EXPLICITLY | TagClause.KEY_REQUIRED_IMPLICITLY))
                == TagClause.KEY_REQUIRED_EXPLICITLY;
            if (requiredExplicitlyOnly) return present && !isNo && EvalExpr(exp, t!);
            return present && EvalExpr(exp, t!);
        }
        // [k!=v]: matches if key absent, or expression holds
        if (!present) return true;
        return EvalExpr(exp, t!);
    }

    private TagEntry? FindTag(TagClause clause, List<TagEntry> tags)
    {
        if (clause.IsGlobalKey())
        {
            int code = clause.KeyCode();
            foreach (TagEntry e in tags)
            {
                if (e.KeyCode == code) return e;
            }
        }
        else
        {
            string name = clause.Name;
            foreach (TagEntry e in tags)
            {
                if (e.KeyCode == 0 && string.Equals(e.KeyString, name, StringComparison.Ordinal)) return e;
            }
        }
        return null;
    }

    private bool EvalExpr(Expression exp, TagEntry t)
    {
        if (exp is UnaryExpression u)
        {
            // only NOT
            return !EvalExpr(u.Operand, t);
        }
        BinaryExpression b = (BinaryExpression)exp;
        Operator op = b.Operator;
        if (op == Operator.AND) return EvalExpr(b.Left, t) && EvalExpr(b.Right, t);
        if (op == Operator.OR) return EvalExpr(b.Left, t) || EvalExpr(b.Right, t);

        object? lit = ((Literal)b.Right).Value;

        if (op == Operator.LT || op == Operator.LE || op == Operator.GT || op == Operator.GE)
        {
            double d = Convert.ToDouble(lit, CultureInfo.InvariantCulture);
            double tv = t.Double();
            if (op == Operator.LT) return tv < d;
            if (op == Operator.LE) return tv <= d;
            if (op == Operator.GT) return tv > d;
            return tv >= d;
        }
        if (op == Operator.EQ || op == Operator.NE)
        {
            bool eq;
            if (lit is double dd)
            {
                eq = t.Double() == dd;
            }
            else
            {
                string s = lit is GlobalString gs ? gs.StringValue : (string)lit!;
                eq = string.Equals(t.String(), s, StringComparison.Ordinal);
            }
            return op == Operator.EQ ? eq : !eq;
        }
        if (op == Operator.MATCH) return RegexMatches(t.String(), (string)lit!);
        if (op == Operator.NOT_MATCH) return !RegexMatches(t.String(), (string)lit!);
        if (op == Operator.IN) return t.String().Contains((string)lit!, StringComparison.Ordinal);
        if (op == MatcherParser.STARTS_WITH) return t.String().StartsWith((string)lit!, StringComparison.Ordinal);
        if (op == MatcherParser.ENDS_WITH) return t.String().EndsWith((string)lit!, StringComparison.Ordinal);
        throw new QueryException("Unsupported operator in interpreted matcher: " + op);
    }

    private static bool RegexMatches(string input, string pattern)
    {
        Regex rx = RegexCache.GetOrAdd(pattern, p => new Regex(p));
        // Java's Pattern.matches() requires a full-string match.
        var m = rx.Match(input);
        return m.Success && m.Index == 0 && m.Length == input.Length;
    }

    // === Tag-table reading (format from STagTable / MatcherCoder) ===

    private List<TagEntry> ReadTags(NioBuffer buf, int pos)
    {
        var entries = new List<TagEntry>();
        int pPtr = pos + 8;
        int taggedPtr = buf.GetInt(pPtr);
        bool hasLocalKeys = (taggedPtr & 1) != 0;
        int tagTablePtr = pPtr + (taggedPtr & ~1);

        // Global (common) keys: forward from the anchor
        int p = tagTablePtr;
        for (; ; )
        {
            int key16 = buf.GetChar(p);
            int keyCode = (key16 >> 2) & 0x1fff;
            if (keyCode == 0) break; // empty-table marker / no global keys
            int kind = key16 & 3;
            bool wide = (kind & 2) != 0;
            var e = new TagEntry(buf, globalStrings) { Kind = kind, KeyCode = keyCode, KeyString = globalStrings[keyCode] };
            int next;
            if (wide)
            {
                int w = buf.GetInt(p + 2);
                if (kind == TagValues.LOCAL_STRING) e.ValueStringLoc = (p + 2) + w;
                else e.ValueCode = w;
                next = p + 6;
            }
            else
            {
                e.ValueCode = buf.GetChar(p + 2);
                next = p + 4;
            }
            entries.Add(e);
            if ((key16 & 0x8000) != 0) break; // last tag
            p = next;
        }

        // Local (uncommon) keys: backward from the anchor
        if (hasLocalKeys)
        {
            int origin = tagTablePtr & ~3;
            p = tagTablePtr - 4;
            for (; ; )
            {
                int keyPtr = buf.GetInt(p);
                int kind = keyPtr & 3;
                bool wide = (kind & 2) != 0;
                int valueSize = wide ? 4 : 2;
                int valuePos = p - valueSize;
                var e = new TagEntry(buf, globalStrings) { Kind = kind, KeyCode = 0 };
                if (wide)
                {
                    int w = buf.GetInt(valuePos);
                    if (kind == TagValues.LOCAL_STRING) e.ValueStringLoc = valuePos + w;
                    else e.ValueCode = w;
                }
                else
                {
                    e.ValueCode = buf.GetChar(valuePos);
                }
                int relPtr = (keyPtr & ~7) >> 1;
                e.KeyString = Bytes.ReadString(buf, origin + relPtr);
                entries.Add(e);
                if ((keyPtr & 4) != 0) break; // first uncommon key
                p = valuePos - 4;
            }
        }

        return entries;
    }

    private sealed class TagEntry
    {
        private readonly NioBuffer buf;
        private readonly string[] globalStrings;

        public int KeyCode;
        public string KeyString = "";
        public int Kind;          // TagValues.NARROW_NUMBER/GLOBAL_STRING/WIDE_NUMBER/LOCAL_STRING
        public int ValueCode;     // narrow number / global string code / wide number code
        public int ValueStringLoc; // for LOCAL_STRING

        public TagEntry(NioBuffer buf, string[] globalStrings)
        {
            this.buf = buf;
            this.globalStrings = globalStrings;
        }

        public string String()
        {
            switch (Kind)
            {
                case TagValues.NARROW_NUMBER:
                    return (ValueCode + TagValues.MIN_NUMBER).ToString(CultureInfo.InvariantCulture);
                case TagValues.GLOBAL_STRING:
                    return globalStrings[ValueCode];
                case TagValues.WIDE_NUMBER:
                    return TagValues.WideNumberToString(ValueCode)!;
                default: // LOCAL_STRING
                    return Bytes.ReadString(buf, ValueStringLoc);
            }
        }

        public double Double()
        {
            switch (Kind)
            {
                case TagValues.NARROW_NUMBER:
                    return ValueCode + TagValues.MIN_NUMBER;
                case TagValues.GLOBAL_STRING:
                    return MathUtils.DoubleFromString(globalStrings[ValueCode]);
                case TagValues.WIDE_NUMBER:
                    return TagValues.WideNumberToDouble(ValueCode);
                default: // LOCAL_STRING
                    return MathUtils.DoubleFromString(String());
            }
        }
    }
}
