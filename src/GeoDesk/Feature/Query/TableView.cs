/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;
using GeoDesk.Feature.Filters;
using GeoDesk.Feature.Match;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using NioBuffer = GeoDesk.Buffers.NioBufferReader;

namespace GeoDesk.Feature.Query;

/// <summary>
/// A Feature Collection that is materialized by scanning a table.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TableView</c>.</remarks>
internal abstract class TableView : View
{

    protected readonly Segment segment;
    protected readonly int pTable;

    /// <summary>
    /// Creates a table view over the table at the given pointer, constrained by the
    /// supplied feature types, matcher, and optional filter.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TableView(FeatureStore, ByteBuffer, int, int, Matcher, Filter)</c>.</remarks>
    public TableView(FeatureStore store, Segment segment, int pTable, int types, Matcher matcher, IFilter? filter)
        : base(store, types, matcher, filter)
    {
        this.segment = segment;
        this.pTable = pTable;
    }

    /// <summary>
    /// A reader over the table's segment, derived on demand. <see cref="NioBuffer"/> is a cheap
    /// struct, so it is not cached as a field.
    /// </summary>
    protected NioBuffer buf => new NioBuffer(segment.Memory);

    /// <summary>
    /// Returns a view restricted to the features in this table that intersect the given
    /// bounding box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.query.TableView.in(Bounds)</c>.</remarks>
    public override IFeatureQuery In(IBounds bbox)
    {
        return Select(new BoundsFilter(bbox));
    }

}
