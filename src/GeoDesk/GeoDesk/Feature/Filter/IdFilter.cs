/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;

namespace GeoDesk.Feature.Filters;

/// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter</c>.</remarks>
internal class IdFilter : Filter
{

    protected readonly int types;
    protected readonly long id;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter(int, long)</c>.</remarks>
    public IdFilter(int types, long id)
    {
        this.types = types;
        this.id = id;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter.strategy()</c>.</remarks>
    public int Strategy()
    {
        return FilterStrategy.RestrictsTypes;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter.acceptedTypes()</c>.</remarks>
    public int AcceptedTypes()
    {
        return types;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.IdFilter.accept(Feature)</c>.</remarks>
    public bool Accept(Feature feature)
    {
        return feature.Id() == id;
    }

}
