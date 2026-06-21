/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Diagnostics;

using GeoDesk.Common.Ast;
using GeoDesk.Feature.Query;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A class that represents a parsed GOQL selector expression
/// (e.g. "na[amenity=restaurant][cuisine=pizza]").
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector</c>.</remarks>
internal class Selector : Expression
{

    public const int CLAUSE_LOCAL_REQUIRED = 1;
    public const int CLAUSE_LOCAL_OPTIONAL = 2;
    public const int CLAUSE_GLOBAL_REQUIRED = 4;
    public const int CLAUSE_GLOBAL_OPTIONAL = 8;

    int _matchTypes;
    int _clauseTypes;
    int _indexBits;
    Selector? _next;
    TagClause? _firstClause;

    /// <summary>
    /// Creates a selector that matches the given feature-type bits and starts with an empty clause list.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector(int)</c>.</remarks>
    public Selector(int matchTypes)
    {
        _matchTypes = matchTypes;
    }

    /// <summary>
    /// Returns the bitmask of feature types (node/way/area/relation) this selector matches.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.matchTypes()</c>.</remarks>
    public int MatchTypes()
    {
        return _matchTypes;
    }

    /// <summary>
    /// Returns the bitmask describing which kinds of tag clause this selector contains: local versus
    /// global key, and required versus optional.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.clauseTypes()</c>.</remarks>
    public int ClauseTypes()
    {
        return _clauseTypes;
    }

    /// <summary>
    /// Returns the spatial-index category bits required by this selector's clauses, used to decide which
    /// tile index buckets must be scanned.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.indexBits()</c>.</remarks>
    public int IndexBitsValue()
    {
        return _indexBits;
    }

    /// <summary>
    /// Returns the next selector in the chain — selectors are linked together to represent a query that
    /// spans multiple feature types — or null if this is the last.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.next()</c>.</remarks>
    public Selector? Next()
    {
        return _next;
    }

    /// <summary>
    /// Links the given selector after this one in the chain.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.setNext(Selector)</c>.</remarks>
    public void SetNext(Selector? sel)
    {
        _next = sel;
    }

    /// <summary>
    /// Returns the first tag clause in this selector's sorted clause list, or null if it has no clauses.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.firstClause()</c>.</remarks>
    internal TagClause? FirstClause()
    {
        return _firstClause;
    }

    /// <summary>
    /// Inserts a tag clause into this selector, keeping the clause list sorted by key, merging clauses
    /// with an equal key, and updating the clause-type and spatial-index-bit summaries accordingly.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.add(TagClause)</c>.</remarks>
    public void Add(TagClause clause)
    {
        if (clause.IsKeyRequired())
        {
            _clauseTypes |= clause.KeyCode() == 0 ?
                CLAUSE_LOCAL_REQUIRED : CLAUSE_GLOBAL_REQUIRED;
            _indexBits |= IndexBits.FromCategory(clause.Category());
        }
        else
        {
            _clauseTypes |= clause.KeyCode() == 0 ?
                CLAUSE_LOCAL_OPTIONAL : CLAUSE_GLOBAL_OPTIONAL;
        }
        if (_firstClause == null)
        {
            _firstClause = clause;
            clause.next = null;
            return;
        }
        var comp = clause.CompareTo(_firstClause);
        if (comp == 0)
        {
            _firstClause.Absorb(clause, true);
            return;
        }
        if (comp < 0)
        {
            clause.next = _firstClause;
            _firstClause = clause;
            return;
        }
        var prev = _firstClause;
        for (; ; )
        {
            var c = prev.next;
            if (c == null)
            {
                prev.next = clause;
                clause.next = null;
                return;
            }
            comp = clause.CompareTo(c);
            if (comp == 0)
            {
                c.Absorb(clause, true);
                return;
            }
            if (comp < 0)
            {
                prev.next = clause;
                clause.next = c;
                return;
            }
            prev = c;
        }
    }

    /// <summary>
    /// Splits off a shallow copy of this Selector matching the given type bits; those bits are removed
    /// from this Selector.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.split(int)</c>.</remarks>
    public Selector Split(int type)
    {
        Debug.Assert((_matchTypes & type) == type, "Selector does not match this type");
        Debug.Assert((_matchTypes ^ type) != 0, "No reason to split this selector");
        var other = new Selector(type);
        other._clauseTypes = _clauseTypes;
        other._indexBits = _indexBits;
        other._firstClause = _firstClause;
        _matchTypes &= ~type;
        return other;
    }

    /// <summary>
    /// Dispatches this selector to the given AST visitor's expression-visit method.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector.accept(AstVisitor)</c>.</remarks>
    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        visitor.VisitExpression(this);
        return default!;
    }

}
