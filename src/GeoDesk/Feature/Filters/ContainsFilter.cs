/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Common.Util;
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
internal class ContainsFilter : IFilter
{

    readonly Geometry _testGeom;
    readonly Box _bounds;

    /// <summary>Creates a filter using the given feature's geometry as the test geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter(Feature)</c>.</remarks>
    public ContainsFilter(IFeature feature)
        : this(feature.ToGeometry())
    {
    }

    /// <summary>Creates a filter that accepts features whose geometry contains the given test geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter(Geometry)</c>.</remarks>
    public ContainsFilter(Geometry geom)
    {
        _testGeom = geom;
        _bounds = Box.FromEnvelope(geom.EnvelopeInternal);
    }

    /// <summary>Creates a filter from a prepared geometry, using its underlying geometry as the test.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter(PreparedGeometry)</c>.</remarks>
    public ContainsFilter(IPreparedGeometry prepared)
        : this(prepared.Geometry)
    {
    }

    /// <summary>The filter strategy flags: uses a bounding box and restricts types.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.strategy()</c>.</remarks>
    public int Strategy => FilterStrategy.UsesBbox | FilterStrategy.RestrictsTypes;

    // TODO: needs acceptedTypes() ???

    /// <summary>Returns true if the given geometry contains the test geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.containedBy(Geometry)</c>.</remarks>
    bool ContainedBy(Geometry g)
    {
        return g.Contains(_testGeom);
    }

    /// <summary>
    /// Returns true if every component of the feature's geometry contains the test
    /// geometry, after a quick bounding-box pre-check.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.accept(Feature, Geometry)</c>.</remarks>
    public bool Accept(IFeature feature, Geometry geom)
    {
        Box featureBounds = feature.Bounds;
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

    /// <summary>The bounding box of the test geometry.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ContainsFilter.bounds()</c>.</remarks>
    public IBounds Bounds => _bounds;

}
