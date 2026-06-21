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
/// A way feature, read from its anchor. It adds the way-specific <c>+12</c> field to the uniform
/// <see cref="FeatureHeader"/>: the body (the way's packed node coordinates and feature-node
/// references), with the relation table — when the way belongs to a relation — sitting just before it.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.StoredWay</c> (the body / reltable pointers).</remarks>
internal readonly struct Way
{

    const int BodyPpOfs = 12;
    const int RelTablePpBackset = 4; // the relation-table pointer sits 4 bytes before the body

    readonly ReadOnlyMemory<byte> _buf; // sliced to the feature anchor (the flags word)

    /// <summary>Wraps the given memory window, sliced to the way's anchor, as a cursor.</summary>
    public Way(ReadOnlyMemory<byte> buf)
    {
        _buf = buf;
    }

    /// <summary>The feature's uniform forward header (identity + tags).</summary>
    public FeatureHeader Header => new FeatureHeader(_buf);

    /// <summary>The way's body: its packed node coordinates and feature-node references.</summary>
    public ReadOnlyMemory<byte> Body => _buf.Slice(BodyPpOfs + _buf.Span.GetIntLE(BodyPpOfs));

    /// <summary>True when the way belongs to a relation, i.e. a relation table precedes its body.</summary>
    public bool HasRelationTable => Header.BelongsToRelation;

    /// <summary>
    /// The relation table — the relations this way belongs to. Stored just before the body. Throws
    /// <see cref="FeatureException"/> when the way has no relation table (see
    /// <see cref="HasRelationTable"/>).
    /// </summary>
    public ReadOnlyMemory<byte> RelationTable
    {
        get
        {
            if (!HasRelationTable)
                throw new FeatureException("Way has no relation table (it does not belong to a relation).");
            var ppRelTable = BodyPpOfs + _buf.Span.GetIntLE(BodyPpOfs) - RelTablePpBackset;
            return _buf.Slice(ppRelTable + _buf.Span.GetIntLE(ppRelTable));
        }
    }

}
