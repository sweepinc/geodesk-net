/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Common.Store;

namespace GeoDesk.Feature.Store.Format;

/// <summary>
/// A variable-length R-tree leaf entry for a node feature (x/y + flags).
/// </summary>
/// <remarks>
/// Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.Nodes.searchLeaf(int)</c>.
/// </remarks>
internal readonly struct NodeEntry
{

    readonly Segment _buf;
    readonly int _p;

    public NodeEntry(Segment buf, int p)
    {
        _buf = buf;
        _p = p;
    }

    public int Flags => _buf.GetInt(_p + 8);

    public bool IsLast => (Flags & 1) != 0;

    public int X => _buf.GetInt(_p);

    public int Y => _buf.GetInt(_p + 4);

    /// <summary>Pointer to the node feature body.</summary>
    public int FeaturePtr => _p + 8;

    /// <summary>Stride to the next entry: 20 bytes, plus 4 if the node carries a relation-table pointer.</summary>
    public int Stride => 20 + (Flags & 4);

    public bool InBounds(int minX, int minY, int maxX, int maxY)
    {
        var x = X;
        var y = Y;
        return !(x > maxX || y > maxY || x < minX || y < minY);
    }

}
