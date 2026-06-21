/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Text;

namespace GeoDesk.Common.Pbf;

// In Java this extends ByteArrayOutputStream; the straight port extends MemoryStream.
/// <summary>
/// A growable in-memory output stream that encodes Protocol Buffers (PBF) primitives: varints,
/// zig-zag signed varints, length-prefixed strings and byte spans, nested messages, fixed 32- and
/// 64-bit integers, and floats and doubles. Backed by a <see cref="MemoryStream"/>.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream</c>.</remarks>
internal class PbfOutputStream : MemoryStream
{

    // check encoding of negative varints
    /// <summary>
    /// Encodes <paramref name="val"/> as a variable-length integer and appends it to the stream.
    /// </summary>
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

    /// <summary>
    /// Zig-zag encodes <paramref name="val"/> and writes it as a varint, so small-magnitude negatives
    /// encode compactly.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeSignedVarint(long)</c>.</remarks>
    public void WriteSignedVarint(long val)
    {
        WriteVarint((val << 1) ^ (val >> 63));
    }

    /// <summary>
    /// Writes a string as its UTF-8 byte length (varint) followed by the UTF-8 bytes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(String)</c>.</remarks>
    public void WriteString(string val)
    {
        var bytes = Encoding.UTF8.GetBytes(val);
        WriteVarint(bytes.Length);
        Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes the contents of another <see cref="PbfOutputStream"/> as a length-prefixed byte span.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(PbfOutputStream)</c>.</remarks>
    public void WriteString(PbfOutputStream other)
    {
        var count = (int)other.Length;
        WriteVarint(count);
        Write(other.GetBuffer(), 0, count);
    }

    /// <summary>
    /// Writes a byte array as a length-prefixed (varint) byte span.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(byte[])</c>.</remarks>
    public void WriteString(byte[] bytes)
    {
        WriteVarint(bytes.Length);
        Write(bytes, 0, bytes.Length);
    }

    /// <summary>
    /// Writes a sub-range of a byte array as a length-prefixed (varint) byte span.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeString(byte[], int, int)</c>.</remarks>
    public void WriteString(byte[] bytes, int start, int len)
    {
        WriteVarint(len);
        Write(bytes, start, len);
    }

    /// <summary>
    /// Writes a nested message: the field tag (varint), the message length (varint), then the bytes
    /// of <paramref name="other"/>. Wraps any I/O failure in a <see cref="PbfException"/>.
    /// </summary>
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

    /// <summary>
    /// Writes a 32-bit integer in fixed little-endian byte order.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeFixed32(int)</c>.</remarks>
    public void WriteFixed32(int val)
    {
        WriteByte((byte)(val & 0xff));
        WriteByte((byte)((val >> 8) & 0xff));
        WriteByte((byte)((val >> 16) & 0xff));
        WriteByte((byte)((val >> 24) & 0xff));
    }

    /// <summary>
    /// Writes a 64-bit integer in fixed little-endian byte order as two fixed 32-bit halves.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeFixed64(long)</c>.</remarks>
    public void WriteFixed64(long val)
    {
        WriteFixed32((int)val);
        WriteFixed32((int)((ulong)val >> 32));
    }

    // TODO: check
    /// <summary>
    /// Writes a single-precision float as its fixed 32-bit IEEE-754 bit pattern.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeFloat(float)</c>.</remarks>
    public void WriteFloat(float val)
    {
        WriteFixed32(BitConverter.SingleToInt32Bits(val));
    }

    // TODO: check
    /// <summary>
    /// Writes a double-precision float as its fixed 64-bit IEEE-754 bit pattern.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.writeDouble(double)</c>.</remarks>
    public void WriteDouble(double val)
    {
        WriteFixed64(BitConverter.DoubleToInt64Bits(val));
    }

    /// <summary>
    /// Returns the underlying backing byte array, which may be larger than the written length.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfOutputStream.buffer()</c>.</remarks>
    public byte[] Buffer()
    {
        return GetBuffer();
    }

}
