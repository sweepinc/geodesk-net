/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Java.Nio;

/// <summary>
/// A faithful subset of <c>java.nio.ByteBuffer</c>: an indexable region of bytes with a
/// position, limit, capacity and configurable byte order. Supports absolute and relative
/// get/put of bytes and 16/32/64-bit integers, plus slice/duplicate.
///
/// A single implementation backed by <see cref="System.Memory{Byte}"/>: heap buffers wrap a
/// managed <c>byte[]</c>; mapped buffers wrap a memory-mapped file view exposed via a
/// <see cref="System.Buffers.MemoryManager{Byte}"/>. A mapped buffer is given the manager as its
/// <see cref="IDisposable"/> owner (deterministic unmap) and a flush delegate for <see cref="Force"/>;
/// heap buffers and slices have neither.
///
/// Only the operations used by the GeoDesk port are implemented; this is not a complete
/// reimplementation of java.nio.
/// </summary>
/// <remarks>.NET stand-in for <c>java.nio.ByteBuffer</c> (no <c>com.*</c> Java source; mirrors the JDK type).</remarks>
public sealed class ByteBuffer : IDisposable
{
    readonly Memory<byte> _mem;
    readonly IDisposable? _owner;
    readonly Action? _onForce;
    int position;
    int limit;
    int capacity;
    ByteOrder order = ByteOrder.BigEndian; // Java's default

    ByteBuffer(Memory<byte> mem, IDisposable? owner, Action? onForce)
    {
        _mem = mem;
        _owner = owner;
        _onForce = onForce;
        capacity = mem.Length;
        limit = capacity;
        position = 0;
    }

    // --- backing primitives (over the Memory<byte> window) ---

    public byte Get(int index)
    {
        return _mem.Span[index];
    }

    public ByteBuffer Put(int index, byte b)
    {
        _mem.Span[index] = b;
        return this;
    }

    void GetBytes(int index, Span<byte> dst)
    {
        _mem.Span.Slice(index, dst.Length).CopyTo(dst);
    }

    void PutBytes(int index, ReadOnlySpan<byte> src)
    {
        src.CopyTo(_mem.Span.Slice(index, src.Length));
    }

    /// <summary>The backing array (heap buffers only); null for mapped buffers.</summary>
    public byte[]? Array()
    {
        return MemoryMarshal.TryGetArray<byte>(_mem, out var seg) ? seg.Array : null;
    }

    public ByteBuffer Slice()
    {
        // Slices share the underlying memory but do not own it (only the root buffer disposes/flushes).
        var s = new ByteBuffer(_mem.Slice(position, limit - position), null, null);
        s.order = order;
        return s;
    }

    /// <summary>Absolute slice (Java 13+): a buffer over [index, index+length).</summary>
    public ByteBuffer Slice(int index, int length)
    {
        var s = new ByteBuffer(_mem.Slice(index, length), null, null);
        s.order = order;
        return s;
    }

    public ByteBuffer Duplicate()
    {
        var d = new ByteBuffer(_mem, null, null);
        d.position = position;
        d.limit = limit;
        d.order = order;
        return d;
    }

    // --- absolute typed accessors (honor byte order) ---

    public int GetInt(int index)
    {
        Span<byte> s = stackalloc byte[4];
        GetBytes(index, s);
        return order == ByteOrder.LittleEndian
            ? BinaryPrimitives.ReadInt32LittleEndian(s)
            : BinaryPrimitives.ReadInt32BigEndian(s);
    }

