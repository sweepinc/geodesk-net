/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Nio;

/// <summary>
/// A <see cref="ByteBuffer"/> backed by a managed <c>byte[]</c>.
/// </summary>
public sealed class HeapByteBuffer : ByteBuffer
{
    private readonly byte[] hb;
    private readonly int offset;

    public HeapByteBuffer(byte[] hb, int offset, int capacity)
    {
        this.hb = hb;
        this.offset = offset;
        this.capacity = capacity;
        limit = capacity;
        position = 0;
    }

    public override byte Get(int index)
    {
        return hb[offset + index];
    }

    public override ByteBuffer Put(int index, byte b)
    {
        hb[offset + index] = b;
        return this;
    }

    protected override void GetBytes(int index, Span<byte> dst)
    {
        new ReadOnlySpan<byte>(hb, offset + index, dst.Length).CopyTo(dst);
    }

    protected override void PutBytes(int index, ReadOnlySpan<byte> src)
    {
        src.CopyTo(new Span<byte>(hb, offset + index, src.Length));
    }

    public override byte[] Array()
    {
        return hb;
    }

    public override ByteBuffer Slice()
    {
        var s = new HeapByteBuffer(hb, offset + position, limit - position);
        s.Order(order);
        return s;
    }

    public override ByteBuffer Slice(int index, int length)
    {
        var s = new HeapByteBuffer(hb, offset + index, length);
        s.Order(order);
        return s;
    }

    public override ByteBuffer Duplicate()
    {
        var d = new HeapByteBuffer(hb, offset, capacity);
        d.position = position;
        d.limit = limit;
        d.Order(order);
        return d;
    }
}
