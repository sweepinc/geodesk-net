/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Geom;

using Xunit;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.PolyformQueryTest</c>.</remarks>
public class PolyformQueryTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.PolyformQueryTest.testPolyfomQueries()</c>.</remarks>
    [Fact(Skip = "Polyform queries are not supported (faithful to geodesk 2.0.0: MatcherCompiler throws QueryException).")]
    public void TestPolyfomQueries()
    {
        var world = new FeatureLibrary(TestSettings.Resolve("de.gol"));
        try
        {
            foreach (var f in world
                .Select("na[amenity=fire_station],n[emergency=fire_hydrant]")
                .In(Box.OfWorld()))
            {
                Log.Debug("%s: ", f);
                Log.Debug("%s", f.Tags.ToString());
            }
        }
        finally
        {
            world.Close();
        }
    }

}
