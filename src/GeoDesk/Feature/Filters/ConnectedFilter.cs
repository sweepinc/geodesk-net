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

/// <summary>
/// A filter that accepts features sharing at least one vertex (an exact coordinate) with a reference
/// feature or geometry — that is, features topologically connected to it. The reference's vertices
/// are collected up front into a point set and a bounding box.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter</c>.</remarks>
internal class ConnectedFilter : IFilter
{

    IFeature? _self;
    readonly HashSet<long> _points = new HashSet<long>();
    IBounds _bounds;

    /// <summary>
    /// Creates a filter connected to the given feature, collecting its vertices and bounds (and
    /// excluding the feature itself from matches).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter(Feature)</c>.</remarks>
    public ConnectedFilter(IFeature f)
    {
        _self = f;
        CollectPoints(f);
        _bounds = f.Bounds;
    }

    /// <summary>
    /// Creates a filter connected to the given geometry, collecting its vertices and bounding box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter(Geometry)</c>.</remarks>
    public ConnectedFilter(Geometry geom)
    {
        var bbox = new Box();
        geom.Apply(new PointCollector(_points, bbox));
        _bounds = bbox;
    }

    /// <summary>
    /// The bounding box of the reference feature or geometry, used to pre-filter candidates.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.ConnectedFilter.bounds()</c>.</remarks>
    public IBounds Bounds => _bounds;

    /// <summary>
    /// Recursively gathers the vertices of a feature (way nodes, relation members, or a node's own
    /// coordinate) into the connected-point set.
    /// </summary>
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

    /// <summary>
    /// Accepts a feature if it shares any vertex with the reference (recursing into relation members
    /// and way nodes), excluding the reference feature itself.
    /// </summary>
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
    /// <summary>
    /// A coordinate-sequence visitor that rounds each visited vertex to integer coordinates and adds
    /// it to the shared connected-point set while expanding the shared bounding box.
    /// </summary>
    class PointCollector : ICoordinateSequenceFilter
    {

        readonly HashSet<long> _points;
        readonly Box _bbox;

        /// <summary>
        /// Creates a collector that accumulates vertices into the given point set and bounding box.
        /// </summary>
        public PointCollector(HashSet<long> points, Box bbox)
        {
            _points = points;
            _bbox = bbox;
        }

        /// <summary>
        /// Rounds the vertex at index <paramref name="i"/> to integer coordinates, adds it to the
        /// point set, and expands the bounding box to include it.
        /// </summary>
        public void Filter(CoordinateSequence seq, int i)
        {
            var x = (int)Math.Floor(seq.GetX(i) + 0.5);
            var y = (int)Math.Floor(seq.GetY(i) + 0.5);
            _points.Add(XY.Of(x, y));
            _bbox.ExpandToInclude(x, y);
        }

        /// <summary>
        /// Always false; every vertex of the sequence is visited.
        /// </summary>
        public bool Done => false;

        /// <summary>
        /// Always false; this visitor only reads coordinates and never mutates the geometry.
        /// </summary>
        public bool GeometryChanged => false;

    }

}
