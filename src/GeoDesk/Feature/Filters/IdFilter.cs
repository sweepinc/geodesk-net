/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A filter that accepts only the feature with a specific id, restricted to a given set of feature
/// types.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter</c>.</remarks>
internal class IdFilter : IFilter
{

    protected readonly int types;
    protected readonly long id;

    /// <summary>
    /// Creates a filter accepting only the feature with the given id among the given feature types.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter(int, long)</c>.</remarks>
    public IdFilter(int types, long id)
    {
        this.types = types;
        this.id = id;
    }

    /// <summary>
    /// The strategy flags: this filter restricts the set of feature types it can match.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter.strategy()</c>.</remarks>
    public int Strategy => FilterStrategy.RestrictsTypes;

    /// <summary>
    /// The bitmask of feature types this filter can accept.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter.acceptedTypes()</c>.</remarks>
    public int AcceptedTypes => types;

    /// <summary>
    /// Accepts a feature only if its id matches the target id.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        return feature.Id == id;
    }

}
