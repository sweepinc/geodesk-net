/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Diagnostics;

using GeoDesk.Common.Util;

namespace GeoDesk.Tests.Benchmark;

// PORT: minimal stand-in for com.geodesk.benchmark.SimpleBenchmark / Benchmark. The full benchmark
// harness is not ported; this runs the target the requested number of times and logs the timing,
// which is all the (assertion-free) perf tests in geodesk-tests require.
/// <remarks>Ported from Java <c>com.geodesk.benchmark.SimpleBenchmark</c>.</remarks>
public static class SimpleBenchmark
{

    /// <remarks>Ported from Java <c>com.geodesk.benchmark.SimpleBenchmark.run(String, int, Runnable)</c>.</remarks>
    public static void Run(string name, int runs, Action target)
    {
        for (var i = 0; i < runs; i++)
        {
            var start = Stopwatch.GetTimestamp();
            target();
            var elapsed = Stopwatch.GetElapsedTime(start);
            Log.Debug("%s: run %d in %d ms", name, i, (long)elapsed.TotalMilliseconds);
        }
    }

}
