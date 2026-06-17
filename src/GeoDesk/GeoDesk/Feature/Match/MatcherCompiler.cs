/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

namespace GeoDesk.Feature.Match;

/// <summary>
/// Compiles GOQL query strings into <see cref="Matcher"/> instances and caches them.
///
/// PORT NOTE: the Java original generates JVM bytecode (via ASM) per query. This port uses
/// the AST-interpreting <see cref="InterpretedMatcher"/> (see PORT.md). A runtime compiler
/// (System.Linq.Expressions or Reflection.Emit — undecided) can later be swapped in here
/// behind GetMatcher without changing callers.
/// </summary>
public class MatcherCompiler
{
    private readonly MatcherParser parser;
    private readonly string[] codesToStrings;
    private readonly int valueNo;
    private readonly Dictionary<string, Matcher> matchers = new Dictionary<string, Matcher>();

    public MatcherCompiler(IReadOnlyDictionary<string, int> stringsToCodes, string[] codesToStrings,
        IReadOnlyDictionary<int, int> keysToCategories)
    {
        this.codesToStrings = codesToStrings;
        valueNo = stringsToCodes.TryGetValue("no", out int v) ? v : 0;
        if (valueNo == 0) throw new QueryException("String table must include \"no\"");
        parser = new MatcherParser(stringsToCodes, keysToCategories);
    }

    public Matcher GetMatcher(string query)
    {
        if (matchers.TryGetValue(query, out Matcher? matcher)) return matcher;
        matcher = CreateMatcher(query);
        matchers[query] = matcher;
        return matcher;
    }

    private Matcher CreateMatcher(string query)
    {
        parser.Parse(query);
        Selector? selectors = parser.Query();
        if (selectors == null)
        {
            // empty query matches everything
            return Matcher.ALL;
        }

        Selector? sel = selectors;
        int commonType = 0;
        while (sel != null)
        {
            int type = sel.MatchTypes();
            if (commonType == 0)
            {
                commonType = type;
            }
            else if (type != commonType)
            {
                throw new QueryException("Polyform queries are not supported.");
            }
            sel = sel.Next();
        }

        return new InterpretedMatcher(selectors, codesToStrings, valueNo);
    }
}
