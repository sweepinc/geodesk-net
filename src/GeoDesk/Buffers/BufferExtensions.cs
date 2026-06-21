/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers.Binary;

namespace GeoDesk.Buffers;

/// <summary>
/// Little-endian typed accessors over a byte span. The store format is always little-endian, so
/// these are the single place that decode/encode happens — every cursor (Header, Block, Tile,
/// MemberTable, …) reads/writes its fields through these, regardless of whether the underlying
/// window is a mapped segment, a heap block, or a decompressed buffer.
/// </summary>
internal static class BufferExtensions
{

    // --- little-endian reads (also callable on a Span<byte> via the overloads below) ---

    /// <summary>Reads a little-endian 32-bit integer at the given offset of the span.</summary>
    public static int GetIntLE(this ReadOnlySpan<byte> b, int offset)
        => BinaryPrimitives.ReadInt32LittleEndian(b.Slice(offset));

    /// <summary>Reads a little-endian 64-bit integer at the given offset of the span.</summary>
    public static long GetLongLE(this ReadOnlySpan<byte> b, int offset)
        => BinaryPrimitives.ReadInt64LittleEndian(b.Slice(offset));

    /// <summary>Reads a little-endian 16-bit integer at the given offset of the span.</summary>
    public static short GetShortLE(this ReadOnlySpan<byte> b, int offset)
        => BinaryPrimitives.ReadInt16LittleEndian(b.Slice(offset));

    /// <summary>Reads a little-endian 16-bit character at the given offset of the span.</summary>
    public static char GetCharLE(this ReadOnlySpan<byte> b, int offset)
        => (char)BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(offset));

    // A Span<byte> does not implicitly satisfy a ReadOnlySpan<byte> extension receiver, so the
    // read accessors are mirrored for Span<byte> (delegating) — lets write cursors read their own data.

    /// <summary>Reads a little-endian 32-bit integer at the given offset of the writable span.</summary>
    public static int GetIntLE(this Span<byte> b, int offset) => GetIntLE((ReadOnlySpan<byte>)b, offset);

    /// <summary>Reads a little-endian 64-bit integer at the given offset of the writable span.</summary>
    public static long GetLongLE(this Span<byte> b, int offset) => GetLongLE((ReadOnlySpan<byte>)b, offset);

    /// <summary>Reads a little-endian 16-bit integer at the given offset of the writable span.</summary>
    public static short GetShortLE(this Span<byte> b, int offset) => GetShortLE((ReadOnlySpan<byte>)b, offset);

    /// <summary>Reads a little-endian 16-bit character at the given offset of the writable span.</summary>
    public static char GetCharLE(this Span<byte> b, int offset) => GetCharLE((ReadOnlySpan<byte>)b, offset);

    // --- little-endian writes ---

    /// <summary>Writes a little-endian 32-bit integer at the given offset of the span.</summary>
    public static void PutIntLE(this Span<byte> b, int offset, int value)
        => BinaryPrimitives.WriteInt32LittleEndian(b.Slice(offset), value);

    /// <summary>Writes a little-endian 64-bit integer at the given offset of the span.</summary>
    public static void PutLongLE(this Span<byte> b, int offset, long value)
        => BinaryPrimitives.WriteInt64LittleEndian(b.Slice(offset), value);

    /// <summary>Writes a little-endian 16-bit integer at the given offset of the span.</summary>
    public static void PutShortLE(this Span<byte> b, int offset, short value)
        => BinaryPrimitives.WriteInt16LittleEndian(b.Slice(offset), value);

    /// <summary>Writes a little-endian 16-bit character at the given offset of the span.</summary>
    public static void PutCharLE(this Span<byte> b, int offset, char value)
        => BinaryPrimitives.WriteUInt16LittleEndian(b.Slice(offset), (ushort)value);

}
