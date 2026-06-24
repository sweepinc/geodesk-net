/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Reflection;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace GeoDesk.Benchmarks;

/// <summary>
/// Entry point. Hands all benchmark classes in this assembly to the BenchmarkDotNet switcher, which reads
/// the command line (e.g. <c>--filter *</c> to run everything, or <c>--filter *pubs-name-bbox-urban-s*</c>
/// for one case). Run in Release: <c>dotnet run -c Release --project src/GeoDesk.Benchmarks -- --filter *</c>.
/// </summary>
/// <remarks>
/// The suite is pinned to BenchmarkDotNet's <see cref="InProcessEmitToolchain"/>: benchmarks run in this
/// process rather than via BDN's default toolchain, which generates and builds a throwaway project per
/// benchmark. In-process keeps it a plain executable — no project manipulation — which also avoids the
/// per-benchmark rebuild re-triggering this project's on-demand GOL fixture generation.
/// </remarks>
public static class Program
{
    public static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

        BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args, config);
    }
}
