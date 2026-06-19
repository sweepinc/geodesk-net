/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

/// <summary>A 32-byte R-tree leaf entry for a way / area / relation feature.</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.searchLeaf(int)</c>.</remarks>
internal readonly struct LeafEntry
{

    readonly Segment _buf;
    readonly int _p;

    public LeafEntry(Segment buf, int p)
    {
        _buf = buf;
        _p = p;
    }

    public int Flags => _buf.GetInt(_p + 16);

    public bool IsLast => (Flags & 1) != 0;

    public Bounds Bounds => new Bounds(_buf, _p);

    /// <summary>This entry's single type bit, to AND against the query's accepted-types mask.</summary>
    public int TypeBit => 1 << (Flags >> 1);

    /// <summary>Pointer to the feature body.</summary>
    public int FeaturePtr => _p + 16;

    /// <summary>Feature pointer with its 2-bit type tag folded in (the value stored in QueryResults).</summary>
    public int TaggedFeaturePtr => FeaturePtr | (int)(((uint)Flags >> 3) & 3);

    public const int Stride = 32;

}
