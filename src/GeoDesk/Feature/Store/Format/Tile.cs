/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Feature.Store.Format;

/// <summary>A tile's header: the four spatial-index roots (nodes, ways, areas, relations).</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.exec()</c> (pTile + 8/12/16/20).</remarks>
internal readonly struct Tile
{

    // Tile-header layout: the four spatial-index root pointers, after the leading header words.
    const int NodeIndexOfs = 8;
    const int WayIndexOfs = 12;
    const int AreaIndexOfs = 16;
    const int RelationIndexOfs = 20;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the tile

    public Tile(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    public SpatialIndex NodeIndex => new SpatialIndex(_buf.Slice(NodeIndexOfs));

    public SpatialIndex WayIndex => new SpatialIndex(_buf.Slice(WayIndexOfs));

    public SpatialIndex AreaIndex => new SpatialIndex(_buf.Slice(AreaIndexOfs));

    public SpatialIndex RelationIndex => new SpatialIndex(_buf.Slice(RelationIndexOfs));

}
