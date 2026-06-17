/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;

// PORT: the Java package is com.geodesk.feature.filter, but the C# type GeoDesk.Feature.Filter
// (the interface) would collide with a same-named namespace, which C# forbids. The filter package
// is therefore namespaced as Filters (plural).
namespace GeoDesk.Feature.Filters;

/// <remarks>Ported from Java <c>com.geodesk.feature.filter.FalseFilter</c>.</remarks>
public class FalseFilter : Filter
{

    public static readonly Filter Instance = new FalseFilter();
        // TODO: move to Filters

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.FalseFilter.accept(Feature)</c>.</remarks>
    public bool Accept(Feature f)
    {
        return false;
    }

}
