/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Text;

using GeoDesk.Buffers;

namespace GeoDesk.Common.Pbf;

// static methods are not threadsafe

// TODO: unify these under an interface, so we can read NioBufferReader, byte array
//  and InputStream the same way
/// <summary>
/// Sequential reader for Protocol Buffers (PBF) data over a <see cref="NioBufferReader"/>, tracking
/// a current position and decoding varints, signed (zig-zag) varints, fixed-width integers, and
/// length-prefixed strings. The instance methods advance the cursor; the static overloads decode
/// directly from a stream.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder</c>.</remarks>
internal class PbfDecoder
{

    readonly NioBufferReader _buf;
    int _pos;

    /// <summary>
    /// Creates a decoder reading from the given buffer starting at the given position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder(ByteBuffer, int)</c>.</remarks>
    public PbfDecoder(NioBufferReader buf, int pos)
    {
        _buf = buf;
        _pos = pos;
    }

    /// <summary>
    /// The current read position within the buffer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.pos()</c>.</remarks>
    public int Pos => _pos;

    /// <summary>
    /// Reads a variable-length unsigned integer from the current position, advancing the cursor.
    /// Throws <see cref="PbfException"/> on a malformed varint or read past the buffer end.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readVarint()</c>.</remarks>
    public long ReadVarint()
    {
        sbyte b;
        long val;
        try
        {
            b = (sbyte)_buf.Get(_pos++);
            val = b & 0x7f;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 7;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 14;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 21;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 28;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 35;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 42;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 49;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 56;
            if (b >= 0) return val;
            b = (sbyte)_buf.Get(_pos++);
            val |= (long)(b & 0x7f) << 63;
            if (b >= 0) return val;
            throw new PbfException("Bad VarInt format.");
        }
        catch (IndexOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

    /// <summary>
    /// Reads a variable-length unsigned integer directly from a stream, throwing
    /// <see cref="PbfException"/> on a malformed varint.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readVarint(InputStream)</c>.</remarks>
    public static long ReadVarint(Stream @in)
    {
        sbyte b;
        long val;
        b = (sbyte)@in.ReadByte();
        val = b & 0x7f;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 7;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 14;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 21;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 28;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 35;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 42;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 49;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 56;
        if (b >= 0) return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 63;
        if (b >= 0) return val;
        throw new PbfException("Bad VarInt format.");
    }

    /// <summary>
    /// Reads a zig-zag-encoded signed varint directly from a stream and decodes it back to a signed
    /// value.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readSignedVarint(InputStream)</c>.</remarks>
    public static long ReadSignedVarint(Stream @in)
    {
        var val = ReadVarint(@in);
        return (val >> 1) ^ -(val & 1);
    }

    /// <summary>
    /// Reads a zig-zag-encoded signed varint from the current position and decodes it back to a
    /// signed value, advancing the cursor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readSignedVarint()</c>.</remarks>
    public long ReadSignedVarint()
    {
        var val = ReadVarint();
        return (val >> 1) ^ -(val & 1);
    }

    /// <summary>
    /// Reads exactly <paramref name="len"/> bytes from the current position and decodes them as a
    /// UTF-8 string, advancing the cursor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readRawString(int)</c>.</remarks>
    public string ReadRawString(int len)
    {
        var bytes = new byte[len];
        _buf.Get(_pos, bytes);
        _pos += len;
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string: a varint byte count followed by that many UTF-8 bytes,
    /// advancing the cursor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readString()</c>.</remarks>
    public string ReadString()
    {
        var len = (int)ReadVarint();
        var bytes = new byte[len];
        for (var i = 0; i < len; i++)
        {
            bytes[i] = _buf.Get(_pos++);
        }
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Reads a fixed-width 32-bit integer from the current position, advancing the cursor by 4 bytes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readFixed32()</c>.</remarks>
    public int ReadFixed32()
    {
        var v = _buf.GetInt(_pos);
        _pos += 4;
        return v;
    }

    /// <summary>
    /// Reads a single byte from the current position, advancing the cursor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readByte()</c>.</remarks>
    public byte ReadByte()
    {
        var b = _buf.Get(_pos);
        _pos++;
        return b;
    }

    // TODO: improve
    /// <summary>
    /// Reads the next length-prefixed string and returns whether it equals <paramref name="s"/>,
    /// advancing the cursor past it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.equalsString(String)</c>.</remarks>
    public bool EqualsString(string s)
    {
        return ReadString().Equals(s);
    }

    /// <summary>
    /// Advances the cursor forward by <paramref name="len"/> bytes without reading them.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.skip(int)</c>.</remarks>
    public void Skip(int len)
    {
        _pos += len;
    }

    /// <summary>
    /// Moves the cursor to the given absolute position within the buffer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.seek(int)</c>.</remarks>
    public void Seek(int newPos)
    {
        _pos = newPos;
    }

    /// <summary>
    /// True if the cursor has not yet reached the end of the buffer.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.hasMore()</c>.</remarks>
    public bool HasMore()
    {
        return _pos < _buf.Length;
    }

    /// <summary>
    /// Returns the underlying buffer this decoder reads from.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.buf()</c>.</remarks>
    public NioBufferReader Buf()
    {
        return _buf;
    }

}
