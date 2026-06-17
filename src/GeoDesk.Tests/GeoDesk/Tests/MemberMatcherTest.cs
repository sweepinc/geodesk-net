/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Clarisma.Common.Util;
using GeoDesk.Feature;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.MemberMatcherTest</c>.</remarks>
[Collection("GolFixture")]
public class MemberMatcherTest : AbstractFeatureTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.MemberMatcherTest.testRoleQuery()</c>.</remarks>
    [Fact]
    public void TestRoleQuery()
    {
var rivers = world.Relations("r[waterway=river]");
        foreach (var river in rivers)
        {
            foreach (var m in river.Members())
            {
                Log.Debug("- %s (name: %s, role: %s)", m, m.StringValue("name"), m.Role());
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.MemberMatcherTest.testMemberQuery()</c>.</remarks>
    [Fact]
    public void TestMemberQuery()
    {
var rivers = world.Relations("r[waterway=river]");
        foreach (var river in rivers)
        {
            foreach (var m in river.Members("n[!natural], w"))
            {
                Log.Debug("- %s (name: %s, role: %s)", m, m.StringValue("name"), m.Role());
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.MemberMatcherTest.testTypedMemberQuery()</c>.</remarks>
    [Fact]
    public void TestTypedMemberQuery()
    {
var rivers = world.Relations();
        foreach (var river in rivers)
        {
            foreach (var m in river.Members("w"))
            {
                Assert.True(m is Way);
            }
        }
    }

}
