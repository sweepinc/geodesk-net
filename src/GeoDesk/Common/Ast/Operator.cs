/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Ast;

/// <summary>
/// Describes an operator usable in expressions: its internal name, optional textual symbol, and
/// precedence level. Common operators are exposed as shared static singletons grouped into
/// arithmetic, comparison, and logical categories.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator</c>.</remarks>
internal class Operator
{

    readonly string _name;
    readonly string? _symbol;
    readonly float _precedence;

    /// <summary>
    /// Creates an operator with the given internal <paramref name="name"/>, display
    /// <paramref name="symbol"/> (may be null), and <paramref name="precedence"/> level.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator(String, String, float)</c>.</remarks>
    public Operator(string name, string? symbol, float precedence)
    {
        _name = name;
        _symbol = symbol;
        _precedence = precedence;
    }

    /// <summary>
    /// The internal name of this operator (for example <c>"add"</c> or <c>"eq"</c>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator.name()</c>.</remarks>
    public string Name => _name;

    /// <summary>
    /// The textual symbol used to render this operator (for example <c>"+"</c>), or null if it has none.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator.symbol()</c>.</remarks>
    public string? Symbol => _symbol;

    /// <summary>
    /// The binding precedence of this operator; higher values bind more tightly during parsing.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator.precedence()</c>.</remarks>
    public float Precedence => _precedence;

    /// <summary>
    /// Returns a hash code derived from the operator symbol.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return _symbol?.GetHashCode() ?? 0;
    }

    // TODO: operator should always be treated as singleton
    /// <summary>
    /// Value equality: two operators are equal when their name and symbol match.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator.equals(Object)</c>.</remarks>
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is Operator other)
        {
            return _name.Equals(other._name) &&
                string.Equals(_symbol, other._symbol);
        }
        return false;
    }

    /// <summary>
    /// Returns the operator's symbol, or an empty string if it has none.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator.toString()</c>.</remarks>
    public override string ToString()
    {
        return _symbol ?? "";
    }

    public const float COMPARISON_LEVEL = 40;
    public const float ADDITION_LEVEL = 50;
    public const float MULTIPLICATION_LEVEL = 60;

    // Arithmetic

    public static readonly Operator ADD = new Operator("add", "+", ADDITION_LEVEL);
    public static readonly Operator SUBTRACT = new Operator("sub", "-", ADDITION_LEVEL);
    public static readonly Operator MULTIPLY = new Operator("mul", "*", MULTIPLICATION_LEVEL);
    public static readonly Operator DIVIDE = new Operator("div", "/", MULTIPLICATION_LEVEL);
    public static readonly Operator MODULO = new Operator("mod", "%", MULTIPLICATION_LEVEL);
    public static readonly Operator UNARY_MINUS = new Operator("neg", "-", MULTIPLICATION_LEVEL + 5);

    // Comparison

    public static readonly Operator EQ = new Operator("eq", "=", COMPARISON_LEVEL);
    public static readonly Operator NE = new Operator("ne", "!=", COMPARISON_LEVEL);
    public static readonly Operator LT = new Operator("lt", "<", COMPARISON_LEVEL);
    public static readonly Operator LE = new Operator("le", "<=", COMPARISON_LEVEL);
    public static readonly Operator GT = new Operator("gt", ">", COMPARISON_LEVEL);
    public static readonly Operator GE = new Operator("ge", ">=", COMPARISON_LEVEL);

    public static readonly Operator IN = new Operator("in", "in", COMPARISON_LEVEL);
    public static readonly Operator NOT_IN = new Operator("notIn", null, COMPARISON_LEVEL);
    public static readonly Operator MATCH = new Operator("match", "like", COMPARISON_LEVEL);
    public static readonly Operator NOT_MATCH = new Operator("notMatch", null, COMPARISON_LEVEL);
        // TODO: needed?

    // Logical

    public static readonly Operator AND = new Operator("and", "and", COMPARISON_LEVEL - 10);
    public static readonly Operator OR = new Operator("or", "or", COMPARISON_LEVEL - 20);
    public static readonly Operator NOT = new Operator("not", "not", COMPARISON_LEVEL - 5);

}
