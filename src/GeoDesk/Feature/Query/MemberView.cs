/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

/// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView</c>.</remarks>
internal class MemberView : TableView
{

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public MemberView(FeatureStore store, NioBuffer buf, int pTable, int types, Matcher matcher, IFilter? filter)
        : base(store, buf, pTable, types, matcher, filter)
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView.newWith(int, Matcher, Filter)</c>.</remarks>
    internal override IFeatureQuery NewWith(int types, Matcher matcher, IFilter? filter)
    {
        return new MemberView(store, buf, ptr, types, matcher, filter);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.MemberView.iterator()</c>.</remarks>
    public override IEnumerator<IFeature> GetEnumerator()
    {
        return new MemberIterator(store, buf, ptr, types, matcher, filter);
    }

}
