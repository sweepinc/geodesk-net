/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using GeoDesk.Common.Fab;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Feature.Store;

using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Tests.Feature.Query;

/// <summary>
/// Differential test for the compiled matcher: for every query the <see cref="ExpressionMatcherCoder"/>
/// can compile, asserts the <see cref="ExpressionTagMatcher"/> produces the same result as the
/// <see cref="AstTagMatcher"/> oracle on every tag case in the corpus. Queries the builder does not
/// support are skipped here (they remain covered by <see cref="MatcherCompilerTest"/>).
/// </summary>
public class ExpressionMatcherDifferentialTest
{
    private readonly ITestOutputHelper output;
    private readonly string[] globalStrings;
    private readonly Dictionary<string, int> stringsToCodes;
    private readonly GlobalStringTable globalStringTable;

    public ExpressionMatcherDifferentialTest(ITestOutputHelper output)
    {
        this.output = output;
        (globalStrings, stringsToCodes) = LoadStrings();
        globalStringTable = GlobalStringTable.FromStrings(globalStrings);
    }

    private static string ResourcePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "TestResources", "feature", name);

    private static (string[], Dictionary<string, int>) LoadStrings()
    {
        var list = new List<string> { "" };
        var map = new Dictionary<string, int>();
        foreach (string s in File.ReadLines(ResourcePath("strings.txt")))
        {
            map[s] = list.Count; // 1-based
            list.Add(s);
        }
        return (list.ToArray(), map);
    }

    private sealed class QueryTestCase
    {
        public string query = "";
        public readonly List<string> tagCases = new List<string>();
    }

    private sealed class QueryReader : FabReader
    {
        public readonly List<QueryTestCase> Cases = new List<QueryTestCase>();
        private QueryTestCase? testCase;

        protected override void BeginKey(string key, string value)
        {
            testCase = new QueryTestCase { query = value };
        }

        protected override void EndKey()
        {
            if (testCase != null) Cases.Add(testCase);
            testCase = null;
        }

        protected override void KeyValue(string key, string value)
        {
            if (testCase == null || key == "error") return;
            testCase.tagCases.Add(key);
        }
    }

    private List<QueryTestCase> LoadQueries()
    {
        var reader = new QueryReader();
        reader.ReadFile(ResourcePath("queries.fab"));
        return reader.Cases;
    }

    [Fact]
    public void CompiledMatchesInterpreted()
    {
        var tester = new TagTableTester();
        var cases = LoadQueries();

        var failures = new StringBuilder();
        int compiledQueries = 0;
        int checks = 0;

        foreach (var qtc in cases)
        {
            Selector? sel;
            var parser = new QueryParser(globalStringTable, null);
            try
            {
                parser.Parse(qtc.query);
                sel = parser.Query();
            }
            catch (QueryException)
            {
                continue; // parse-error cases are the interpreter test's concern
            }

            if (sel == null) continue;

            var compiled = ExpressionMatcherCoder.TryCompile(sel, globalStringTable);
            if (compiled == null) continue; // shape not yet supported by the emitter

            compiledQueries++;
            var interpreted = new AstTagMatcher(sel, globalStringTable);

            foreach (var tagCase in qtc.tagCases)
            {
                using var tags = tester.MakeCase(tagCase, 0, null);
                var expected = interpreted.Accept(tags, 0);
                var actual = compiled.Accept(tags, 0);
                checks++;
                if (actual != expected)
                {
                    failures.AppendLine(
                        $"Tags '{tagCase}' for query [{qtc.query}]: interpreted={expected}, compiled={actual}");
                }
            }
        }

        output.WriteLine($"Compiled {compiledQueries} queries, {checks} compiled/interpreted comparisons");
        Assert.True(failures.Length == 0, "\n" + failures);
    }
}
