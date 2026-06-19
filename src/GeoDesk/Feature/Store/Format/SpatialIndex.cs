/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

/// <summary>
/// One of a tile's spatial indexes: a list of <see cref="IndexBucket"/>s, keyed by tag bits.
/// </summary>
/// <remarks>
/// Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchRTree(int, Matcher, RTreeQueryTask)</c>.
/// </remarks>
internal readonly struct SpatialIndex
{

    readonly Segment _buf;
    readonly int _pp;

    public SpatialIndex(Segment buf, int pp)
    {
        _buf = buf;
        _pp = pp;
    }

    public bool IsEmpty => _buf.GetInt(_pp) == 0;

    /// <summary>Pointer to the first 8-byte bucket entry. Only valid when <see cref="IsEmpty"/> is false.</summary>
    public int FirstBucketPtr => _pp + _buf.GetInt(_pp);

}
