/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

namespace GeoDesk.Feature.Store.Format;

// Strong typed cursors over the on-disk tile / spatial-index layout. Each is a readonly struct over a
// ReadOnlyMemory<byte> already sliced to the record's start — the memory carries the position, so the
// fields read at fixed relative offsets and navigation is just a further Slice. These replace the raw
// magic-offset buffer reads the query engine used to inline.
//
// PORT: the field offsets encapsulated here are the ones the Java query engine read inline in
// TileQueryTask and RTreeQueryTask; no single Java struct exists, so each cursor cites the Java
// method whose accesses it folds.

/// <summary>A bounding box stored as four consecutive little-endian ints (minX, minY, maxX, maxY).</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchTrunk/searchLeaf</c> bbox tests.</remarks>
internal readonly struct Bounds
{

    const int MinXOfs = 0;
    const int MinYOfs = 4;
    const int MaxXOfs = 8;
    const int MaxYOfs = 12;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the 4-int bbox

    /// <summary>Wraps the given memory window, sliced to the start of the 4-int bbox, as a cursor.</summary>
    public Bounds(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>The box's minimum X coordinate.</summary>
    public int MinX => _buf.Span.GetIntLE(MinXOfs);

    /// <summary>The box's minimum Y coordinate.</summary>
    public int MinY => _buf.Span.GetIntLE(MinYOfs);

    /// <summary>The box's maximum X coordinate.</summary>
    public int MaxX => _buf.Span.GetIntLE(MaxXOfs);

    /// <summary>The box's maximum Y coordinate.</summary>
    public int MaxY => _buf.Span.GetIntLE(MaxYOfs);

    /// <summary>
    /// Returns true if this box intersects the given query box.
    /// </summary>
    public bool Intersects(int minX, int minY, int maxX, int maxY)
    {
        return !(MinX > maxX || MinY > maxY || MaxX < minX || MaxY < minY);
    }

}
