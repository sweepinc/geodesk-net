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

/// <summary>
/// A filter that rejects every feature. Exposed as a shared singleton, it is used as the result of a
/// query that can never match anything.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.FalseFilter</c>.</remarks>
internal class FalseFilter : IFilter
{

    public static readonly IFilter Instance = new FalseFilter();
        // TODO: move to Filters

    /// <summary>
    /// Always returns false, rejecting the given feature.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.FalseFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature f)
    {
        return false;
    }

}