    public ByteBuffer PutInt(int index, int value)
    {
        Span<byte> s = stackalloc byte[4];
        if (order == ByteOrder.LittleEndian) BinaryPrimitives.WriteInt32LittleEndian(s, value);
        else BinaryPrimitives.WriteInt32BigEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    public long GetLong(int index)
    {
        Span<byte> s = stackalloc byte[8];
        GetBytes(index, s);
        return order == ByteOrder.LittleEndian
            ? BinaryPrimitives.ReadInt64LittleEndian(s)
            : BinaryPrimitives.ReadInt64BigEndian(s);
    }

    public ByteBuffer PutLong(int index, long value)
    {
        Span<byte> s = stackalloc byte[8];
        if (order == ByteOrder.LittleEndian) BinaryPrimitives.WriteInt64LittleEndian(s, value);
        else BinaryPrimitives.WriteInt64BigEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    public short GetShort(int index)
    {
        Span<byte> s = stackalloc byte[2];
        GetBytes(index, s);
        return order == ByteOrder.LittleEndian
            ? BinaryPrimitives.ReadInt16LittleEndian(s)
            : BinaryPrimitives.ReadInt16BigEndian(s);
    }

    public ByteBuffer PutShort(int index, short value)
    {
        Span<byte> s = stackalloc byte[2];
        if (order == ByteOrder.LittleEndian) BinaryPrimitives.WriteInt16LittleEndian(s, value);
        else BinaryPrimitives.WriteInt16BigEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    public char GetChar(int index)
    {
        Span<byte> s = stackalloc byte[2];
        GetBytes(index, s);
        ushort v = order == ByteOrder.LittleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(s)
            : BinaryPrimitives.ReadUInt16BigEndian(s);
        return (char)v;
    }

    public ByteBuffer PutChar(int index, char value)
    {
        Span<byte> s = stackalloc byte[2];
        if (order == ByteOrder.LittleEndian) BinaryPrimitives.WriteUInt16LittleEndian(s, value);
        else BinaryPrimitives.WriteUInt16BigEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    // --- relative accessors ---

    public byte Get()
    {
        byte b = Get(position);
        position++;
        return b;
    }

    public ByteBuffer Put(byte b)
    {
        Put(position, b);
        position++;
        return this;
    }

    public int GetInt()
    {
        int v = GetInt(position);
        position += 4;
        return v;
    }

    public ByteBuffer PutInt(int value)
    {
        PutInt(position, value);
        position += 4;
        return this;
    }

    public long GetLong()
    {
        long v = GetLong(position);
        position += 8;
        return v;
    }

    public short GetShort()
    {
        short v = GetShort(position);
        position += 2;
        return v;
    }

    public char GetChar()
    {
        char v = GetChar(position);
        position += 2;
        return v;
    }

    /// <summary>Relative bulk get into the whole array.</summary>
    public ByteBuffer Get(byte[] dst)
    {
        return Get(dst, 0, dst.Length);
    }

    /// <summary>Relative bulk get.</summary>
    public ByteBuffer Get(byte[] dst, int offset, int length)
    {
        GetBytes(position, dst.AsSpan(offset, length));
        position += length;
        return this;
    }

    /// <summary>Absolute bulk get (Java 13+).</summary>
    public ByteBuffer Get(int index, byte[] dst)
    {
        GetBytes(index, dst.AsSpan());
        return this;
    }

    /// <summary>Absolute bulk get (Java 13+).</summary>
    public ByteBuffer Get(int index, byte[] dst, int offset, int length)
    {
        GetBytes(index, dst.AsSpan(offset, length));
        return this;
    }

    /// <summary>Absolute bulk put (Java 13+).</summary>
    public ByteBuffer Put(int index, byte[] src)
    {
        PutBytes(index, src.AsSpan());
        return this;
    }

    /// <summary>Absolute bulk put (Java 13+).</summary>
    public ByteBuffer Put(int index, byte[] src, int offset, int length)
    {
        PutBytes(index, src.AsSpan(offset, length));
        return this;
    }

    /// <summary>Absolute bulk put from another buffer (Java 16+).</summary>
    public ByteBuffer Put(int index, ByteBuffer src, int srcIndex, int length)
    {
        byte[] tmp = new byte[length];
        src.GetBytes(srcIndex, tmp);
        PutBytes(index, tmp);
        return this;
    }

    /// <summary>Relative bulk put of the whole array.</summary>
    public ByteBuffer Put(byte[] src)
    {
        return Put(src, 0, src.Length);
    }

    /// <summary>Relative bulk put.</summary>
    public ByteBuffer Put(byte[] src, int offset, int length)
    {
        PutBytes(position, src.AsSpan(offset, length));
        position += length;
        return this;
    }

    // --- position / limit / capacity / order ---

    public int Position()
    {
        return position;
    }

    public ByteBuffer Position(int newPosition)
    {
        position = newPosition;
        return this;
    }

    public int Limit()
    {
        return limit;
    }

    public ByteBuffer Limit(int newLimit)
    {
        limit = newLimit;
        if (position > limit) position = limit;
        return this;
    }

    public int Capacity()
    {
        return capacity;
    }

    public ByteOrder Order()
    {
        return order;
    }

    public ByteBuffer Order(ByteOrder bo)
    {
        order = bo;
        return this;
    }

    public ByteBuffer Clear()
    {
        position = 0;
        limit = capacity;
        return this;
    }

    public ByteBuffer Flip()
    {
        limit = position;
        position = 0;
        return this;
    }

    public ByteBuffer Rewind()
    {
        position = 0;
        return this;
    }

    public int Remaining()
    {
        return limit - position;
    }

    public bool HasRemaining()
    {
        return position < limit;
    }

    // --- mapped-backing lifetime (no-op for heap buffers) ---

    /// <summary>Flushes a mapped buffer's pending writes to disk (Java's <c>MappedByteBuffer.force()</c>).</summary>
    public void Force()
    {
        _onForce?.Invoke();
    }

    /// <summary>Releases the mapped backing (view + mapping). No-op for heap buffers and for slices.</summary>
    public void Dispose()
    {
        _owner?.Dispose();
    }

    // --- factories ---

    public static ByteBuffer Allocate(int capacity)
    {
        return new ByteBuffer(new byte[capacity], null, null);
    }

    public static ByteBuffer Wrap(byte[] array)
    {
        return new ByteBuffer(array, null, null);
    }

    /// <summary>
    /// Wraps an arbitrary <see cref="System.Memory{Byte}"/> window. For a mapped backing, pass the
    /// <paramref name="owner"/> (disposed to unmap) and an <paramref name="onForce"/> delegate
    /// (the view's flush). Heap buffers pass neither.
    /// </summary>
    public static ByteBuffer Of(Memory<byte> memory, IDisposable? owner = null, Action? onForce = null)
    {
        return new ByteBuffer(memory, owner, onForce);
    }
}
