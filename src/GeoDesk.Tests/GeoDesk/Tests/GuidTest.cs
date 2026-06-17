/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using Clarisma.Common.Util;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.GuidTest</c> (java.util.UUID → System.Guid).</remarks>
public class GuidTest
{

    /// <remarks>Ported from Java <c>com.geodesk.tests.GuidTest.testGuid()</c>.</remarks>
    [Fact]
    public void TestGuid()
    {
        var guid = Guid.NewGuid();
        // PORT: .NET Guid does not expose version()/variant()/64-bit-halves the way java.util.UUID
        // does (and its byte layout differs); the version nibble is read from the canonical form.
        var version = Convert.ToInt32(guid.ToString("N")[12].ToString(), 16);
        Log.Debug("version = %d", version);
        Log.Debug("guid = %s", guid.ToString());
        for (var i = 0; i < 20; i++) Log.Debug("%s", Guid.NewGuid().ToString());
    }

}
