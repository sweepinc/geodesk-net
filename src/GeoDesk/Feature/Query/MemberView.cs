/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A queryable view over the members of a relation. Enumerating it produces the
/// member features that satisfy the view's type, matcher, and filter constraints.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView</c>.</remarks>
internal class MemberView : TableView
{

    /// <summary>
    /// Creates a member view over the relation member table at the given pointer,
    /// constrained by the supplied feature types, matcher, and optional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public MemberView(FeatureStore store, NioBuffer buf, int pTable, int types, Matcher matcher, IFilter? filter)
        : base(store, buf, pTable, types, matcher, filter)
    {
    }

    /// <summary>
    /// Returns a new member view over the same member table with the given type,
    /// matcher, and filter constraints applied.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return new MemberView(store, buf, ptr, types, matcher, filter);
    }

    /// <summary>
    /// Returns an iterator over the relation members matched by this view.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return new MemberIterator(store, buf, ptr, types, matcher, filter);
    }

}
