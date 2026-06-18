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
/// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause</c>.</remarks>
internal class TagClause : Variable, IComparable<TagClause>
{

    // explicit is [k], implicit are all others except [k!=v] and [k!~v]
    public const int KEY_REQUIRED_EXPLICITLY = 128;
    public const int KEY_REQUIRED_IMPLICITLY = 64;

    // The types of values that a clause considers
    public const int VALUE_GLOBAL_STRING = 1;
    public const int VALUE_LOCAL_STRING = 2;
    public const int VALUE_ANY_STRING = 4;
    public const int VALUE_DOUBLE = 8;
    public const int VALUE_ANY = 15;

    int _flags;
    readonly int _key;
    readonly int _category;
    Expression? _exp;
    public TagClause? next;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause(int, String, int, int, Expression)</c>.</remarks>
    public TagClause(int flags, string keyString, int key, int category, Expression? exp) :
        base(keyString)
    {
        _flags = flags;
        _key = key;
        _category = category;
        _exp = exp;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.flags()</c>.</remarks>
    public int Flags() => _flags;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.next()</c>.</remarks>
    public TagClause? Next()
    {
        return next;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.expression()</c>.</remarks>
    public Expression? Expression()
    {
        return _exp;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.setExpression(Expression)</c>.</remarks>
    public void SetExpression(Expression? exp)
    {
        _exp = exp;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.keyCode()</c>.</remarks>
    public int KeyCode()
    {
        return _key;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.category()</c>.</remarks>
    public int Category()
    {
        return _category;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.isKeyRequired()</c>.</remarks>
    public bool IsKeyRequired()
    {
        return (_flags & (KEY_REQUIRED_EXPLICITLY | KEY_REQUIRED_IMPLICITLY)) != 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.isLocalKey()</c>.</remarks>
    public bool IsLocalKey() => _key == 0;

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.isGlobalKey()</c>.</remarks>
    public bool IsGlobalKey() => _key != 0;

    /// <summary>
    /// Tag Clauses are grouped and sorted as follows:
    /// - Common tags in ascending order of the key code
    /// - Uncommon tags in ascending alphabetical order
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.compareTo(TagClause)</c>.</remarks>
    public int CompareTo(TagClause? other)
    {
        if (_key == 0)
        {
            return other!._key == 0 ? string.CompareOrdinal(Name, other.Name) : 1;
        }
        return other!._key == 0 ? -1 : _key.CompareTo(other._key);
    }

    /// <summary>
    /// Checks if this clause can be AND-combined with another clause. If so, returns true. If the
    /// other clause is superfluous, changes both clauses to make sense and returns false, in which
    /// case no further merging steps should be taken. If a combination of clauses is nonsensical,
    /// throws QueryException.
    /// </summary>
    /// <returns>true if the clauses can be combined, false if they are problematic but have been fixed</returns>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.checkConjoined(TagClause)</c>.</remarks>
    bool CheckConjoined(TagClause other)
    {
        if (!IsKeyRequired() && _exp == null)
        {
            if (other.IsKeyRequired())
            {
                // [!k][k] and [!k][k=v] will never yield any results
                throw new QueryException(string.Format(CultureInfo.InvariantCulture,
                    "Conflicting clauses for key {0}", Name));
            }
            else if (other._exp != null)
            {
                // [!k] combined with [k!=v] is simply [!k]
                other._exp = null;
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Merges another TagClause into this one. For example, a filter query may consist of two clauses
    /// with the same key, e.g. <c>[k&gt;3][k&lt;8]</c>. The generated code expects to visit each tag
    /// only once, so we need to merge the two clauses into a single one that contains an AND
    /// expression.
    /// </summary>
    /// <param name="other">another TagClause with the same key</param>
    /// <param name="conjoin">true if the expressions should be combined using AND, or false if logical OR should be used</param>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.TagClause.absorb(TagClause, boolean)</c>.</remarks>
    public void Absorb(TagClause other, bool conjoin)
    {
        if (conjoin)
        {
            if (!CheckConjoined(other))
                return;
            if (!other.CheckConjoined(this))
                return;
            _flags |= other._flags;
            if (_exp == null)
            {
                _exp = other._exp;
            }
            else if (other._exp != null)
            {
                _exp = new BinaryExpression(Operator.AND, _exp, other._exp);
            }
        }
        else
        {
            // TODO
        }
    }

}
