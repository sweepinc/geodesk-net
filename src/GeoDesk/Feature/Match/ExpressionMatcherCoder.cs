/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using GeoDesk.Common.Ast;
using GeoDesk.Common.Store;
using GeoDesk.Feature.Store;

using AstBinary = GeoDesk.Common.Ast.BinaryExpression;
using AstConditional = GeoDesk.Common.Ast.ConditionalExpression;
using AstExpression = GeoDesk.Common.Ast.Expression;
using AstUnary = GeoDesk.Common.Ast.UnaryExpression;
using Expression = System.Linq.Expressions.Expression;

namespace GeoDesk.Feature.Match;

/// <summary>
/// Compiles a query into an <see cref="ExpressionTagMatcher"/> by building a LINQ expression tree and calling
/// <c>.Compile()</c> — the .NET counterpart of the Java <c>com.geodesk.feature.match.MatcherCoder</c>
/// (which generates JVM bytecode via ASM). All branching — selectors OR'd, clauses AND'd, the value
/// expression's AND/OR/NOT — lives in the expression tree; the concrete operations (the tag-table scan,
/// value decoding, string/regex comparison) live in <see cref="MatcherOps"/>. The value
/// expression is built through the AST's own <see cref="IAstVisitor{R}"/> dispatch (<c>Accept</c>). The
/// query's literals (and their UTF-8) and regexes bake into the tree as constants; the global string table
/// is read from the matcher instance at runtime via the <c>self</c> parameter.
/// </summary>
/// <remarks>
/// <para>
/// The per-clause logic mirrors <see cref="AstTagMatcher"/> exactly (it is the oracle the differential
/// test validates against); the difference is that here it is built once into a compiled delegate, with no
/// per-feature AST walk. This is a <em>complete</em> implementation of the matcher — it handles the same
/// grammar as <see cref="AstTagMatcher"/>, and is not a subset that degrades to it. <see cref="TryCompile"/>
/// returns <c>null</c> only when the runtime has no dynamic-code support (e.g. Native AOT), where
/// <c>Expression.Compile()</c> is unavailable and the caller uses the equally complete <see cref="AstTagMatcher"/>
/// instead. A node or operator the emitter cannot build is a bug, not a fallback case: it throws.
/// </para>
/// <para>Ported from Java <c>com.geodesk.feature.match.MatcherCoder</c>.</para>
/// <para>
/// The notes below are ported from <c>MatcherCoder</c>. They describe the tag-table layout this matcher
/// reads and the order in which a query's clauses are arranged; in particular, global-key tags are stored
/// in ascending key order, which is what lets <see cref="MatcherOps.FindTagGlobal"/> stop scanning once it
/// passes the key it is looking for.
/// </para>
/// <para><b>1. Layout of Tag Tables</b></para>
/// <para>
/// Tag Tables must follow the layout described in the Tile File Format Specification. To recap, tags are
/// stored as key/value combos. Tags with a frequently used key (found in the Global String Table) are
/// Global-Key Tags, which are stored in ascending order of their numeric key value, starting at the anchor
/// address of the Tag Table. Tags with uncommon key values use a pointer to locally-stored UTF-8 string
/// (Local-Key Tags), and are placed in reverse alphabetical order ahead of the Tag Table's anchor point.
/// </para>
/// <para>
/// Each tag has a value, which takes up 2 or 4 bytes, accommodating four different types: narrow string (a
/// number from 1 to 64K-1 which refers to an entry in the Global String Table), wide string (a pointer to a
/// local string), narrow number (-256 to ~64K), or wide number (a decimal with a mantissa -256 to ~1M, and
/// 0 to 3 digits after the decimal point). Numerical values that cannot be represented as narrow or wide
/// numbers are stored as strings.
/// </para>
/// <para>
/// Values of Global-Key Tags are stored immediately after their key, values of Local-Key Tags are stored
/// ahead of their key (this allows us to scan the Tag Table from the Tag-Table Pointer -- forward for
/// global, backward for local).
/// </para>
/// <code>
///          String pointer                          Global string value
///          or wide number                          or narrow number
///          │                                       │
///          │         Local key      Local key      │    Global key
///          │         │              │              │    │
///       ═╦═════════╤═════════╦════╤═════════╦════╤════╦════╤═════════╦═
///     ...║ XX ╎ XX │  "dog"  ║ XX │ "apple" ║ 13 │ XX ║ 42 │ XX ╎ XX ║...
///       ═╩═════════╧═════════╩════╧═════════╩════╧════╩════╧═════════╩═
///                              │              ┃              │
///                      Global string value    Global key     String pointer
///                      or narrow number       ┃              or wide number
///                                             ┃
///                                             Tag-table pointer
///                                             points here
///
///                 ⯇── keys increase backward  ┆  keys increase forward ──⯈
/// </code>
/// <para>Global Keys are 16 bits wide and have the following format:</para>
/// <code>
///     ┏━━━━┳━━━━━━┳━━━┳━━━┓
///     ┃ 15 ┃ 2-14 ┃ 1 ┃ 0 ┃
///     ┗━━┯━┻━━━┯━━┻━┯━┻━┯━┛
///        │     │    │   ╰── type bit: 1 = string, 0 = number
///        │     │    ╰────── size bit: 1 = wide, 0 = narrow
///        │     ╰─────────── entry in the Global String Table (first 8K entries)
///        ╰───────────────── 1 = last global-key tag
/// </code>
/// <para>Local Keys are 32 bits wide and have the following format:</para>
/// <code>
///     ┏━━━━━━┳━━━┳━━━┳━━━┓
///     ┃ 3-31 ┃ 2 ┃ 1 ┃ 0 ┃
///     ┗━━━┯━━┻━┯━┻━┯━┻━┯━┛
///         │    │   │   ╰── type bit: 1 = string, 0 = number
///         │    │   ╰────── size bit: 1 = wide, 0 = narrow
///         │    ╰────────── 1 = last local-key tag
///         ╰─────────────── relative pointer to the key string (see note)
/// </code>
/// <para>
/// Since we only have 29 bits to represent the string pointer, it can only refer to 4-byte aligned strings
/// inside a 1-GB tile file. However, Tag Tables (and hence the position of a Local Key) are only guaranteed
/// to be 2-byte aligned, so we use a little trick: we don't base the pointer off the address where it is
/// stored (as we do for all other pointers), but we add it to the address of the Tag Table, with its lower
/// 2 bits set to zero.
/// </para>
/// <para>If a Tag Table contains Local-Key Tags, bit 0 of the Tag-Table Pointer is set to 1.</para>
/// <para><b>2. Queries</b></para>
/// <para>
/// A Query is composed of one or more Selectors. A Selector applies to one or more feature types (node,
/// way, area, relation) and contains one or more Tag Clauses. Tag Clauses are ordered: global keys, in
/// ascending order, then local keys, in ascending alphabetical order (this allows scanning of keys in the
/// same order in which they are laid out in the Tag Table). There is never more than one Tag Clause per key
/// (multiple clauses in a query string, such as <c>[maxspeed&gt;20][maxspeed&lt;70]</c>, are consolidated by
/// the Query Parser).
/// </para>
/// <para>
/// A Tag Clause is "Positive" if its key must be present and not be "no". A Tag Clause is "Negative" if its
/// key can (or must) be absent, or be "no".
/// </para>
/// <para>
/// A Tag Clause can contain an Expression used to check a tag's value (without an Expression, the generated
/// TagFilter merely checks for presence or absence of a key).
/// </para>
/// </remarks>
internal sealed class ExpressionMatcherCoder : IAstVisitor<Expression>
{

