/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Xunit;

namespace Java.Nio;

// Validates the ByteBuffer abstraction introduced for the .NET port (no direct
// Java counterpart test).
public class ByteBufferTest
{

    [Fact]
    public void LittleEndianRoundTrip()
    {
        ByteBuffer buf = ByteBuffer.Allocate(64).Order(ByteOrder.LittleEndian);
        buf.PutInt(0, 0x11223344);
        Assert.Equal(0x44, buf.Get(0));
        Assert.Equal(0x33, buf.Get(1));
        Assert.Equal(0x22, buf.Get(2));
        Assert.Equal(0x11, buf.Get(3));
        Assert.Equal(0x11223344, buf.GetInt(0));

        buf.PutLong(8, 0x0102030405060708L);
        Assert.Equal(0x0102030405060708L, buf.GetLong(8));

        buf.PutShort(16, unchecked((short)0xBEEF));
        Assert.Equal(unchecked((short)0xBEEF), buf.GetShort(16));
        Assert.Equal((char)0xBEEF, buf.GetChar(16));
    }

    [Fact]
    public void BigEndianRoundTrip()
    {
        ByteBuffer buf = ByteBuffer.Allocate(16); // default big-endian
        buf.PutInt(0, 0x11223344);
        Assert.Equal(0x11, buf.Get(0));
        Assert.Equal(0x44, buf.Get(3));
        Assert.Equal(0x11223344, buf.GetInt(0));
    }

    [Fact]
    public void RelativePositionAdvances()
    {
        ByteBuffer buf = ByteBuffer.Allocate(16).Order(ByteOrder.LittleEndian);
        buf.PutInt(123);
        buf.PutInt(456);
        Assert.Equal(8, buf.Position());
        buf.Flip();
        Assert.Equal(123, buf.GetInt());
        Assert.Equal(456, buf.GetInt());
        Assert.False(buf.HasRemaining());
    }

    [Fact]
    public void SliceSharesContent()
    {
        ByteBuffer buf = ByteBuffer.Allocate(16).Order(ByteOrder.LittleEndian);
        buf.PutInt(0, 1);
        buf.PutInt(4, 2);
        buf.Position(4);
        ByteBuffer slice = buf.Slice();
        Assert.Equal(2, slice.GetInt(0));
        slice.PutInt(0, 99);
        Assert.Equal(99, buf.GetInt(4)); // shared backing
    }

    [Fact]
    public void WrapExposesArray()
    {
        byte[] data = { 1, 2, 3, 4 };
        ByteBuffer buf = ByteBuffer.Wrap(data);
        Assert.Same(data, buf.Array());
        Assert.Equal(4, buf.Capacity());
    }

}
