/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using Clarisma.Common.Ast;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A class that represents a parsed GOQL tag clause (e.g. "[amenity=restaurant]").
/// </summary>
public class TagClause : Variable, IComparable<TagClause>
{
    private int flags;
    private readonly int key;
    private readonly int category;
    private Expression? exp;
    public TagClause? next;

    // explicit is [k], implicit are all others except [k!=v] and [k!~v]
    public const int KEY_REQUIRED_EXPLICITLY = 128;
    public const int KEY_REQUIRED_IMPLICITLY = 64;

    // The types of values that a clause considers
    public const int VALUE_GLOBAL_STRING = 1;
    public const int VALUE_LOCAL_STRING = 2;
    public const int VALUE_ANY_STRING = 4;
    public const int VALUE_DOUBLE = 8;
    public const int VALUE_ANY = 15;

    public TagClause(int flags, string keyString, int key, int category, Expression? exp)
        : base(keyString)
    {
        this.flags = flags;
        this.key = key;
        this.category = category;
        this.exp = exp;
    }

    public int Flags() => flags;

    public TagClause? Next()
    {
        return next;
    }

    public Expression? Expression()
    {
        return exp;
    }

    public void SetExpression(Expression? exp)
    {
        this.exp = exp;
    }

    public int KeyCode()
    {
        return key;
    }

    public int Category()
    {
        return category;
    }

    public bool IsKeyRequired()
    {
        return (flags & (KEY_REQUIRED_EXPLICITLY | KEY_REQUIRED_IMPLICITLY)) != 0;
    }

    public bool IsLocalKey() => key == 0;

    public bool IsGlobalKey() => key != 0;

    /// <summary>
    /// Tag Clauses are grouped and sorted as follows:
    /// - Common tags in ascending order of the key code
    /// - Uncommon tags in ascending alphabetical order
    /// </summary>
    public int CompareTo(TagClause? other)
    {
        if (key == 0)
        {
            return other!.key == 0 ? string.CompareOrdinal(Name, other.Name) : 1;
        }
        return other!.key == 0 ? -1 : key.CompareTo(other.key);
    }

    private bool CheckConjoined(TagClause other)
    {
        if (!IsKeyRequired() && exp == null)
        {
            if (other.IsKeyRequired())
            {
                // [!k][k] and [!k][k=v] will never yield any results
                throw new QueryException(string.Format(CultureInfo.InvariantCulture,
                    "Conflicting clauses for key {0}", Name));
            }
            else if (other.exp != null)
            {
                // [!k] combined with [k!=v] is simply [!k]
                other.exp = null;
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Merges another TagClause into this one (e.g. <c>[k&gt;3][k&lt;8]</c> become one clause
    /// with an AND expression).
    /// </summary>
    public void Absorb(TagClause other, bool conjoin)
    {
        if (conjoin)
        {
            if (!CheckConjoined(other)) return;
            if (!other.CheckConjoined(this)) return;
            flags |= other.flags;
            if (exp == null)
            {
                exp = other.exp;
            }
            else if (other.exp != null)
            {
                exp = new BinaryExpression(Operator.AND, exp, other.exp);
            }
        }
        else
        {
            // TODO
        }
    }
}
