/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

public class MemberView : TableView
{
    public MemberView(FeatureStore store, NioBuffer buf, int pTable,
        int types, Matcher matcher, Filter? filter)
        : base(store, buf, pTable, types, matcher, filter)
    {
    }

    protected override Features NewWith(int types, Matcher matcher, Filter? filter)
    {
        return new MemberView(store, buf, ptr, types, matcher, filter);
    }

    public override IEnumerator<Feature> GetEnumerator()
    {
        return new MemberIterator(store, buf, ptr, types, matcher, filter);
    }
}