    static readonly MethodInfo FindTagGlobalMethod = Op(nameof(MatcherOps.FindTagGlobal));
    static readonly MethodInfo FindTagLocalMethod = Op(nameof(MatcherOps.FindTagLocal));
    static readonly MethodInfo TagDoubleMethod = Op(nameof(MatcherOps.TagDouble));
    static readonly MethodInfo TagValueEqualsMethod = Op(nameof(MatcherOps.TagValueEquals));
    static readonly MethodInfo TagValueStartsWithMethod = Op(nameof(MatcherOps.TagValueStartsWith));
    static readonly MethodInfo TagValueEndsWithMethod = Op(nameof(MatcherOps.TagValueEndsWith));
    static readonly MethodInfo TagValueContainsMethod = Op(nameof(MatcherOps.TagValueContains));
    static readonly MethodInfo TagValueMatchesMethod = Op(nameof(MatcherOps.TagValueMatches));

    /// <summary>Resolves a public <see cref="MatcherOps"/> method by name, for use as an emitted call target.</summary>
    static MethodInfo Op(string name) => typeof(MatcherOps).GetMethod(name)!;

    static readonly PropertyInfo GlobalStringsProp =
        typeof(ExpressionTagMatcher).GetProperty(nameof(ExpressionTagMatcher.GlobalStrings))!;

