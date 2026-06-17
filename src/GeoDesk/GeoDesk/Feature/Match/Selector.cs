/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Diagnostics;
using Clarisma.Common.Ast;
using GeoDesk.Feature.Query;

namespace GeoDesk.Feature.Match;

/// <summary>
/// A class that represents a parsed GOQL selector expression
/// (e.g. "na[amenity=restaurant][cuisine=pizza]").
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.Selector</c>.</remarks>
public class Selector : Expression
{
    public const int CLAUSE_LOCAL_REQUIRED = 1;
    public const int CLAUSE_LOCAL_OPTIONAL = 2;
    public const int CLAUSE_GLOBAL_REQUIRED = 4;
    public const int CLAUSE_GLOBAL_OPTIONAL = 8;

    private int matchTypes;
    private int clauseTypes;
    private int indexBits;
    private Selector? next;
    private TagClause? firstClause;

    public Selector(int matchTypes)
    {
        this.matchTypes = matchTypes;
    }

    public int MatchTypes()
    {
        return matchTypes;
    }

    public int ClauseTypes()
    {
        return clauseTypes;
    }

    public int IndexBitsValue()
    {
        return indexBits;
    }

    public Selector? Next()
    {
        return next;
    }

    public void SetNext(Selector? sel)
    {
        next = sel;
    }

    internal TagClause? FirstClause()
    {
        return firstClause;
    }

    public void Add(TagClause clause)
    {
        if (clause.IsKeyRequired())
        {
            clauseTypes |= clause.KeyCode() == 0 ?
                CLAUSE_LOCAL_REQUIRED : CLAUSE_GLOBAL_REQUIRED;
            indexBits |= IndexBits.FromCategory(clause.Category());
        }
        else
        {
            clauseTypes |= clause.KeyCode() == 0 ?
                CLAUSE_LOCAL_OPTIONAL : CLAUSE_GLOBAL_OPTIONAL;
        }
        if (firstClause == null)
        {
            firstClause = clause;
            clause.next = null;
            return;
        }
        int comp = clause.CompareTo(firstClause);
        if (comp == 0)
        {
            firstClause.Absorb(clause, true);
            return;
        }
        if (comp < 0)
        {
            clause.next = firstClause;
            firstClause = clause;
            return;
        }
        TagClause prev = firstClause;
        for (; ; )
        {
            TagClause? c = prev.next;
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
    /// Splits off a shallow copy of this Selector matching the given type bits;
    /// those bits are removed from this Selector.
    /// </summary>
    public Selector Split(int type)
    {
        Debug.Assert((matchTypes & type) == type, "Selector does not match this type");
        Debug.Assert((matchTypes ^ type) != 0, "No reason to split this selector");
        Selector other = new Selector(type);
        other.clauseTypes = clauseTypes;
        other.indexBits = indexBits;
        other.firstClause = firstClause;
        matchTypes &= ~type;
        return other;
    }

    public override R Accept<R>(IAstVisitor<R> visitor)
    {
        visitor.VisitExpression(this);
        return default!;
    }
}
