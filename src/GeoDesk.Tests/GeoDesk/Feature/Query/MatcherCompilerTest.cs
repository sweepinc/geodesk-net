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
using Clarisma.Common.Fab;
using GeoDesk.Feature.Match;
using NioBuffer = Java.Nio.ByteBuffer;
using Xunit;
using Xunit.Abstractions;

namespace GeoDesk.Feature.Query;

/// <summary>
/// Tests the Query Matcher against feature/queries.fab (query → expected results per object),
/// feature/tags.fab (objects and their tags) and feature/strings.txt (the global string table).
/// Uses <see cref="InterpretedMatcher"/> in place of the bytecode-compiled matcher.
/// </summary>
public class MatcherCompilerTest
{
    private readonly ITestOutputHelper output;
    private readonly string[] globalStrings;
    private readonly Dictionary<string, int> stringsToCodes;

    public MatcherCompilerTest(ITestOutputHelper output)
    {
        this.output = output;
        (globalStrings, stringsToCodes) = LoadStrings();
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
        public string? error;
        public readonly List<KeyValuePair<string, bool>> expected = new List<KeyValuePair<string, bool>>();
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
            if (testCase == null) return;
            if (key == "error")
            {
                testCase.error = value;
                return;
            }
            testCase.expected.Add(new KeyValuePair<string, bool>(key, bool.Parse(value)));
        }
    }

    private List<QueryTestCase> LoadQueries()
    {
        var reader = new QueryReader();
        reader.ReadFile(ResourcePath("queries.fab"));
        return reader.Cases;
    }

    [Fact]
    public void Test()
    {
        var tester = new TagTableTester();
        List<QueryTestCase> cases = LoadQueries();
        output.WriteLine($"Testing {cases.Count} queries");
        int valueNo = stringsToCodes["no"];

        var failures = new StringBuilder();
        int passed = 0;
        foreach (QueryTestCase qtc in cases)
        {
            Selector? sel;
            var parser = new MatcherParser(stringsToCodes, null);
            try
            {
                parser.Parse(qtc.query);
                sel = parser.Query();
            }
            catch (QueryException)
            {
                if (qtc.error == null)
                {
                    failures.AppendLine($"Unexpected parse error for: {qtc.query}");
                }
                continue;
            }

            if (sel == null) continue;
            var matcher = new InterpretedMatcher(sel, globalStrings, valueNo);

            foreach (KeyValuePair<string, bool> e in qtc.expected)
            {
                NioBuffer tags = tester.MakeCase(e.Key, 0, null);
                bool result = matcher.Accept(tags, 0);
                if (result != e.Value)
                {
                    failures.AppendLine($"Tags '{e.Key}' for query [{qtc.query}]: expected {e.Value}, got {result}");
                }
                else
                {
                    passed++;
                }
            }
        }

        output.WriteLine($"Passed {passed} object/query checks");
        Assert.True(failures.Length == 0, "\n" + failures);
    }
}
