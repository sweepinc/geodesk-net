/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Query;
using GeoDesk.Geom;

using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A Node without tags that doesn't belong to a Relation, i.e. one that isn't a proper feature,
/// but merely defines the geometry of a Way.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode</c>.</remarks>
internal class AnonymousWayNode : INode
{

    readonly FeatureStore _store;
    readonly int _x;
    readonly int _y;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode(FeatureStore, int, int)</c>.</remarks>
    public AnonymousWayNode(FeatureStore store, int x, int y)
    {
        _store = store;
        _x = x;
        _y = y;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.iterator()</c>.</remarks>
    public IEnumerator<IFeature> GetEnumerator()
    {
        return Enumerable.Empty<IFeature>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.id()</c>.</remarks>
    public long Id => 0;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.type()</c>.</remarks>
    public FeatureType Type => FeatureType.Node;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.isNode()</c>.</remarks>
    public bool IsNode => true;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.x()</c>.</remarks>
    public int X => _x;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.y()</c>.</remarks>
    public int Y => _y;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.toXY()</c>.</remarks>
    public int[] ToXY()
    {
        var coords = new int[2];
        coords[0] = X;
        coords[1] = Y;
        return coords;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.bounds()</c>.</remarks>
    public Box Bounds => new Box(_x, _y);

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.tags()</c>.</remarks>
    public TagCollection Tags => default;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.tag(String)</c>.</remarks>
    public string Tag(string k)
    {
        return "";
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.hasTag(String)</c>.</remarks>
    public bool HasTag(string k)
    {
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.hasTag(String, String)</c>.</remarks>
    public bool HasTag(string k, string v)
    {
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.belongsTo(Feature)</c>.</remarks>
    public bool BelongsTo(IFeature parent)
    {
        if (parent is StoredWay way)     // TODO: other possible types?
        {
            var xy = XY.Of(_x, _y);
            var iter = way.IterXY(0);
            while (iter.HasNext())
            {
                if (iter.NextXY() == xy)
                    return true;
            }
        }
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.role()</c>.</remarks>
    public string? Role => null;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.stringValue(String)</c>.</remarks>
    public string StringValue(string key)
    {
        return "";
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.intValue(String)</c>.</remarks>
    public int IntValue(string key)
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.longValue(String)</c>.</remarks>
    public long LongValue(string key)
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.doubleValue(String)</c>.</remarks>
    public double DoubleValue(string key)
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.booleanValue(String)</c>.</remarks>
    public bool BooleanValue(string key)
    {
        return false;
    }

    /// <summary>
    /// Always returns <c>false</c>, because this type of Node by definition does not belong to any
    /// relation.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.belongsToRelation()</c>.</remarks>
    public bool BelongsToRelation => false;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.isArea()</c>.</remarks>
    public bool IsArea => false;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.toGeometry()</c>.</remarks>
    public Geometry ToGeometry()
    {
        return _store.GeometryFactory().CreatePoint(new Coordinate(X, Y));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.equals(Object)</c>.</remarks>
    public override bool Equals(object? other)
    {
        if (!(other is INode otherNode))
            return false;
        return otherNode.Id == 0 && otherNode.X == _x && otherNode.Y == _y;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.hashCode()</c>.</remarks>
    public override int GetHashCode()
    {
        return _x ^ _y;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.toString()</c>.</remarks>
    public override string ToString()
    {
        return "node@" + _x + "," + _y;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.parents(int, Matcher, Filter)</c>.</remarks>
    public IFeatureQuery Parents(int types, Matcher matcher, IFilter? filter)
    {
        if ((types & TypeBits.WAYS) == 0)
            return EmptyView.Any;
        IFilter newFilter = new ParentWayFilter(_x, _y);
        if (filter != null)
            newFilter = AndFilter.Create(newFilter, filter);
        return new WorldView(_store, types, Bounds, matcher, newFilter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.parents()</c>.</remarks>
    public IFeatureQuery Parents()
    {
        return new WorldView(_store, TypeBits.WAYS, Bounds, Matcher.ALL, new ParentWayFilter(_x, _y));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.parents(String)</c>.</remarks>
    public IFeatureQuery Parents(string query)
    {
        var matcher = _store.GetMatcher(query);
        if ((matcher.AcceptedTypes & TypeBits.WAYS) == 0)
            return EmptyView.Any;
        return new WorldView(_store, matcher.AcceptedTypes, Bounds, matcher, new ParentWayFilter(_x, _y));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.ParentWayFilter</c>.</remarks>
    class ParentWayFilter : IFilter
    {

        long _xy;

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.ParentWayFilter(int, int)</c>.</remarks>
        public ParentWayFilter(int x, int y)
        {
            _xy = XY.Of(x, y);
        }

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.AnonymousWayNode.ParentWayFilter.accept(Feature)</c>.</remarks>
        public bool Accept(IFeature feature)
        {
            var way = (StoredWay)feature;
            var iter = way.IterXY(0);
            while (iter.HasNext())
            {
                if (iter.NextXY() == _xy)
                    return true;
            }
            return false;
        }

    }

}
