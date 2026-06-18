/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using GeoDesk.Feature;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Filters;

/// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter</c>.</remarks>
internal class ConnectedFilter : IFilter
{

    IFeature? _self;
    readonly HashSet<long> _points = new HashSet<long>();
    IBounds _bounds;

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter(Feature)</c>.</remarks>
    public ConnectedFilter(IFeature f)
    {
        _self = f;
        CollectPoints(f);
        _bounds = f.Bounds;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter(Geometry)</c>.</remarks>
    public ConnectedFilter(Geometry geom)
    {
        var bbox = new Box();
        geom.Apply(new PointCollector(_points, bbox));
        _bounds = bbox;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter.bounds()</c>.</remarks>
    public IBounds Bounds()
    {
        return _bounds;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter.collectPoints(Feature)</c>.</remarks>
    void CollectPoints(IFeature f)
    {
        if (f is IWay)
        {
            // TODO: accept other implementations
            var way = (StoredWay)f;
            var iter = way.IterXY(0);
            while (iter.HasNext())
            {
                _points.Add(iter.NextXY());
            }
        }
        else if (f is IRelation rel)
        {
            foreach (var member in rel) CollectPoints(member);
        }
        else
        {
            _points.Add(XY.Of(f.X, f.Y));
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        if (_self != null && _self.Equals(feature)) return false;
        if (feature is IWay)
        {
            // TODO: accept other implementations
            var way = (StoredWay)feature;
            var iter = way.IterXY(0);
            while (iter.HasNext())
            {
                if (_points.Contains(iter.NextXY())) return true;
            }
            return false;
        }
        if (feature is IRelation rel)
        {
            foreach (var member in rel)
            {
                if (Accept(member)) return true;
            }
            return false;
        }
        return _points.Contains(XY.Of(feature.X, feature.Y));
    }

    // PORT: Java uses an anonymous CoordinateSequenceFilter; in .NET this is a named nested class
    // implementing ICoordinateSequenceFilter that collects vertices into the shared point set/bbox.
    class PointCollector : ICoordinateSequenceFilter
    {

        readonly HashSet<long> _points;
        readonly Box _bbox;

        public PointCollector(HashSet<long> points, Box bbox)
        {
            _points = points;
            _bbox = bbox;
        }

        public void Filter(CoordinateSequence seq, int i)
        {
            var x = (int)Math.Floor(seq.GetX(i) + 0.5);
            var y = (int)Math.Floor(seq.GetY(i) + 0.5);
            _points.Add(XY.Of(x, y));
            _bbox.ExpandToInclude(x, y);
        }

        public bool Done => false;

        public bool GeometryChanged => false;

    }

}
