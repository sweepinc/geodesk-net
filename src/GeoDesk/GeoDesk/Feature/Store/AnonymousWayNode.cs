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
/// A Node without tags that doesn't belong to a Relation, i.e. one that isn't a proper
/// feature, but merely defines the geometry of a Way.
/// </summary>
public class AnonymousWayNode : Node
{
    private readonly FeatureStore store;
    private readonly int x;
    private readonly int y;

    public AnonymousWayNode(FeatureStore store, int x, int y)
    {
        this.store = store;
        this.x = x;
        this.y = y;
    }

    public IEnumerator<Feature> GetEnumerator()
    {
        return Enumerable.Empty<Feature>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public long Id()
    {
        return 0;
    }

    public FeatureType Type()
    {
        return FeatureType.Node;
    }

    public bool IsNode()
    {
        return true;
    }

    public int X()
    {
        return x;
    }

    public int Y()
    {
        return y;
    }

    public int[] ToXY()
    {
        int[] coords = new int[2];
        coords[0] = X();
        coords[1] = Y();
        return coords;
    }

    public Box Bounds()
    {
        return new Box(x, y);
    }

    public Tags Tags()
    {
        return EmptyTags.SINGLETON;
    }

    public string Tag(string k)
    {
        return "";
    }

    public bool HasTag(string k)
    {
        return false;
    }

    public bool HasTag(string k, string v)
    {
        return false;
    }

    public bool BelongsTo(Feature parent)
    {
        if (parent is StoredWay way)     // TODO: other possible types?
        {
            long xy = XY.Of(x, y);
            StoredWay.XYIterator iter = way.IterXY(0);
            while (iter.HasNext())
            {
                if (iter.NextXY() == xy) return true;
            }
        }
        return false;
    }

    public string? Role()
    {
        return null;
    }

    public string StringValue(string key)
    {
        return "";
    }

    public int IntValue(string key)
    {
        return 0;
    }

    public long LongValue(string key)
    {
        return 0;
    }

    public double DoubleValue(string key)
    {
        return 0;
    }

    public bool BooleanValue(string key)
    {
        return false;
    }

    /// <summary>
    /// Always returns <c>false</c>, because this type of Node by definition does not
    /// belong to any relation.
    /// </summary>
    public bool BelongsToRelation()
    {
        return false;
    }

    public bool IsArea()
    {
        return false;
    }

    public Geometry ToGeometry()
    {
        return store.GeometryFactory().CreatePoint(new Coordinate(X(), Y()));
    }

    public override bool Equals(object? other)
    {
        if (!(other is Node otherNode)) return false;
        return otherNode.Id() == 0 && otherNode.X() == x && otherNode.Y() == y;
    }

    public override int GetHashCode()
    {
        return x ^ y;
    }

    public override string ToString()
    {
        return "node@" + x + "," + y;
    }

    public Features Parents(int types, Matcher matcher, Filter? filter)
    {
        if ((types & TypeBits.WAYS) == 0) return EmptyView.ANY;
        Filter newFilter = new ParentWayFilter(x, y);
        if (filter != null)
        {
            newFilter = AndFilter.Create(newFilter, filter);
        }
        return new WorldView(store, types, Bounds(), matcher, newFilter);
    }

    public Features Parents()
    {
        return new WorldView(store, TypeBits.WAYS,
            Bounds(), Matcher.ALL, new ParentWayFilter(x, y));
    }

    public Features Parents(string query)
    {
        Matcher matcher = store.GetMatcher(query);
        if ((matcher.AcceptedTypes() & TypeBits.WAYS) == 0) return EmptyView.ANY;
        return new WorldView(store, matcher.AcceptedTypes(),
            Bounds(), matcher, new ParentWayFilter(x, y));
    }

    private class ParentWayFilter : Filter
    {
        private long xy;

        public ParentWayFilter(int x, int y)
        {
            this.xy = XY.Of(x, y);
        }

        public bool Accept(Feature feature)
        {
            StoredWay way = (StoredWay)feature;
            StoredWay.XYIterator iter = way.IterXY(0);
            while (iter.HasNext())
            {
                if (iter.NextXY() == xy) return true;
            }
            return false;
        }
    }
}
