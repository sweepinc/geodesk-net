/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO.MemoryMappedFiles;

namespace Java.Nio;

/// <summary>
/// A <see cref="ByteBuffer"/> backed by a memory-mapped file segment — the .NET stand-in
/// for Java's <c>MappedByteBuffer</c>. Backed by a <see cref="MemoryMappedViewAccessor"/>
/// and accessed via an acquired raw pointer.
///
/// In Java, mapped buffers are unmapped non-deterministically (or via Unsafe.invokeCleaner);
/// here, <see cref="Dispose"/> releases the pointer and disposes the accessor deterministically.
/// </summary>
public sealed unsafe class MappedByteBuffer : ByteBuffer, IDisposable
{
    private readonly MemoryMappedViewAccessor accessor;
    private readonly byte* basePtr;
    private readonly int dataOffset;
    private readonly bool owns;
    private bool released;

    public MappedByteBuffer(MemoryMappedViewAccessor accessor, int capacity)
    {
        this.accessor = accessor;
        byte* ptr = null;
        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        basePtr = ptr + accessor.PointerOffset;
        dataOffset = 0;
        owns = true;
        this.capacity = capacity;
        limit = capacity;
        position = 0;
    }

    private MappedByteBuffer(MemoryMappedViewAccessor accessor, byte* basePtr, int dataOffset, int capacity)
    {
        this.accessor = accessor;
        this.basePtr = basePtr;
        this.dataOffset = dataOffset;
        owns = false;
        this.capacity = capacity;
        limit = capacity;
        position = 0;
    }

    public override byte Get(int index)
    {
        return basePtr[dataOffset + index];
    }

    public override ByteBuffer Put(int index, byte b)
    {
        basePtr[dataOffset + index] = b;
        return this;
    }

    protected override void GetBytes(int index, Span<byte> dst)
    {
        new ReadOnlySpan<byte>(basePtr + dataOffset + index, dst.Length).CopyTo(dst);
    }

    protected override void PutBytes(int index, ReadOnlySpan<byte> src)
    {
        src.CopyTo(new Span<byte>(basePtr + dataOffset + index, src.Length));
    }

    public override byte[]? Array()
    {
        return null; // mapped buffers have no backing array
    }

    public override ByteBuffer Slice()
    {
        var s = new MappedByteBuffer(accessor, basePtr, dataOffset + position, limit - position);
        s.Order(order);
        return s;
    }

    public override ByteBuffer Slice(int index, int length)
    {
        var s = new MappedByteBuffer(accessor, basePtr, dataOffset + index, length);
        s.Order(order);
        return s;
    }

    public override ByteBuffer Duplicate()
    {
        var d = new MappedByteBuffer(accessor, basePtr, dataOffset, capacity);
        d.position = position;
        d.limit = limit;
        d.Order(order);
        return d;
    }

    /// <summary>
    /// Forces any changes made to this buffer to be written to the underlying file.
    /// (Java's MappedByteBuffer.force())
    /// </summary>
    public void Force()
    {
        accessor.Flush();
    }

    public void Dispose()
    {
        if (owns && !released)
        {
            released = true;
            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            accessor.Dispose();
        }
    }
}
