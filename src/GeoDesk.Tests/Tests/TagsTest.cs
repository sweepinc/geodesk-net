/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Tests.Benchmark;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.TagsTest</c>.</remarks>
public class TagsTest : IDisposable
{

    readonly IFeatureQuery world;
    readonly FeatureLibrary _lib;

    public TagsTest()
    {
        _lib = new FeatureLibrary(TestSettings.GolFile());
        world = _lib;
    }

    public void Dispose() => _lib.Close();

    /// <remarks>Ported from Java <c>com.geodesk.tests.TagsTest.testTagsPerformance()</c>.</remarks>
    [Fact]
    public void TestTagsPerformance()
    {
        var streets = world.Ways("w[highway]").ToList();

        long count2 = 0;
        SimpleBenchmark.Run("taglookup", 10, () =>
        {
            foreach (var street in streets)
            {
                var crossings = 0;
                foreach (var node in street)
                {
                    if (node.HasTag("highway", "crossing") &&
                        (node.HasTag("crossing", "marked") || node.HasTag("crossing", "zebra")))
                    {
                        crossings++;
                    }
                }
                count2 += crossings;
            }
        });

        long count = 0;
        SimpleBenchmark.Run("select", 10, () =>
        {
            foreach (var street in streets)
            {
                count += street.Nodes("[highway=crossing][crossing=marked,zebra]").Count();
            }
        });

        long count3 = 0;
        SimpleBenchmark.Run("taglookup", 10, () =>
        {
            foreach (var street in streets)
            {
                var crossings = 0;
                foreach (var node in street)
                {
                    if (node.HasTag("highway", "crossing"))
                    {
                        var crossing = node.StringValue("crossing");
                        if (crossing == "marked" || crossing == "zebra")
                        {
                            crossings++;
                        }
                    }
                }
                count3 += crossings;
            }
        });

        Log.Debug("%d = %d = %d crossings", count, count2, count3);
    }

}
