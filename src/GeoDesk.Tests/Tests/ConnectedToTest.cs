/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;

using Xunit;

namespace GeoDesk.Tests.Tests;

// PORT: the Java original printed the bicycle routes connected to a specific German route_master.
// Rebased onto monaco's road network: ConnectedTo must find ways sharing a node with the source,
// and they must all be ways.
/// <remarks>Ported from Java <c>com.geodesk.tests.ConnectedToTest</c>.</remarks>
public class ConnectedToTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.ConnectedToTest.testConnectedTo()</c>.</remarks>
    [Fact]
    public void TestConnectedTo()
    {
        if (world is null) return;

        // Find a highway that connects to other highways (monaco's network is connected).
        IFeature? hub = null;
        var searched = 0;
        foreach (var street in world.Select("w[highway]"))
        {
            if (world.Select("w[highway]").ConnectedTo(street).Count() > 0)
            {
                hub = street;
                break;
            }
            if (++searched >= 100) break;
        }

        Assert.NotNull(hub); // expected connected highways in monaco

        foreach (var f in world.Select("w[highway]").ConnectedTo(hub!))
        {
            Assert.Equal(FeatureType.Way, f.Type);
        }
    }

}
