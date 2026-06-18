/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using GeoDesk.Feature;

namespace GeoDesk.Tests;

// PORT: JUnit @Before/@After become the xUnit constructor / IDisposable. The library is exposed as
// Features (interface) so the default-interface query/filter methods are visible (C# does not
// surface default-interface members on the concrete FeatureLibrary type). As in the Java original,
// the library is opened unconditionally; a missing GOL fixture surfaces as a constructor failure.
/// <remarks>Ported from Java <c>com.geodesk.tests.AbstractFeatureTest</c>.</remarks>
public abstract class AbstractFeatureTest : IDisposable
{

    protected readonly IFeatures world;
    readonly FeatureLibrary _lib;

    protected AbstractFeatureTest()
    {
        _lib = new FeatureLibrary(TestSettings.GolFile());
        world = _lib;
    }

    public virtual void Dispose()
    {
        _lib.Close();
    }

}
