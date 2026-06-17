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

namespace Clarisma.Common.Pbf;

// static methods are not threadsafe

// TODO: unify these under an interface, so we can read ByteBuffer, byte array
//  and InputStream the same way
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder</c>.</remarks>
public class PbfDecoder
{
    private readonly ByteBuffer buf;
    private int pos;

    public PbfDecoder(ByteBuffer buf, int pos)
    {
        this.buf = buf;
        this.pos = pos;
    }

    public int Pos => pos;

    public long ReadVarint()
    {
        sbyte b;
        long val;
        try
        {
            b = (sbyte)buf.Get(pos++);
            val = b & 0x7f;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 7;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 14;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 21;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 28;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 35;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 42;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 49;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
            val |= (long)(b & 0x7f) << 56;
            if (b >= 0) return val;
            b = (sbyte)buf.Get(pos++);
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

    public static long ReadVarint(ByteBuffer buf)
    {
        sbyte b;
        long val;
        try
        {
            b = (sbyte)buf.Get();
            val = b & 0x7f;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 7;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 14;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 21;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 28;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 35;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 42;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 49;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 56;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (long)(b & 0x7f) << 63;
            if (b >= 0) return val;
            throw new PbfException("Bad VarInt format.");
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

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

    public static long ReadSignedVarint(Stream @in)
    {
        long val = ReadVarint(@in);
        return (val >> 1) ^ -(val & 1);
    }

    public static int ReadVarintSmall(ByteBuffer buf)
    {
        sbyte b;
        int val;
        try
        {
            b = (sbyte)buf.Get();
            val = b & 0x7f;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (b & 0x7f) << 7;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (b & 0x7f) << 14;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (b & 0x7f) << 21;
            if (b >= 0) return val;
            b = (sbyte)buf.Get();
            val |= (b & 0x7f) << 28;
            if (b >= 0) return val;
            throw new PbfException("Bad VarInt format.");
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

    public long ReadSignedVarint()
    {
        long val = ReadVarint();
        return (val >> 1) ^ -(val & 1);
    }

    public static long ReadSignedVarint(ByteBuffer buf)
    {
        long val = ReadVarint(buf);
        return (val >> 1) ^ -(val & 1);
    }

    public string ReadRawString(int len)
    {
        buf.Position(pos);
        byte[] bytes = new byte[len];
        buf.Get(bytes);
        pos += len;
        return Encoding.UTF8.GetString(bytes);
    }

    public string ReadString()
    {
        int len = (int)ReadVarint();
        byte[] bytes = new byte[len];
        for (int i = 0; i < len; i++)
        {
            bytes[i] = buf.Get(pos++);
        }
        return Encoding.UTF8.GetString(bytes);
    }

    public int ReadFixed32()
    {
        int v = buf.GetInt(pos);
        pos += 4;
        return v;
    }

    public byte ReadByte()
    {
        byte b = buf.Get(pos);
        pos++;
        return b;
    }

    public static string ReadString(ByteBuffer buf)
    {
        int len = (int)ReadVarint(buf);
        byte[] bytes = new byte[len];
        buf.Get(bytes);
        return Encoding.UTF8.GetString(bytes);
    }

    // TODO: improve
    public bool EqualsString(string s)
    {
        return ReadString().Equals(s);
    }

    public void Skip(int len)
    {
        pos += len;
    }

    public void Seek(int newPos)
    {
        pos = newPos;
    }

    public bool HasMore()
    {
        return pos < buf.Limit();
    }

    public ByteBuffer Buf()
    {
        return buf;
    }
}
