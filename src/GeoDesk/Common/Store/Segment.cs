/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace GeoDesk.Common.Store;

/// <summary>
/// A little-endian byte window over a <see cref="System.Memory{Byte}"/> region, addressed by absolute
/// offset. This is the unit the store maps the file in — one <c>Segment</c> per 1 GB mapped view (the
/// dominant case) — and also backs heap staging buffers (<see cref="Allocate"/>/<see cref="Wrap"/>)
/// and slices. Supports absolute get/put of bytes and 16/32/64-bit integers, plus bulk copy and slice.
///
/// Store data is always little-endian, so the typed accessors read/write little-endian
/// unconditionally. (The crash-recovery journal is big-endian, but it never goes through
/// <c>Segment</c> — see <c>Store.JournalReadInt/JournalWriteInt</c>.)
///
/// Backed by <see cref="System.Memory{Byte}"/>: heap buffers wrap a managed <c>byte[]</c>; mapped
/// segments wrap a memory-mapped file view exposed via a <see cref="System.Buffers.MemoryManager{Byte}"/>.
/// A mapped segment is given the manager as its <see cref="IDisposable"/> owner (deterministic unmap)
/// and a flush delegate for <see cref="Force"/>; heap buffers and slices have neither.
/// </summary>
/// <remarks>Originated as a port of <c>java.nio.ByteBuffer</c> (the store's <c>MappedByteBuffer</c>);
/// renamed to <c>Segment</c> and reduced to absolute-offset access (the java.nio position/limit cursor
/// was dropped — the read path never used it, and its few write/export callers were moved to absolute
/// access). Heap/slice roles are slated to move to <see cref="System.Memory{Byte}"/> as the migration
/// continues.</remarks>
internal sealed class Segment : IDisposable
{

    public static Segment Allocate(int capacity)
    {
        return new Segment(new byte[capacity], null, null);
    }

    public static Segment Wrap(byte[] array)
    {
        return new Segment(array, null, null);
    }

    /// <summary>
    /// Wraps an arbitrary <see cref="System.Memory{Byte}"/> window. For a mapped backing, pass the
    /// <paramref name="owner"/> (disposed to unmap) and an <paramref name="onForce"/> delegate
    /// (the view's flush). Heap buffers pass neither.
    /// </summary>
    public static Segment Of(Memory<byte> memory, IDisposable? owner = null, Action? onForce = null)
    {
        return new Segment(memory, owner, onForce);
    }

    readonly Memory<byte> _mem;
    readonly IDisposable? _owner;
    readonly Action? _onForce;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="mem"></param>
    /// <param name="owner"></param>
    /// <param name="onForce"></param>
    Segment(Memory<byte> mem, IDisposable? owner, Action? onForce)
    {
        _mem = mem;
        _owner = owner;
        _onForce = onForce;
    }

    // --- backing primitives (over the Memory<byte> window) ---

    /// <summary>The underlying memory window, as a read-only view (for generic byte consumers, e.g. PbfDecoder).</summary>
    public ReadOnlyMemory<byte> Memory => _mem;

    /// <summary>The length of the window, in bytes.</summary>
    public int Capacity()
    {
        return _mem.Length;
    }

    public byte Get(int index)
    {
        return _mem.Span[index];
    }

    public Segment Put(int index, byte b)
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

    /// <summary>
    /// Tells whether this buffer is backed by an accessible byte array.
    /// </summary>
    /// <remarks>Mirrors <c>java.nio.ByteBuffer.hasArray()</c>: true for heap buffers, false for mapped.</remarks>
    public bool HasArray()
    {
        return MemoryMarshal.TryGetArray<byte>(_mem, out _);
    }

    /// <summary>
    /// Returns the byte array that backs this buffer (heap buffers only).
    /// </summary>
    /// <remarks>Mirrors <c>java.nio.ByteBuffer.array()</c>: throws <see cref="NotSupportedException"/>
    /// (Java's <c>UnsupportedOperationException</c>) if this buffer is not backed by an accessible
    /// array — e.g. a mapped segment. Guard with <see cref="HasArray"/>.</remarks>
    public byte[] Array()
    {
        if (MemoryMarshal.TryGetArray<byte>(_mem, out var seg))
            return seg.Array!;
        throw new NotSupportedException();
    }

    /// <summary>Absolute slice: a window over [index, index+length).</summary>
    public Segment Slice(int index, int length)
    {
        return new Segment(_mem.Slice(index, length), null, null);
    }

    // --- absolute typed accessors (little-endian) ---

    public int GetInt(int index)
    {
        Span<byte> s = stackalloc byte[4];
        GetBytes(index, s);
        return BinaryPrimitives.ReadInt32LittleEndian(s);
    }

    public Segment PutInt(int index, int value)
    {
        Span<byte> s = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    public long GetLong(int index)
    {
        Span<byte> s = stackalloc byte[8];
        GetBytes(index, s);
        return BinaryPrimitives.ReadInt64LittleEndian(s);
    }

    public Segment PutLong(int index, long value)
    {
        Span<byte> s = stackalloc byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    public short GetShort(int index)
    {
        Span<byte> s = stackalloc byte[2];
        GetBytes(index, s);
        return BinaryPrimitives.ReadInt16LittleEndian(s);
    }

    public Segment PutShort(int index, short value)
    {
        Span<byte> s = stackalloc byte[2];
        BinaryPrimitives.WriteInt16LittleEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    public char GetChar(int index)
    {
        Span<byte> s = stackalloc byte[2];
        GetBytes(index, s);
        return (char)BinaryPrimitives.ReadUInt16LittleEndian(s);
    }

    public Segment PutChar(int index, char value)
    {
        Span<byte> s = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(s, value);
        PutBytes(index, s);
        return this;
    }

    // --- absolute bulk copy ---

    /// <summary>Absolute bulk get (Java 13+).</summary>
    public Segment Get(int index, byte[] dst)
    {
        GetBytes(index, dst.AsSpan());
        return this;
    }

    /// <summary>Absolute bulk get (Java 13+).</summary>
    public Segment Get(int index, byte[] dst, int offset, int length)
    {
        GetBytes(index, dst.AsSpan(offset, length));
        return this;
    }

    /// <summary>Absolute bulk put (Java 13+).</summary>
    public Segment Put(int index, byte[] src)
    {
        PutBytes(index, src.AsSpan());
        return this;
    }

    /// <summary>Absolute bulk put (Java 13+).</summary>
    public Segment Put(int index, byte[] src, int offset, int length)
    {
        PutBytes(index, src.AsSpan(offset, length));
        return this;
    }

    /// <summary>Absolute bulk put from another segment (Java 16+).</summary>
    public Segment Put(int index, Segment src, int srcIndex, int length)
    {
        byte[] tmp = new byte[length];
        src.GetBytes(srcIndex, tmp);
        PutBytes(index, tmp);
        return this;
    }

    // --- mapped-backing lifetime (no-op for heap buffers) ---

    /// <summary>Flushes a mapped segment's pending writes to disk (Java's <c>MappedByteBuffer.force()</c>).</summary>
    public void Force()
    {
        _onForce?.Invoke();
    }

    /// <summary>Releases the mapped backing (view + mapping). No-op for heap buffers and for slices.</summary>
    public void Dispose()
    {
        _owner?.Dispose();
    }

}
