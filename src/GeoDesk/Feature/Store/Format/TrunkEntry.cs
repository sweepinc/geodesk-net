/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

/// <summary>A 20-byte R-tree trunk entry: a child bounding box plus a pointer to a child trunk or leaf.</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchTrunk(int)</c>.</remarks>
internal readonly struct TrunkEntry
{

    readonly Segment _buf;
    readonly int _p;

    public TrunkEntry(Segment buf, int p)
    {
        _buf = buf;
        _p = p;
    }

    int Word => _buf.GetInt(_p);

    public bool IsLast => (Word & 1) != 0;

    public bool IsLeaf => (Word & 2) != 0;

    public Bounds Bounds => new Bounds(_buf, _p + 4);

    /// <summary>Pointer to the child node (trunk or leaf), with the flag bits cleared.</summary>
    public int ChildPtr => _p + (int)((uint)Word & 0xffff_fffc);

    public const int Stride = 20;

}
