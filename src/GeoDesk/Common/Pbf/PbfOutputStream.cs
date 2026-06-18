/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Text;

using Java.Nio;

namespace GeoDesk.Common.Pbf;

// In Java this extends ByteArrayOutputStream; the straight port extends MemoryStream.
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream</c>.</remarks>
internal class PbfOutputStream : MemoryStream
{

    // check encoding of negative varints
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeVarint(long)</c>.</remarks>
    public void WriteVarint(long val)
    {
        while (val >= 0x80 || val < 0) // TODO: improve check?
        {
            WriteByte((byte)((val & 0x7f) | 0x80));
            val = (long)((ulong)val >> 7);
        }
        WriteByte((byte)val);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeSignedVarint(long)</c>.</remarks>
    public void WriteSignedVarint(long val)
    {
        WriteVarint((val << 1) ^ (val >> 63));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(String)</c>.</remarks>
    public void WriteString(string val)
    {
        var bytes = Encoding.UTF8.GetBytes(val);
        WriteVarint(bytes.Length);
        Write(bytes, 0, bytes.Length);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(PbfOutputStream)</c>.</remarks>
    public void WriteString(PbfOutputStream other)
    {
        var count = (int)other.Length;
        WriteVarint(count);
        Write(other.GetBuffer(), 0, count);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(byte[])</c>.</remarks>
    public void WriteString(byte[] bytes)
    {
        WriteVarint(bytes.Length);
        Write(bytes, 0, bytes.Length);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(byte[], int, int)</c>.</remarks>
    public void WriteString(byte[] bytes, int start, int len)
    {
        WriteVarint(len);
        Write(bytes, start, len);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeMessage(int, ByteArrayOutputStream)</c>.</remarks>
    public void WriteMessage(int tag, MemoryStream other)
    {
        WriteVarint(tag);
        WriteVarint(other.Length);
        try
        {
            other.WriteTo(this);
        }
        catch (IOException ex)
        {
            throw new PbfException("Writing failed.", ex);
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeFixed32(int)</c>.</remarks>
    public void WriteFixed32(int val)
    {
        WriteByte((byte)(val & 0xff));
        WriteByte((byte)((val >> 8) & 0xff));
        WriteByte((byte)((val >> 16) & 0xff));
        WriteByte((byte)((val >> 24) & 0xff));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeFixed64(long)</c>.</remarks>
    public void WriteFixed64(long val)
    {
        WriteFixed32((int)val);
        WriteFixed32((int)((ulong)val >> 32));
    }

    // TODO: check
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeFloat(float)</c>.</remarks>
    public void WriteFloat(float val)
    {
        WriteFixed32(BitConverter.SingleToInt32Bits(val));
    }

    // TODO: check
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeDouble(double)</c>.</remarks>
    public void WriteDouble(double val)
    {
        WriteFixed64(BitConverter.DoubleToInt64Bits(val));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeTo(ByteBuffer)</c>.</remarks>
    public void WriteTo(ByteBuffer @out)
    {
        @out.Put(GetBuffer(), 0, (int)Length);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.buffer()</c>.</remarks>
    public byte[] Buffer()
    {
        return GetBuffer();
    }

}