    // The compiled body is the open-instance form of Accept: (self, segment, pFeature).
    readonly ParameterExpression _self = Expression.Parameter(typeof(ExpressionTagMatcher), "self");
    readonly ParameterExpression _segment = Expression.Parameter(typeof(Segment), "segment");
    readonly ParameterExpression _pFeature = Expression.Parameter(typeof(int), "pFeature");
    readonly Expression _strings;       // self.GlobalStrings — read from the matcher instance at run time
    readonly GlobalStringTable _table;  // the compile-time table, for resolving global codes to bake in

    ParameterExpression _m = null!; // the located tag of the clause currently being built

    /// <summary>
    /// Creates a coder for one query. The string table is read at run time from the matcher's
    /// <c>self.GlobalStrings</c> for the value operations; the same table is held here so the coder can resolve
    /// global-string codes during compilation and bake them into the emitted tree as constants.
    /// </summary>
    ExpressionMatcherCoder(GlobalStringTable strings)
    {
        _strings = Expression.Property(_self, GlobalStringsProp);
        _table = strings;
    }

    /// <summary>
    /// Compiles a matcher for the selector chain. Returns <c>null</c> only when the runtime cannot compile
    /// expression trees (e.g. Native AOT), in which case the caller uses the equally complete
    /// <see cref="AstTagMatcher"/>. The compiler handles the full matcher grammar; a shape it cannot build
    /// throws (an incomplete-implementation bug), it does not silently fall back.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCoder.createMatcherClass(String, Selector)</c>.</remarks>
    public static Matcher? TryCompile(Selector first, GlobalStringTable strings)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
            return null; // Expression.Compile() needs dynamic code (unavailable in Native AOT)

