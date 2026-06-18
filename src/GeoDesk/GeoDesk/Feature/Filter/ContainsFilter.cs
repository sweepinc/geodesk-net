/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Clarisma.Common.Util;
using GeoDesk.Feature;
using GeoDesk.Feature.Match;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A Filter that only accepts features whose geometry contains the test geometry.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter</c>.</remarks>
internal class ContainsFilter : Filter
{

    readonly Geometry _testGeom;
    readonly Box _bounds;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter(Feature)</c>.</remarks>
    public ContainsFilter(Feature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter(Geometry)</c>.</remarks>
    public ContainsFilter(Geometry geom)
    {
        _testGeom = geom;
        _bounds = Box.FromEnvelope(geom.EnvelopeInternal);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter(PreparedGeometry)</c>.</remarks>
    public ContainsFilter(IPreparedGeometry prepared)
        : this(prepared.Geometry)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.strategy()</c>.</remarks>
    public int Strategy()
    {
        return FilterStrategy.UsesBbox | FilterStrategy.RestrictsTypes;
    }

    // TODO: needs acceptedTypes() ???

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.containedBy(Geometry)</c>.</remarks>
    bool ContainedBy(Geometry g)
    {
        return g.Contains(_testGeom);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.accept(Feature, Geometry)</c>.</remarks>
    public bool Accept(Feature feature, Geometry geom)
    {
        Box featureBounds = feature.Bounds();
        if (!featureBounds.Contains(_bounds)) return false;
        if (geom == null) geom = feature.ToGeometry();

        // TODO: for non-area relations, pre-check dimension of member (e.g. lineal way can't contain
        //  area, no need to do full test)

        try
        {
            for (var i = 0; i < geom.NumGeometries; i++)
            {
                var g = geom.GetGeometryN(i);
                if (!ContainedBy(g)) return false;
            }
        }
        catch (Exception ex)
        {
            Log.Debug("Exception (%s) while checking containedBy(%s)", ex.Message, feature);
            throw new QueryException("Query failed due to topology problem");
        }
        return true;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.bounds()</c>.</remarks>
    public Bounds Bounds()
    {
        return _bounds;
    }

}
