/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;

using GeoDesk.Feature.Match;
using GeoDesk.Geom;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A view that contains no features. Every refinement operation returns the same empty view and the
/// terminal operations report emptiness, making it the canonical result of a query that can never
/// match. Exposed as the shared <see cref="Any"/> singleton.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView</c>.</remarks>
internal class EmptyView : View
{

    public static readonly IFeatureQuery Any = new EmptyView();

    /// <summary>
    /// Creates the empty view with no store, no types, and no matcher.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView()</c>.</remarks>
    public EmptyView() :
        base(null!, 0, null!, null)
    {

    }

    /// <summary>
    /// Selecting by type and query on an empty view yields the same empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.select(int, String)</c>.</remarks>
    protected override IFeatureQuery Select(int newTypes, string query)
    {
        return this;
    }

    /// <summary>
    /// Refining an empty view always produces the same empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return this;
    }

    /// <summary>
    /// Always true; the empty view contains no features.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.isEmpty()</c>.</remarks>
    public override bool IsEmpty() => true;

    /// <summary>
    /// Always zero.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.count()</c>.</remarks>
    public override long Count() => 0;

    /// <summary>
    /// Always false; the empty view contains no features.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.contains(Object)</c>.</remarks>
    public override bool Contains(IFeature f)
    {
        return false;
    }

    /// <summary>
    /// Selecting by query string yields the empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.select(String)</c>.</remarks>
    public override IFeatureQuery Select(string filter)
    {
        return Any;
    }

    /// <summary>
    /// Restricting to a bounding box yields the same empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.in(Bounds)</c>.</remarks>
    public override IFeatureQuery In(IBounds bbox)
    {
        return this;
    }

    /// <summary>
    /// Applying a filter yields the same empty view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.select(Filter)</c>.</remarks>
    public override IFeatureQuery Select(IFilter filter)
    {
        return this;
    }

    /// <summary>
    /// The nodes of a parent within an empty view are empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.nodesOf(Feature)</c>.</remarks>
    public override IFeatureQuery NodesOf(IFeature parent)
    {
        return this;
    }

    /// <summary>
    /// The members of a parent within an empty view are empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.membersOf(Feature)</c>.</remarks>
    public override IFeatureQuery MembersOf(IFeature parent)
    {
        return this;
    }

    /// <summary>
    /// The parents of a child within an empty view are empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.parentsOf(Feature)</c>.</remarks>
    public override IFeatureQuery ParentsOf(IFeature child)
    {
        return this;
    }

    /// <summary>
    /// Returns an enumerator over no features.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.EmptyView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return Enumerable.Empty<IFeature>().GetEnumerator();
    }

}
