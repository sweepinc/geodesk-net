/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Common.Util;
using GeoDesk.Feature;

using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.FilterTest</c>.</remarks>
public class FilterTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.FilterTest.testConnectedStreets()</c>.</remarks>
    [Fact]
    public void TestConnectedStreets()
    {
        var world = FeatureLibrary.Open(TestSettings.GolFile());
        try
        {
            var streets = world.Ways("w[highway]");
            foreach (var street in streets)
            {
                Log.Debug("%s %s %s connects to:", street,
                    street.StringValue("highway"), street.StringValue("name"));
                foreach (var connected in streets.ConnectedTo(street))
                {
                    Log.Debug("- %s %s %s", connected,
                        connected.StringValue("highway"), connected.StringValue("name"));
                }
            }
        }
        finally
        {
            ((IDisposable)world).Dispose();
        }
    }

}
