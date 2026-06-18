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
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A Feature Collection that is materialized by scanning a table.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TableView</c>.</remarks>
internal abstract class TableView : View
{

    protected readonly NioBuffer buf;
    protected readonly int ptr;

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TableView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public TableView(FeatureStore store, NioBuffer buf, int ptr, int types, Matcher matcher, IFilter? filter)
        : base(store, types, matcher, filter)
    {
        this.buf = buf;
        this.ptr = ptr;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TableView.in(Bounds)</c>.</remarks>
    public override IFeatures In(IBounds bbox)
    {
        return Select(new BoundsFilter(bbox));
    }

}
