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
/// A node feature, read from its anchor. A node has <em>no body</em>; its <c>+12</c> pointer, when
/// present, is the relation table directly — and it is present only when the node belongs to a
/// relation. So beyond the uniform <see cref="FeatureHeader"/> a node maps only that one optional field.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredNode</c> (<c>GetRelationTablePtr</c>:
/// "A Node's body pointer is the pointer to its reltable").</remarks>
internal readonly struct Node
{

    const int RelTablePpOfs = 12; // a node's +12 pointer IS its relation table (there is no body)

    readonly ReadOnlyMemory<byte> _buf; // sliced to the feature anchor (the flags word)

    public Node(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>The feature's uniform forward header (identity + tags).</summary>
    public FeatureHeader Header => new FeatureHeader(_buf);

    /// <summary>True when the node belongs to a relation, i.e. it has a relation table at +12.</summary>
    public bool HasRelationTable => Header.BelongsToRelation;

    /// <summary>
    /// The relation table — the relations this node belongs to. A node has no body; its +12 pointer is
    /// the relation table directly. Only valid when <see cref="HasRelationTable"/>.
    /// </summary>
    public ReadOnlyMemory<byte> RelationTable => _buf.Slice(RelTablePpOfs + _buf.Span.GetIntLE(RelTablePpOfs));

}
