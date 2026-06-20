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
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder</c>.</remarks>
internal class PbfDecoder
{

    readonly NioBufferReader _buf;
    int _pos;

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder(ByteBuffer, int)</c>.</remarks>
    public PbfDecoder(NioBufferReader buf, int pos)
    {
        _buf = buf;
        _pos = pos;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.pos()</c>.</remarks>
    public int Pos => _pos;

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

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readSignedVarint(InputStream)</c>.</remarks>
    public static long ReadSignedVarint(Stream @in)
    {
        var val = ReadVarint(@in);
        return (val >> 1) ^ -(val & 1);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readSignedVarint()</c>.</remarks>
    public long ReadSignedVarint()
    {
        var val = ReadVarint();
        return (val >> 1) ^ -(val & 1);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readRawString(int)</c>.</remarks>
    public string ReadRawString(int len)
    {
        var bytes = new byte[len];
        _buf.Get(_pos, bytes);
        _pos += len;
        return Encoding.UTF8.GetString(bytes);
    }

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

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readFixed32()</c>.</remarks>
    public int ReadFixed32()
    {
        var v = _buf.GetInt(_pos);
        _pos += 4;
        return v;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readByte()</c>.</remarks>
    public byte ReadByte()
    {
        var b = _buf.Get(_pos);
        _pos++;
        return b;
    }

    // TODO: improve
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.equalsString(String)</c>.</remarks>
    public bool EqualsString(string s)
    {
        return ReadString().Equals(s);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.skip(int)</c>.</remarks>
    public void Skip(int len)
    {
        _pos += len;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.seek(int)</c>.</remarks>
    public void Seek(int newPos)
    {
        _pos = newPos;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.hasMore()</c>.</remarks>
    public bool HasMore()
    {
        return _pos < _buf.Length;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.buf()</c>.</remarks>
    public NioBufferReader Buf()
    {
        return _buf;
    }

}
