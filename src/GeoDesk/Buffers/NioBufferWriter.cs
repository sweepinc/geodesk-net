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

    public NioBufferWriter(Memory<byte> mem)
    {
        _mem = mem;
    }

    /// <summary>The underlying window — for further slicing or constructing cursors.</summary>
    public Memory<byte> Memory => _mem;

    /// <summary>The length of the window, in bytes.</summary>
    public int Length => _mem.Length;

    // --- reads ---

    public byte Get(int index) => _mem.Span[index];

    public int GetInt(int index) => _mem.Span.GetIntLE(index);

    public long GetLong(int index) => _mem.Span.GetLongLE(index);

    public short GetShort(int index) => _mem.Span.GetShortLE(index);

    public char GetChar(int index) => _mem.Span.GetCharLE(index);

    public void Get(int index, byte[] dst) => _mem.Span.Slice(index, dst.Length).CopyTo(dst);

    public void Get(int index, byte[] dst, int offset, int length) =>
        _mem.Span.Slice(index, length).CopyTo(dst.AsSpan(offset, length));

    // --- writes ---

    public void Put(int index, byte b) => _mem.Span[index] = b;

    public void PutInt(int index, int value) => _mem.Span.PutIntLE(index, value);

    public void PutLong(int index, long value) => _mem.Span.PutLongLE(index, value);

    public void PutShort(int index, short value) => _mem.Span.PutShortLE(index, value);

    public void PutChar(int index, char value) => _mem.Span.PutCharLE(index, value);

    /// <summary>Absolute bulk put (<c>ByteBuffer.put(int, byte[])</c>).</summary>
    public void Put(int index, byte[] src) => src.AsSpan().CopyTo(_mem.Span.Slice(index));

    /// <summary>Absolute bulk put (<c>ByteBuffer.put(int, byte[], int, int)</c>).</summary>
    public void Put(int index, byte[] src, int offset, int length) =>
        src.AsSpan(offset, length).CopyTo(_mem.Span.Slice(index, length));

    /// <summary>A writable sub-window over [index, index+length).</summary>
    public NioBufferWriter Slice(int index, int length) => new NioBufferWriter(_mem.Slice(index, length));

}