        var compiler = new ExpressionMatcherCoder(strings);
        var body = compiler.BuildQuery(first);
        var fn = Expression.Lambda<ExpressionTagMatcher.AcceptDelegate>(body, compiler._self, compiler._segment, compiler._pFeature).Compile();
        return new ExpressionTagMatcher(AcceptedTypesOf(first), strings, KeyMaskOf(first), KeyMinOf(first), fn);
    }

    // === expression building ===

    /// <summary>
    /// Builds the boolean for the whole query: the OR of its selectors (an empty chain matches everything).
    /// </summary>
    Expression BuildQuery(Selector first)
    {
        Expression? result = null;
        for (var sel = first; sel != null; sel = sel.Next())
        {
            var s = BuildSelector(sel);
            result = result == null ? s : Expression.OrElse(result, s);
        }
        return result ?? Expression.Constant(true);
    }

    /// <summary>
    /// Builds the boolean for one selector: the AND of its clauses (a clauseless selector matches everything).
    /// </summary>
    Expression BuildSelector(Selector sel)
    {
        Expression? result = null;
        for (var c = sel.FirstClause(); c != null; c = c.Next())
        {
            var cl = BuildClause(c);
            result = result == null ? cl : Expression.AndAlso(result, cl);
        }
        return result ?? Expression.Constant(true);
    }

    /// <summary>
    /// Builds the boolean for one clause: locate its tag once into a local, then apply the presence/"no"/value
    /// logic — mirroring <see cref="AstTagMatcher"/>'s per-clause rules. The value expression (if any) is built
    /// through the visitor over the located tag <c>_m</c>.
    /// </summary>
    Expression BuildClause(TagClause c)
    {
        var m = Expression.Variable(typeof(TagMatch), "m");
        _m = m;
        var find = c.IsGlobalKey()
            ? Expression.Call(FindTagGlobalMethod, _segment, _pFeature, Expression.Constant(c.KeyCode()))
            : Expression.Call(FindTagLocalMethod, _segment, _pFeature, Expression.Constant(Encoding.UTF8.GetBytes(c.Name)));
        var assign = Expression.Assign(m, find);

        var present = Expression.Field(m, nameof(TagMatch.Present));

        // isNo: present and the value is the global string "no" (its code looked up at compile time and baked in)
        _table.TryGetCode("no", out var valueNo);
        var isNo = Expression.AndAlso(
            present,
            Expression.AndAlso(
                Expression.Equal(Expression.Field(m, nameof(TagMatch.Kind)), Expression.Constant(TagValues.GLOBAL_STRING)),
                Expression.Equal(Expression.Field(m, nameof(TagMatch.ValueCode)), Expression.Constant(valueNo))));

        Expression logic;
        var exp = c.Expression();
        if (exp == null)
        {
            logic = c.IsKeyRequired()
                ? Expression.AndAlso(present, Expression.Not(isNo))  // [k]: present and not "no"
                : Expression.OrElse(Expression.Not(present), isNo);  // [!k]: absent or "no"
        }
        else if (c.IsKeyRequired())
        {
            var requiredExplicitlyOnly =
                (c.Flags() & (TagClause.KEY_REQUIRED_EXPLICITLY | TagClause.KEY_REQUIRED_IMPLICITLY))
                    == TagClause.KEY_REQUIRED_EXPLICITLY;
            logic = requiredExplicitlyOnly
                ? Expression.AndAlso(present, Expression.AndAlso(Expression.Not(isNo), exp.Accept(this)))
                : Expression.AndAlso(present, exp.Accept(this));
        }
        else
        {
            // [k!=v]: matches if absent, or the expression holds
            logic = Expression.OrElse(Expression.Not(present), exp.Accept(this));
        }

        return Expression.Block(typeof(bool), new[] { m }, assign, logic);
    }

    // === IAstVisitor: builds the boolean for a value expression over the located tag _m ===

    /// <summary>
    /// Builds the boolean for a value-expression node: <c>AND</c>/<c>OR</c> recurse via <c>Accept</c>, while the
    /// leaf comparison operators consume their operands directly (the located tag's value vs. the literal),
    /// emitting a call into <see cref="MatcherOps"/>.
    /// </summary>
    /// <remarks>Mirrors <see cref="AstTagMatcher"/>'s EvalExpr.</remarks>
    public Expression VisitBinary(AstBinary b)
    {
        var op = b.Operator;
        if (op == Operator.AND)
            return Expression.AndAlso(b.Left.Accept(this), b.Right.Accept(this));
        if (op == Operator.OR)
            return Expression.OrElse(b.Left.Accept(this), b.Right.Accept(this));

        var lit = ((Literal)b.Right).Value;
        var kind = Expression.Field(_m, nameof(TagMatch.Kind));
        var valueCode = Expression.Field(_m, nameof(TagMatch.ValueCode));
        var valueBytes = Expression.Field(_m, nameof(TagMatch.ValueBytes));

        if (op == Operator.LT || op == Operator.LE || op == Operator.GT || op == Operator.GE)
        {
            var d = Expression.Constant(Convert.ToDouble(lit, CultureInfo.InvariantCulture));
            var tagD = Expression.Call(TagDoubleMethod, valueBytes, kind, valueCode, _strings);
            if (op == Operator.LT)
                return Expression.LessThan(tagD, d);
            if (op == Operator.LE)
                return Expression.LessThanOrEqual(tagD, d);
            if (op == Operator.GT)
                return Expression.GreaterThan(tagD, d);
            return Expression.GreaterThanOrEqual(tagD, d);
        }
        if (op == Operator.EQ || op == Operator.NE)
        {
            Expression eq;
            if (lit is double dd)
            {
                var tagD = Expression.Call(TagDoubleMethod, valueBytes, kind, valueCode, _strings);
                eq = Expression.Equal(tagD, Expression.Constant(dd));
            }
            else
            {
                eq = StringCall(TagValueEqualsMethod, valueBytes, kind, valueCode, lit);
            }
            return op == Operator.EQ ? eq : Expression.Not(eq);
        }
        if (op == Operator.IN)
            return StringCall(TagValueContainsMethod, valueBytes, kind, valueCode, lit);
        if (op == QueryParser.STARTS_WITH)
            return StringCall(TagValueStartsWithMethod, valueBytes, kind, valueCode, lit);
        if (op == QueryParser.ENDS_WITH)
            return StringCall(TagValueEndsWithMethod, valueBytes, kind, valueCode, lit);
        if (op == Operator.MATCH)
            return RegexCall(valueBytes, kind, valueCode, (string)lit!);
        if (op == Operator.NOT_MATCH)
            return Expression.Not(RegexCall(valueBytes, kind, valueCode, (string)lit!));

        throw new NotSupportedException("Compiled matcher has no implementation for operator: " + op);
    }

    /// <summary>Builds the boolean for a unary <c>NOT</c>.</summary>
    public Expression VisitUnary(AstUnary u) => Expression.Not(u.Operand.Accept(this));

    // The remaining visit methods are not reached by a matcher value expression — comparisons consume their
    // literal/variable operands directly, and call/conditional/string nodes are not part of the grammar — so
    // each throws if the emitter is ever pointed at one (a completeness bug).

    /// <summary>Not reached: a bare literal is consumed by its comparison operator; throws.</summary>
    public Expression VisitLiteral(Literal exp) => throw Unsupported();

    /// <summary>Not reached: a variable (the tag value) is consumed by its comparison operator; throws.</summary>
    public Expression VisitVariable(Variable exp) => throw Unsupported();

    /// <summary>Not part of the matcher grammar: function-call expressions are unsupported; throws.</summary>
    public Expression VisitCall(CallExpression exp) => throw Unsupported();

    /// <summary>Not part of the matcher grammar: conditional (ternary) expressions are unsupported; throws.</summary>
    public Expression VisitConditional(AstConditional exp) => throw Unsupported();

    /// <summary>Not part of the matcher grammar: string-template expressions are unsupported; throws.</summary>
    public Expression VisitString(StringExpression exp) => throw Unsupported();

    /// <summary>Not reached: a generic expression node has no matcher meaning; throws.</summary>
    public Expression VisitExpression(AstExpression exp) => throw Unsupported();

    /// <summary>The exception thrown when the emitter meets a node it does not implement (a completeness bug).</summary>
    static Exception Unsupported() =>
        new NotSupportedException("Compiled matcher has no implementation for this value-expression node");

    /// <summary>
    /// Emits a call to a string-comparison op (<c>TagValueEquals</c>/<c>Contains</c>/…) over the located tag's
    /// value, baking the literal as a constant <see cref="MatchLiteral"/> (text + UTF-8 + global code).
    /// </summary>
    Expression StringCall(MethodInfo method, Expression valueBytes, Expression kind, Expression valueCode, object? literal) =>
        Expression.Call(method, valueBytes, kind, valueCode, _strings, Expression.Constant(MakeLiteral(literal)));

    /// <summary>Emits a call to the regex op over the located tag's value, baking the compiled <see cref="Regex"/> as a constant.</summary>
    Expression RegexCall(Expression valueBytes, Expression kind, Expression valueCode, string pattern) =>
        Expression.Call(TagValueMatchesMethod, valueBytes, kind, valueCode, _strings,
            Expression.Constant(new Regex(pattern)));

    /// <summary>
    /// Builds the <see cref="MatchLiteral"/> for a query literal, carrying its global-string code when it is
    /// one so that equality against a global-string value reduces to an integer compare.
    /// </summary>
    static MatchLiteral MakeLiteral(object? lit) =>
        lit is GlobalString gs ? new MatchLiteral(gs.StringValue, gs.Value) : new MatchLiteral((string)lit!);

    // === selector-chain summaries (shared with the interpreter's logic) ===

    /// <summary>The OR of the match-types across the selector chain (the matcher's accepted feature types).</summary>
    /// <remarks>Port-only.</remarks>
    static int AcceptedTypesOf(Selector first)
    {
        var t = 0;
        for (Selector? s = first; s != null; s = s.Next())
            t |= s.MatchTypes();
        return t;
    }

    /// <summary>The OR of the key-index bits across the selector chain, for index-based pruning.</summary>
    /// <remarks>Port-only.</remarks>
    static int KeyMaskOf(Selector first)
    {
        var m = 0;
        for (var s = first; s != null; s = s.Next())
            m |= s.IndexBitsValue();
        return m;
    }

    /// <summary>The minimum key-index value across the selector chain, for index-based pruning.</summary>
    /// <remarks>Port-only.</remarks>
    static int KeyMinOf(Selector first)
    {
        var min = int.MaxValue;
        for (var s = first; s != null; s = s.Next())
            if (s.IndexBitsValue() < min)
                min = s.IndexBitsValue();
        return min;
    }

}
