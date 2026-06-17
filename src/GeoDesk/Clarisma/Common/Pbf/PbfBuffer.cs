/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Clarisma.Common.Pbf;

// TODO: unify these under an interface, so we can read ByteBuffer, byte array
//  and InputStream the same way

// TODO: maybe maintain start pos so we can reset the buffer?
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer</c>.</remarks>
public class PbfBuffer
{
    protected byte[]? buf;
    protected int pos;
    protected int endPos;

    public static readonly PbfBuffer Empty = new PbfBuffer();

    public PbfBuffer()
    {
        buf = null;
        pos = 0;
        endPos = 0;
    }

    public PbfBuffer(byte[] data)
    {
        Wrap(data);
    }

    public PbfBuffer(byte[] data, int start, int len)
    {
        buf = data;
        pos = start;
        endPos = start + len;
    }

    public void Wrap(byte[] data)
    {
        buf = data;
        pos = 0;
        endPos = data.Length;
    }

    public byte[]? Buf => buf;

    public int Pos => pos;

    public int BytesRemaining => endPos - pos;

    public int EndPos => endPos;

    public bool Load(Stream @in, int len)
    {
        buf = new byte[len];
        pos = 0;
        endPos = len;
        try
        {
            int bytesRead = @in.Read(buf, 0, len);
            return bytesRead == len;
        }
        catch (IOException ex)
        {
            throw new PbfException("Unable to load buffer", ex);
        }
    }

    // TODO: does not respect the original window
    public void Reset(byte[] data)
    {
        buf = data;
        pos = 0;
        endPos = data.Length;
    }

    public byte ReadByte()
    {
        try
        {
            return buf![pos++];
        }
        catch (IndexOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

    public int ReadTag()
    {
        return (int)ReadVarint();
    }

    public long ReadVarint()
    {
        Debug.Assert(pos < endPos);
        sbyte b;
        long val;
        try
        {
            b = (sbyte)buf![pos++];
            val = b & 0x7f;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 7;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 14;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 21;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 28;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 35;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 42;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 49;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 56;
            if (b >= 0) return val;
            b = (sbyte)buf[pos++];
            val |= (long)(b & 0x7f) << 63;
            if (b >= 0) return val;
            throw new PbfException("Bad VarInt format.");
        }
        catch (IndexOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

    /// <summary>
    /// Counts the number of varint values from the current buffer
    /// position until the end of the buffer. The current position
    /// remains unchanged. Useful when allocating an array prior
    /// to reading a series of values.
    /// </summary>
    /// <returns>the number of varints</returns>
    public int CountVarInts()
    {
        return CountVarInts(pos);
    }

    public int CountVarInts(int start)
    {
        int count = 0;
        for (int i = start; i < endPos; i++)
        {
            if ((sbyte)buf![i] >= 0) count++;
        }
        return count;
    }

    // TODO: can this fail if a non-canonical encoding is used?
    //  e.g. 0x81 0x00 instead of 0x01
    public int CountVarIntsUntilZero(int start)
    {
        int count = 0;
        for (int i = start; i < endPos; i++)
        {
            if (buf![i] == 0) break;
            if ((sbyte)buf[i] >= 0) count++;
        }
        return count;
    }

    public long ReadSignedVarint()
    {
        long val = ReadVarint();
        return (val >> 1) ^ -(val & 1);
    }

    public int ReadFixed32()
    {
        try
        {
            int val = (buf![pos] & 0xff) |
                ((buf[pos + 1] & 0xff) << 8) |
                ((buf[pos + 2] & 0xff) << 16) |
                ((buf[pos + 3] & 0xff) << 24);
            pos += 4;
            return val;
        }
        catch (IndexOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

    public long ReadFixed64()
    {
        return ((long)ReadFixed32() & 0xffff_ffffL) |
            ((long)ReadFixed32() << 32);
    }

    public float ReadFloat()
    {
        return BitConverter.Int32BitsToSingle(ReadFixed32());
    }

    public double ReadDouble()
    {
        double d = BitConverter.Int64BitsToDouble(ReadFixed64());
        return d;
    }

    /// <summary>
    /// Reads a 32-bit integer in network byte order
    /// (big endian) -- this is not how fixed 32-bit values
    /// are encoded, use readFixed32 instead
    ///
    /// TODO: move out of this class, used only by OsmReader
    /// </summary>
    public int ReadFixedIntNbo()
    {
        try
        {
            int val = ((sbyte)buf![0] << 24) | ((sbyte)buf[1] << 16) | ((sbyte)buf[2] << 8) | (sbyte)buf[3];
            pos += 4;
            return val;
        }
        catch (IndexOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

    public string ReadString()
    {
        int len = (int)ReadVarint();
        return ReadString(len);
    }

    public string ReadString(int len)
    {
        string val;
        try
        {
            val = Encoding.UTF8.GetString(buf!, pos, len);
        }
        catch (Exception ex)
        {
            throw new PbfException("Unable to read string.", ex);
        }
        pos += len;
        return val;
    }

    public bool HasMore()
    {
        return pos < endPos;
    }

    public void Skip(int len)
    {
        pos += len;
    }

    public void SkipEntity(int marker)
    {
        switch (marker & 7)
        {
            case PbfType.Varint:
                ReadVarint();
                break;
            case PbfType.Fixed64:
                Skip(8);
                break;
            case PbfType.String:
                int len = (int)ReadVarint();
                Skip(len);
                break;
            case PbfType.Fixed32:
                Skip(4);
                break;
            default:
                throw new PbfException("Unknown type: " + (marker & 7));
        }
    }

    public void End()
    {
        pos = endPos;
    }

    public void Dump(int from, int to)
    {
        StringBuilder b = new StringBuilder();
        StringBuilder bHex = new StringBuilder();
        for (int i = from; i < buf!.Length && i < to; i++)
        {
            b.Append(" ");
            b.Append((buf[i] & 0xff).ToString());
            bHex.Append(" ");
            bHex.Append((buf[i] & 0xff).ToString("x"));
        }
        Console.WriteLine("Remaining bytes: " + b.ToString());
        Console.WriteLine("         as hex: " + bHex.ToString());
    }

    public PbfBuffer ReadMessage()
    {
        int len = (int)ReadVarint();
        PbfBuffer msg = new PbfBuffer(buf!, pos, len);
        pos += len;
        return msg;
    }

    public void Seek(int newPos)
    {
        pos = newPos;
    }

    public int Peek()
    {
        if (pos < endPos) return buf![pos] & 0xff;
        return 0;
    }

    public int GetByte(int p)
    {
        return buf![p] & 0xff;
    }
}
