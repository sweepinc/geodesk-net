/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

using GeoDesk.Buffers;

using Xunit;

namespace GeoDesk.Tests.Buffers;

// Validates the little-endian typed accessors that the cursor structs read/write through.
// Store data is always little-endian. (Replaces the former SegmentTest, whose typed accessors
// moved here when Segment became a pure mapped-window MemoryManager.)
public class BufferExtensionsTest
{

    [Fact]
    public void LittleEndianRoundTrip()
    {
        var b = new byte[64];

        b.AsSpan().PutIntLE(0, 0x11223344);
        Assert.Equal((byte)0x44, b[0]);
        Assert.Equal((byte)0x33, b[1]);
        Assert.Equal((byte)0x22, b[2]);
        Assert.Equal((byte)0x11, b[3]);
        Assert.Equal(0x11223344, b.AsSpan().GetIntLE(0));

        b.AsSpan().PutLongLE(8, 0x0102030405060708L);
        Assert.Equal(0x0102030405060708L, b.AsSpan().GetLongLE(8));

        b.AsSpan().PutShortLE(16, unchecked((short)0xBEEF));
        Assert.Equal(unchecked((short)0xBEEF), b.AsSpan().GetShortLE(16));
        Assert.Equal((char)0xBEEF, b.AsSpan().GetCharLE(16));
    }

    [Fact]
    public void ReadOnlyAndWritableSpansAgree()
    {
        var b = new byte[8];
        b.AsSpan().PutIntLE(0, -123456);

        // the read accessors are available on both Span<byte> and ReadOnlySpan<byte>
        Assert.Equal(-123456, b.AsSpan().GetIntLE(0));
        Assert.Equal(-123456, ((System.ReadOnlySpan<byte>)b).GetIntLE(0));
    }

}
