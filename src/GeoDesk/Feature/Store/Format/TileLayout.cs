/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

// Strong typed cursors over the on-disk tile / spatial-index layout. Each is a readonly struct over
// a (buffer, offset) pair — the buffer is one mapped segment, the offset locates the record within
// it. These replace the raw magic-offset buffer reads the query engine used to inline.
//
// PORT: the field offsets encapsulated here are the ones the Java query engine read inline in
// TileQueryTask and RTreeQueryTask; no single Java struct exists, so each cursor cites the Java
// method whose accesses it folds.

/// <summary>A bounding box stored as four consecutive little-endian ints (minX, minY, maxX, maxY).</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchTrunk/searchLeaf</c> bbox tests.</remarks>
internal readonly struct Bounds
{

    readonly Segment _buf;
    readonly int _p;

    public Bounds(Segment buf, int p)
    {
        _buf = buf;
        _p = p;
    }

    public int MinX => _buf.GetInt(_p);

    public int MinY => _buf.GetInt(_p + 4);

    public int MaxX => _buf.GetInt(_p + 8);

    public int MaxY => _buf.GetInt(_p + 12);

    /// <summary>
    /// Returns true if this box intersects the given query box.
    /// </summary>
    public bool Intersects(int minX, int minY, int maxX, int maxY)
    {
        return !(MinX > maxX || MinY > maxY || MaxX < minX || MaxY < minY);
    }

}
