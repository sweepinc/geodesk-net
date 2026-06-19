/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView</c>.</remarks>
internal class EmptyView : View
{

    public static readonly IFeatureQuery Any = new EmptyView();

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView()</c>.</remarks>
    public EmptyView()
        : base(null!, 0, null!, null)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.select(int, String)</c>.</remarks>
    protected override IFeatureQuery Select(int newTypes, string query)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.isEmpty()</c>.</remarks>
    public bool IsEmpty()
    {
        return true;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.count()</c>.</remarks>
    public long Count()
    {
        return 0;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.contains(Object)</c>.</remarks>
    public bool Contains(object f)
    {
        return false;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.select(String)</c>.</remarks>
    public override IFeatureQuery Select(string filter)
    {
        return Any;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.in(Bounds)</c>.</remarks>
    public override IFeatureQuery In(IBounds bbox)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.select(Filter)</c>.</remarks>
    public override IFeatureQuery Select(IFilter filter)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.nodesOf(Feature)</c>.</remarks>
    public override IFeatureQuery NodesOf(IFeature parent)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.membersOf(Feature)</c>.</remarks>
    public override IFeatureQuery MembersOf(IFeature parent)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.parentsOf(Feature)</c>.</remarks>
    public override IFeatureQuery ParentsOf(IFeature child)
    {
        return this;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return Enumerable.Empty<IFeature>().GetEnumerator();
    }

}
