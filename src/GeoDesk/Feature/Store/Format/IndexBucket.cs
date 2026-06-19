/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

/// <summary>An 8-byte index bucket: an accepted-tag key set plus the R-tree root for its features.</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.TileQueryTask.searchRTree(int, Matcher, RTreeQueryTask)</c>.</remarks>
internal readonly struct IndexBucket
{

    readonly Segment _buf;
    readonly int _p;

    public IndexBucket(Segment buf, int p)
    {
        _buf = buf;
        _p = p;
    }

    public int KeyBits => _buf.GetInt(_p + 4);

    public bool IsLast => (_buf.GetInt(_p) & 1) != 0;

    /// <summary>Pointer to the bucket's R-tree root trunk node.</summary>
    public int RootPtr => _p + (int)((uint)_buf.GetInt(_p) & 0xffff_fffc);

    public const int Stride = 8;

}
