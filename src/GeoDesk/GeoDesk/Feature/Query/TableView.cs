/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A Feature Collection that is materialized by scanning a table.
/// </summary>
public abstract class TableView : View
{
    protected readonly NioBuffer buf;
    protected readonly int ptr;

    public TableView(FeatureStore store, NioBuffer buf, int ptr,
        int types, Matcher matcher, Filter? filter)
        : base(store, types, matcher, filter)
    {
        this.buf = buf;
        this.ptr = ptr;
    }

    public override Features In(Bounds bbox)
    {
        return Select(new BoundsFilter(bbox));
    }
}
