/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.View</c>.</remarks>
public abstract class View : Features
{

    internal readonly FeatureStore store;
    internal readonly int types;
    internal readonly Matcher matcher;
    internal readonly Filter? filter;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View(FeatureStore, int, Matcher, Filter)</c>.</remarks>
    internal View(FeatureStore store, int types, Matcher matcher, Filter? filter)
    {
        this.store = store;
        this.types = types;
        this.matcher = matcher;
        this.filter = filter;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.newWith(int, Matcher, Filter)</c>.</remarks>
    internal abstract Features NewWith(int types, Matcher matcher, Filter? filter);

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.types()</c>.</remarks>
    public int TypesValue()
    {
        return types;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(int)</c>.</remarks>
    protected Features Select(int newTypes)
    {
        newTypes &= types;
        if (newTypes == 0) return EmptyView.Any;
        return NewWith(newTypes, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(int, String)</c>.</remarks>
    protected virtual Features Select(int newTypes, string query)
    {
        var newMatcher = store.GetMatcher(query);
        if (matcher != Matcher.ALL) newMatcher = new AndMatcher(matcher, newMatcher);
        newTypes &= types & newMatcher.AcceptedTypes();
        if (newTypes == 0) return EmptyView.Any;
        return NewWith(newTypes, newMatcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(String)</c>.</remarks>
    public virtual Features Select(string query)
    {
        return Select(TypeBits.ALL, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodes()</c>.</remarks>
    public Features Nodes()
    {
        return Select(TypeBits.NODES);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodes(String)</c>.</remarks>
    public Features Nodes(string query)
    {
        return Select(TypeBits.NODES, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.ways()</c>.</remarks>
    public Features Ways()
    {
        return Select(TypeBits.WAYS);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.ways(String)</c>.</remarks>
    public Features Ways(string query)
    {
        return Select(TypeBits.WAYS, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.relations()</c>.</remarks>
    public Features Relations()
    {
        return Select(TypeBits.RELATIONS);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.relations(String)</c>.</remarks>
    public Features Relations(string query)
    {
        return Select(TypeBits.RELATIONS, query);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(Filter)</c>.</remarks>
    public virtual Features Select(Filter filter)
    {
        var newTypes = types;
        if (this.filter != null)
        {
            filter = AndFilter.Create(this.filter, filter);
            if (filter == FalseFilter.Instance) return EmptyView.Any;
        }
        var strategy = filter.Strategy();
        if ((strategy & FilterStrategy.RestrictsTypes) != 0)
        {
            newTypes &= filter.AcceptedTypes();
            if (newTypes == 0) return EmptyView.Any;
        }
        return NewWith(newTypes, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.parentsOf(Feature)</c>.</remarks>
    public virtual Features ParentsOf(Feature child)
    {
        if (child.IsNode())
        {
            if (child is AnonymousWayNode wayNode)
            {
                // An anonymous node *always* has at least one parent way, and *never* has parent
                // relations
                return wayNode.Parents(types, matcher, filter);
            }
            return ((StoredNode)child).Parents(types, matcher, filter);
        }
        else
        {
            // Ways and relations can only have relations as parents
            if ((types & TypeBits.RELATIONS) == 0) return EmptyView.Any;
            var f = (StoredFeature)child;
            if (!f.BelongsToRelation()) return EmptyView.Any;
            return new ParentRelationView(store, f.Buffer(), f.GetRelationTablePtr(), types, matcher, filter);
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.membersOf(Feature)</c>.</remarks>
    public virtual Features MembersOf(Feature parent)
    {
        if (parent.IsRelation()) return ((StoredRelation)parent).Members(types, matcher, filter);
        // TODO: membersOf() for ways
        return EmptyView.Any;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.nodesOf(Feature)</c>.</remarks>
    public virtual Features NodesOf(Feature parent)
    {
        if ((types & TypeBits.NODES) == 0) return EmptyView.Any;
        if (parent.IsWay())
        {
            var way = (StoredWay)parent;
            if (matcher != Matcher.ALL && (way.Flags() & IFeatureFlags.WAYNODE_FLAG) == 0)
            {
                // GOQL queries only return feature nodes; if the Way's waynode_flag is cleared, it
                // only contains anonymous nodes, so we return an empty set
                return EmptyView.Any;
            }
            return new WayNodeView(store, way.Buffer(), way.Pointer(), types, matcher, filter);
        }

        // TODO: nodesOf() for relations

        return EmptyView.Any;
    }

    // PORT: Java's View does not declare in(); it is abstract here so View can satisfy the
    // Features.in(Bounds) contract, with each concrete view providing the body.
    /// <remarks>Implements Java <c>com.geodesk.feature.Features.in(Bounds)</c> (abstract in this base).</remarks>
    public abstract Features In(Bounds bbox);

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.View.select(Features)</c>.</remarks>
    public Features Select(Features otherFeatures)
    {
        // TODO: This assumes both views are WorldViews (which is wrong); at least one must be a
        //  WorldView
        // TODO: Throw exception if the Views have different stores

        var other = (View)otherFeatures;
            // TODO: For now, all Features are implemented as a View, but this may change in the
            //  future
        var newTypes = types & other.types;
        if (newTypes == 0) return EmptyView.Any;
        var newMatcher = other.matcher;
        if (matcher != Matcher.ALL)
        {
            if (newMatcher != Matcher.ALL)
                newMatcher = new AndMatcher(matcher, newMatcher);
            else
                newMatcher = matcher;
        }
        var newFilter = other.filter;
        if (filter != null)
        {
            if (newFilter != null)
                newFilter = AndFilter.Create(filter, newFilter);
            else
                newFilter = filter;
        }
        return NewWith(newTypes, newMatcher, newFilter);
    }

    /// <remarks>Implements Java <c>java.lang.Iterable.iterator()</c> (abstract in this base).</remarks>
    public abstract IEnumerator<Feature> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

}
