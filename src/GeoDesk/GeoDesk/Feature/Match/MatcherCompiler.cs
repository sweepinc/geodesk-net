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
/// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler</c>.</remarks>
internal class MatcherCompiler
{

    readonly MatcherParser _parser;
    readonly string[] _codesToStrings;
    readonly int _valueNo;
    readonly Dictionary<string, Matcher> _matchers = new Dictionary<string, Matcher>();

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler(ObjectIntMap, String[], IntIntMap)</c>.</remarks>
    public MatcherCompiler(IReadOnlyDictionary<string, int> stringsToCodes, string[] codesToStrings,
        IReadOnlyDictionary<int, int> keysToCategories)
    {
        _codesToStrings = codesToStrings;
        _valueNo = stringsToCodes.TryGetValue("no", out var v) ? v : 0;
        if (_valueNo == 0) throw new QueryException("String table must include \"no\"");
        _parser = new MatcherParser(stringsToCodes, keysToCategories);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler.getMatcher(String)</c>.</remarks>
    public Matcher GetMatcher(string query)
    {
        if (_matchers.TryGetValue(query, out var matcher)) return matcher;
        matcher = CreateMatcher(query);
        _matchers[query] = matcher;
        return matcher;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.match.MatcherCompiler.createMatcher(String)</c>.</remarks>
    Matcher CreateMatcher(string query)
    {
        _parser.Parse(query);
        var selectors = _parser.Query();
        if (selectors == null)
        {
            // empty query matches everything
            return Matcher.ALL;
        }

        var sel = selectors;
        var commonType = 0;
        while (sel != null)
        {
            var type = sel.MatchTypes();
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

        return new InterpretedMatcher(selectors, _codesToStrings, _valueNo);
    }

}
