/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

/// <summary>A tile's header: the four spatial-index roots (nodes, ways, areas, relations).</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.exec()</c> (pTile + 8/12/16/20).</remarks>
internal readonly struct Tile
{

    readonly Segment _buf;
    readonly int _p;

    public Tile(Segment buf, int pTile)
    {
        _buf = buf;
        _p = pTile;
    }

    public SpatialIndex NodeIndex => new SpatialIndex(_buf, _p + 8);

    public SpatialIndex WayIndex => new SpatialIndex(_buf, _p + 12);

    public SpatialIndex AreaIndex => new SpatialIndex(_buf, _p + 16);

    public SpatialIndex RelationIndex => new SpatialIndex(_buf, _p + 20);

}
