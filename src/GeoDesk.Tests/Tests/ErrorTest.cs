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

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest</c>.</remarks>
public class ErrorTest : AbstractFeatureTest
{

    /// <summary>Filter that throws an exception after accepting 10 features.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest.BadFilter</c>.</remarks>
    class BadFilter : IFilter
    {

        int _count;

        /// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest.BadFilter.accept(Feature)</c>.</remarks>
        public bool Accept(IFeature feature)
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

    // PORT: Issue #22 — a filter that throws mid-query must surface its error to the caller (not
    // hang or swallow it). Monaco has far more than 10 ways, so BadFilter throws; we assert the
    // exception propagates out of the query. This also exercises the engine's error path (the
    // background scan's fault is surfaced through to the consuming enumerator).
    /// <remarks>Ported from Java <c>com.geodesk.tests.ErrorTest.testFilterError()</c> (Issue #22).</remarks>
    [Fact]
    public void TestFilterError()
    {
        if (world is null) return;

        var badFilter = new BadFilter();
        var ex = Assert.ThrowsAny<Exception>(() =>
        {
            foreach (var f in world.Select("w").Select(badFilter))
            {
                // drain the query; BadFilter throws once it has accepted more than 10 features
            }
        });
        Assert.Contains("BadFilter", ex.ToString());
    }

}
