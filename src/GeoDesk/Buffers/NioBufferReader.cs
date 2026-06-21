/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Buffers;

/// <summary>
/// A lightweight, allocation-free <em>read-only</em> view over a <see cref="System.ReadOnlyMemory{Byte}"/>,
/// exposing the absolute (index-based) read accessors of the Java NIO <c>ByteBuffer</c> —
/// <see cref="GetInt"/>, <see cref="GetLong"/>, … — decoded little-endian via <see cref="BufferExtensions"/>.
/// </summary>
/// <remarks>
/// The store hands out <c>Segment</c>s, never buffers; a consumer that wants the NIO-style read API
/// wraps a <c>segment.Memory</c> in one of these on demand (<c>new NioBufferReader(segment.Memory)</c>).
/// Its writable counterpart is <see cref="NioBufferWriter"/>. New code should prefer the cursor structs.
/// </remarks>
internal readonly struct NioBufferReader
{

    readonly ReadOnlyMemory<byte> _mem;

    /// <summary>
    /// Wraps the given read-only memory window in a buffer reader.
    /// </summary>
    public NioBufferReader(ReadOnlyMemory<byte> mem)
    {
        _mem = mem;
    }

    /// <summary>The underlying window — for constructing cursors or further slicing.</summary>
    public ReadOnlyMemory<byte> Memory => _mem;

    /// <summary>The length of the window, in bytes.</summary>
    public int Length => _mem.Length;

    /// <summary>The byte at an absolute index (<c>ByteBuffer.get(int)</c>).</summary>
    public byte Get(int index) => _mem.Span[index];

    /// <summary>The little-endian 32-bit integer at an absolute index.</summary>
    public int GetInt(int index) => _mem.Span.GetIntLE(index);

    /// <summary>The little-endian 64-bit integer at an absolute index.</summary>
    public long GetLong(int index) => _mem.Span.GetLongLE(index);

    /// <summary>The little-endian 16-bit integer at an absolute index.</summary>
    public short GetShort(int index) => _mem.Span.GetShortLE(index);

    /// <summary>The little-endian 16-bit character at an absolute index.</summary>
    public char GetChar(int index) => _mem.Span.GetCharLE(index);

    /// <summary>Absolute bulk get (<c>ByteBuffer.get(int, byte[])</c>).</summary>
    public void Get(int index, byte[] dst) => _mem.Span.Slice(index, dst.Length).CopyTo(dst);

    /// <summary>Absolute bulk get (<c>ByteBuffer.get(int, byte[], int, int)</c>).</summary>
    public void Get(int index, byte[] dst, int offset, int length) => _mem.Span.Slice(index, length).CopyTo(dst.AsSpan(offset, length));

}
