/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

namespace GeoDesk.Feature.Store.Format;

/// <summary>
/// One of a tile's spatial indexes: a list of <see cref="IndexBucket"/>s, keyed by tag bits.
/// </summary>
/// <remarks>
/// Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchRTree(int, Matcher, RTreeQueryTask)</c>.
/// </remarks>
internal readonly struct SpatialIndex
{

    // Relative pointer to the first bucket; a value of 0 means the index is empty.
    const int FirstBucketPtrOfs = 0;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the index's root pointer

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="buf"></param>
    public SpatialIndex(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>
    /// True if the index is empty (has no buckets). An empty index is valid but has no contents.
    /// </summary>
    public bool IsNil => _buf.Span.GetIntLE(FirstBucketPtrOfs) == 0;

    /// <summary>
    /// The first bucket. Only valid when <see cref="IsNil"/> is false.
    /// </summary>
    public IndexBucket FirstBucket => new IndexBucket(_buf.Slice(_buf.Span.GetIntLE(FirstBucketPtrOfs)));

}
