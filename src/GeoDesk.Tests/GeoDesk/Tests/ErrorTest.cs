/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Threading;

using GeoDesk.Common.Util;
using GeoDesk.Feature;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest</c>.</remarks>
[Collection("GolFixture")]
public class ErrorTest : AbstractFeatureTest
{

    /// <summary>Filter that throws an exception after accepting 10 features.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest.BadFilter</c>.</remarks>
    class BadFilter : IFilter
    {

        int _count;

        /// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest.BadFilter.accept(Feature)</c>.</remarks>
        public bool Accept(GeoDesk.Feature.IFeature feature)
        {
            var currentCount = Interlocked.Increment(ref _count);
            if (currentCount > 10)
            {
                throw new Exception("[Test] Something bad happened in the BadFilter!");
            }
            Log.Debug("Accepted %d features (%s)", currentCount, feature);
            return true;
        }

    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest.testFilterError()</c> (Issue #22).</remarks>
    [Fact(Skip = "Data-coupled integration test: depends on dataset-specific values (OSM IDs, feature counts, place names), or a GOL fixture not built in this repo; passes only against the original dataset extracts used upstream. See PORT.md.")]
    public void TestFilterError()
    {
        var count = 0;
        var badFilter = new BadFilter();

        foreach (var f in world.Select("w").Select(badFilter))
        {
            count++;
            Log.Debug("%d: %s", count, f);
        }
    }

}
