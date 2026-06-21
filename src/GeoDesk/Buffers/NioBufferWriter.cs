/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Buffers;

/// <summary>
/// A lightweight, allocation-free <em>read/write</em> view over a <see cref="System.Memory{Byte}"/>,
/// exposing the absolute (index-based) accessors of the Java NIO <c>ByteBuffer</c> — both the
/// <c>Get*</c> reads and the <c>Put*</c> writes — encoded/decoded little-endian via
/// <see cref="BufferExtensions"/>.
/// </summary>
/// <remarks>
/// The store hands out <c>Segment</c>s, never buffers; the write path wraps a <c>segment.Memory</c> in
/// one of these on demand (<c>new NioBufferWriter(segment.Memory)</c>) where it needs the NIO-style
/// write API. Its read-only counterpart is <see cref="NioBufferReader"/>.
/// </remarks>
internal readonly struct NioBufferWriter
{

    readonly Memory<byte> _mem;

    /// <summary>
    /// Wraps the given writable memory window in a buffer writer.
    /// </summary>
    public NioBufferWriter(Memory<byte> mem)
    {
        _mem = mem;
    }

    /// <summary>The underlying window — for further slicing or constructing cursors.</summary>
    public Memory<byte> Memory => _mem;

    /// <summary>The length of the window, in bytes.</summary>
    public int Length => _mem.Length;

    // --- reads ---

    /// <summary>The byte at an absolute index.</summary>
    public byte Get(int index) => _mem.Span[index];

    /// <summary>The little-endian 32-bit integer at an absolute index.</summary>
    public int GetInt(int index) => _mem.Span.GetIntLE(index);

    /// <summary>The little-endian 64-bit integer at an absolute index.</summary>
    public long GetLong(int index) => _mem.Span.GetLongLE(index);

    /// <summary>The little-endian 16-bit integer at an absolute index.</summary>
    public short GetShort(int index) => _mem.Span.GetShortLE(index);

    /// <summary>The little-endian 16-bit character at an absolute index.</summary>
    public char GetChar(int index) => _mem.Span.GetCharLE(index);

    /// <summary>Absolute bulk get into the whole destination array.</summary>
    public void Get(int index, byte[] dst) => _mem.Span.Slice(index, dst.Length).CopyTo(dst);

    /// <summary>Absolute bulk get of <paramref name="length"/> bytes into a region of the destination.</summary>
    public void Get(int index, byte[] dst, int offset, int length) =>
        _mem.Span.Slice(index, length).CopyTo(dst.AsSpan(offset, length));

    // --- writes ---

    /// <summary>Writes a byte at an absolute index.</summary>
    public void Put(int index, byte b) => _mem.Span[index] = b;

    /// <summary>Writes a little-endian 32-bit integer at an absolute index.</summary>
    public void PutInt(int index, int value) => _mem.Span.PutIntLE(index, value);

    /// <summary>Writes a little-endian 64-bit integer at an absolute index.</summary>
    public void PutLong(int index, long value) => _mem.Span.PutLongLE(index, value);

    /// <summary>Writes a little-endian 16-bit integer at an absolute index.</summary>
    public void PutShort(int index, short value) => _mem.Span.PutShortLE(index, value);

    /// <summary>Writes a little-endian 16-bit character at an absolute index.</summary>
    public void PutChar(int index, char value) => _mem.Span.PutCharLE(index, value);

    /// <summary>Absolute bulk put (<c>ByteBuffer.put(int, byte[])</c>).</summary>
    public void Put(int index, byte[] src) => src.AsSpan().CopyTo(_mem.Span.Slice(index));

    /// <summary>Absolute bulk put (<c>ByteBuffer.put(int, byte[], int, int)</c>).</summary>
    public void Put(int index, byte[] src, int offset, int length) =>
        src.AsSpan(offset, length).CopyTo(_mem.Span.Slice(index, length));

    /// <summary>A writable sub-window over [index, index+length).</summary>
    public NioBufferWriter Slice(int index, int length) => new NioBufferWriter(_mem.Slice(index, length));

}
