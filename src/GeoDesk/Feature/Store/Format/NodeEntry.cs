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
/// A variable-length R-tree leaf entry for a node feature (x/y + flags).
/// </summary>
/// <remarks>
/// Ported from Java <c>com.geodesk.feature.query.RTreeQueryTask.Nodes.searchLeaf(int)</c>.
/// </remarks>
internal readonly struct NodeEntry
{

    // Layout: x at 0, y at 4; the embedded node feature starts at +8, its first word being the flags.
    const int XOfs = 0;
    const int YOfs = 4;
    const int FeatureOfs = 8;
    const int LastFlag = 1;
    const int BaseSize = 20;

    readonly ReadOnlyMemory<byte> _buf; // sliced to the start of the node entry

    /// <summary>Wraps the given memory window, sliced to the start of a node entry, as a cursor.</summary>
    public NodeEntry(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>The flags word of the embedded node feature.</summary>
    public int Flags => _buf.Span.GetIntLE(FeatureOfs);

    /// <summary>True if this is the last entry in its leaf.</summary>
    public bool IsLast => (Flags & LastFlag) != 0;

    /// <summary>The node's X coordinate.</summary>
    public int X => _buf.Span.GetIntLE(XOfs);

    /// <summary>The node's Y coordinate.</summary>
    public int Y => _buf.Span.GetIntLE(YOfs);

    /// <summary>The embedded node feature, anchored at its flags word.</summary>
    public Node Feature => new Node(_buf.Slice(FeatureOfs));

    /// <summary>
    /// The number of bytes this entry occupies — the base size, plus the relation-table pointer when
    /// the node is a relation member. A consumer advances by this to reach the next entry. (The flag's
    /// value, 4, is also the size in bytes of that extra pointer.)
    /// </summary>
    public int Size => BaseSize + (Flags & FeatureFlags.RELATION_MEMBER_FLAG);

    /// <summary>Returns true if the node lies within the given bounding box.</summary>
    public bool InBounds(int minX, int minY, int maxX, int maxY)
    {
        var x = X;
        var y = Y;
        return !(x > maxX || y > maxY || x < minX || y < minY);
    }

}
