/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;

namespace GeoDesk.Benchmarks;

/// <summary>
/// Resolves the GOL file the benchmarks run against — a country-scale dataset (Germany). Resolution order:
/// <list type="number">
///   <item>the <c>GEODESK_GOL</c> environment variable, if set (an explicit path to any GOL);</item>
///   <item>otherwise, the <c>germany.gol</c> fixture the project's build generates on demand into the
///   output <c>Fixtures\</c> directory (next to the executing assembly).</item>
/// </list>
/// Throws with build instructions if neither is found.
/// </summary>
internal static class GolFixture
{

    /// <summary>The fixture the build generates on demand (see the GOL fixture targets in the .csproj).</summary>
    const string DefaultFixture = "germany.gol";

    /// <summary>Returns an absolute path to a GOL file to benchmark against, or throws if none is found.</summary>
    public static string Resolve()
    {
        var env = Environment.GetEnvironmentVariable("GEODESK_GOL");
        if (!string.IsNullOrEmpty(env))
        {
            if (!File.Exists(env))
                throw new FileNotFoundException($"GEODESK_GOL is set to '{env}', but no file exists there.", env);

            return Path.GetFullPath(env);
        }

        // The build copies the generated fixture to <output>\Fixtures\, the same convention the test
        // project uses (TestSettings.FixturesDir).
        var fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", DefaultFixture);
        if (File.Exists(fixture))
            return fixture;

        throw new FileNotFoundException(
            $"Could not locate '{DefaultFixture}' (looked in '{Path.GetDirectoryName(fixture)}'). The build " +
            "generates it on demand from a Geofabrik extract, which requires the GOL Tool 2.0 " +
            "(https://github.com/clarisma/geodesk-gol) on PATH and downloads a multi-GB .pbf. Build the " +
            "project (e.g. `dotnet build src/GeoDesk.Benchmarks -c Release`), or set the GEODESK_GOL " +
            "environment variable to an existing .gol path.",
            fixture);
    }

}
