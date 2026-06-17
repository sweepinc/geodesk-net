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

/// @hidden
public abstract class View : Features
{
    /// @hidden
    protected internal readonly FeatureStore store;
    /// @hidden
    protected internal readonly int types;
    /// @hidden
    protected internal readonly Matcher matcher;
    /// @hidden
    protected internal readonly Filter? filter;

    protected View(FeatureStore store, int types, Matcher matcher, Filter? filter)
    {
        this.store = store;
        this.types = types;
        this.matcher = matcher;
        this.filter = filter;
    }

    protected abstract Features NewWith(int types, Matcher matcher, Filter? filter);

    public int TypesValue()
    {
        return types;
    }

    /// @hidden
    protected Features Select(int newTypes)
    {
        newTypes &= types;
        if (newTypes == 0) return EmptyView.ANY;
        return NewWith(newTypes, matcher, filter);
    }

    /// @hidden
    protected virtual Features Select(int newTypes, string query)
    {
        Matcher newMatcher = store.GetMatcher(query);
        if (matcher != Matcher.ALL)
        {
            newMatcher = new AndMatcher(matcher, newMatcher);
        }
        newTypes &= types & newMatcher.AcceptedTypes();
        if (newTypes == 0) return EmptyView.ANY;
        return NewWith(newTypes, newMatcher, filter);
    }

    public virtual Features Select(string query)
    {
        return Select(TypeBits.ALL, query);
    }

    public Features Nodes()
    {
        return Select(TypeBits.NODES);
    }

    public Features Nodes(string query)
    {
        return Select(TypeBits.NODES, query);
    }

    public Features Ways()
    {
        return Select(TypeBits.WAYS);
    }

    public Features Ways(string query)
    {
        return Select(TypeBits.WAYS, query);
    }

    public Features Relations()
    {
        return Select(TypeBits.RELATIONS);
    }

    public Features Relations(string query)
    {
        return Select(TypeBits.RELATIONS, query);
    }

    public virtual Features Select(Filter filter)
    {
        int newTypes = types;
        if (this.filter != null)
        {
            filter = AndFilter.Create(this.filter, filter);
            if (filter == FalseFilter.INSTANCE) return EmptyView.ANY;
        }
        int strategy = filter.Strategy();
        if ((strategy & FilterStrategy.RESTRICTS_TYPES) != 0)
        {
            newTypes &= filter.AcceptedTypes();
            if (newTypes == 0) return EmptyView.ANY;
        }
        return NewWith(newTypes, matcher, filter);
    }

    public virtual Features ParentsOf(Feature child)
    {
        if (child.IsNode())
        {
            if (child is AnonymousWayNode wayNode)
            {
                // An anonymous node *always* has at least one parent way,
                // and *never* has parent relations
                return wayNode.Parents(types, matcher, filter);
            }
            return ((StoredNode)child).Parents(types, matcher, filter);
        }
        else
        {
            // Ways and relations can only have relations as parents
            if ((types & TypeBits.RELATIONS) == 0) return EmptyView.ANY;
            StoredFeature f = (StoredFeature)child;
            if (!f.BelongsToRelation()) return EmptyView.ANY;
            return new ParentRelationView(store, f.Buffer(),
                f.GetRelationTablePtr(), types, matcher, filter);
        }
    }

    public virtual Features MembersOf(Feature parent)
    {
        if (parent.IsRelation())
        {
            return ((StoredRelation)parent).Members(types, matcher, filter);
        }
        // TODO: membersOf() for ways

        return EmptyView.ANY;
    }

    public virtual Features NodesOf(Feature parent)
    {
        if ((types & TypeBits.NODES) == 0) return EmptyView.ANY;
        if (parent.IsWay())
        {
            StoredWay way = (StoredWay)parent;
            if (matcher != Matcher.ALL &&
                (way.Flags() & IFeatureFlags.WAYNODE_FLAG) == 0)
            {
                // GOQL queries only return feature nodes; if the Way's
                // waynode_flag is cleared, it only contains anonymous
                // nodes, so we return an empty set
                return EmptyView.ANY;
            }
            return new WayNodeView(store, way.Buffer(), way.Pointer(),
                types, matcher, filter);
        }

        // TODO: nodesOf() for relations

        return EmptyView.ANY;
    }

    public abstract Features In(Bounds bbox);

    public Features Select(Features otherFeatures)
    {
        // TODO: This assumes both views are WorldViews (which is wrong)
        //  At least one must be a WorldView
        // TODO: Throw exception if the Views have different stores

        View other = (View)otherFeatures;
            // TODO: For now, all Features are implemented as a View,
            //  but this may change in the future
        int newTypes = types & other.types;
        if (newTypes == 0) return EmptyView.ANY;
        Matcher newMatcher = other.matcher;
        if (matcher != Matcher.ALL)
        {
            if (newMatcher != Matcher.ALL)
            {
                newMatcher = new AndMatcher(matcher, newMatcher);
            }
            else
            {
                newMatcher = matcher;
            }
        }
        Filter? newFilter = other.filter;
        if (filter != null)
        {
            if (newFilter != null)
            {
                newFilter = AndFilter.Create(filter, newFilter);
            }
            else
            {
                newFilter = filter;
            }
        }
        return NewWith(newTypes, newMatcher, newFilter);
    }

    public abstract IEnumerator<Feature> GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
