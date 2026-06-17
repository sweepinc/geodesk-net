/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Ast;

/// <remarks>Ported from Java <c>com.clarisma.common.ast.Operator</c>.</remarks>
public class Operator
{
    private readonly string name;
    private readonly string? symbol;
    private readonly float precedence;

    // TODO: associativity

    public Operator(string name, string? symbol, float precedence)
    {
        this.name = name;
        this.symbol = symbol;
        this.precedence = precedence;
    }

    public string Name => name;

    public string? Symbol => symbol;

    public float Precedence => precedence;

    public override int GetHashCode()
    {
        return symbol?.GetHashCode() ?? 0;
    }

    // TODO: operator should always be treated as singleton
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is Operator other)
        {
            return name.Equals(other.name) &&
                string.Equals(symbol, other.symbol);
        }
        return false;
    }

    public override string ToString()
    {
        return symbol ?? "";
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
