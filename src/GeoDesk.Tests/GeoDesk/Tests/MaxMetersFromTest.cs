/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using GeoDesk.Feature;
using GeoDesk.Util;
using Xunit;

namespace GeoDesk.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.MaxMetersFromTest</c>.</remarks>
public class MaxMetersFromTest : IDisposable
{

    readonly IFeatures features;
    readonly FeatureLibrary _lib;

    public MaxMetersFromTest()
    {
        _lib = new FeatureLibrary(TestSettings.GolFile());
        features = _lib;
    }

    public void Dispose() => _lib.Close();

    /// <remarks>Ported from Java <c>com.geodesk.tests.MaxMetersFromTest.testFromPoint()</c>.</remarks>
    [Fact]
    public void TestFromPoint()
    {
        var map = new MapMaker();
        map.Add(features.Select("a[building]").MaxMetersFromLonLat(500, 11.078, 49.454));
        map.Save(Path.Combine(TestSettings.OutputPath(), "max-meters.html"));
    }

}
