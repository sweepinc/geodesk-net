/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers.Binary;

namespace Java.Nio;

/// <summary>
/// A faithful subset of <c>java.nio.ByteBuffer</c>: an indexable region of bytes with a
/// position, limit, capacity and configurable byte order. Supports absolute and relative
/// get/put of bytes and 16/32/64-bit integers, plus slice/duplicate.
///
/// Concrete backings: <see cref="HeapByteBuffer"/> (byte[]) and
/// <see cref="MappedByteBuffer"/> (memory-mapped file segment).
///
/// Only the operations used by the GeoDesk port are implemented; this is not a complete
/// reimplementation of java.nio.
/// </summary>
public abstract class ByteBuffer
{
    protected int position;
    protected int limit;
    protected int capacity;
    protected ByteOrder order = ByteOrder.BigEndian; // Java's default

    // --- backing primitives (implemented by subclasses) ---

    public abstract byte Get(int index);
    public abstract ByteBuffer Put(int index, byte b);
    protected abstract void GetBytes(int index, Span<byte> dst);
    protected abstract void PutBytes(int index, ReadOnlySpan<byte> src);

    /// <summary>The backing array (heap buffers only); null for mapped buffers.</summary>
    public abstract byte[]? Array();

    public abstract ByteBuffer Slice();
    /// <summary>Absolute slice (Java 13+): a buffer over [index, index+length).</summary>
    public abstract ByteBuffer Slice(int index, int length);
    public abstract ByteBuffer Duplicate();

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

    // --- factories ---

    public static ByteBuffer Allocate(int capacity)
    {
        return new HeapByteBuffer(new byte[capacity], 0, capacity);
    }

    public static ByteBuffer Wrap(byte[] array)
    {
        return new HeapByteBuffer(array, 0, array.Length);
    }
}
