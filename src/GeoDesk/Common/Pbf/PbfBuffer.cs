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

namespace GeoDesk.Common.Pbf;

// TODO: unify these under an interface, so we can read ByteBuffer, byte array
//  and InputStream the same way

// TODO: maybe maintain start pos so we can reset the buffer?
/// <summary>
/// A cursor over a byte-array window for reading Protocol Buffers (PBF) data. Tracks a current and
/// end position and decodes tags, varints, signed (zig-zag) varints, fixed 32/64-bit integers,
/// floats, doubles, and length-prefixed strings, as well as skipping fields and reading nested
/// messages. Reads past the window raise <see cref="PbfException"/>.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer</c>.</remarks>
internal class PbfBuffer
{

    protected byte[]? buf;
    protected int pos;
    protected int endPos;

    public static readonly PbfBuffer Empty = new PbfBuffer();

    /// <summary>
    /// Creates an empty buffer with no backing array; useful as a placeholder.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer()</c>.</remarks>
    public PbfBuffer()
    {
        buf = null;
        pos = 0;
        endPos = 0;
    }

    /// <summary>
    /// Creates a buffer wrapping the entire given byte array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer(byte[])</c>.</remarks>
    public PbfBuffer(byte[] data)
    {
        Wrap(data);
    }

    /// <summary>
    /// Creates a buffer over a window of the given byte array, starting at <paramref name="start"/>
    /// and spanning <paramref name="len"/> bytes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer(byte[], int, int)</c>.</remarks>
    public PbfBuffer(byte[] data, int start, int len)
    {
        buf = data;
        pos = start;
        endPos = start + len;
    }

    /// <summary>
    /// Resets this buffer to wrap the entire given byte array, positioning the cursor at the start.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.wrap(byte[])</c>.</remarks>
    public void Wrap(byte[] data)
    {
        buf = data;
        pos = 0;
        endPos = data.Length;
    }

    /// <summary>
    /// The underlying backing byte array, or null if the buffer is empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.buf()</c>.</remarks>
    public byte[]? Buf => buf;

    /// <summary>
    /// The current read position within the backing array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.pos()</c>.</remarks>
    public int Pos => pos;

    /// <summary>
    /// The number of bytes between the current position and the end of the window.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.bytesRemaining()</c>.</remarks>
    public int BytesRemaining => endPos - pos;

    /// <summary>
    /// The exclusive end position of the readable window.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.endPos()</c>.</remarks>
    public int EndPos => endPos;

    /// <summary>
    /// Allocates a fresh backing array of <paramref name="len"/> bytes and reads that many bytes from
    /// the stream into it, returning true if the full length was read. Wraps I/O errors in a
    /// <see cref="PbfException"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.load(InputStream, int)</c>.</remarks>
    public bool Load(Stream @in, int len)
    {
        buf = new byte[len];
        pos = 0;
        endPos = len;
        try
        {
            var bytesRead = @in.Read(buf, 0, len);
            return bytesRead == len;
        }
        catch (IOException ex)
        {
            throw new PbfException("Unable to load buffer", ex);
        }
    }

    // TODO: does not respect the original window
    /// <summary>
    /// Re-points this buffer at a new byte array, resetting the cursor to the start.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.reset(byte[])</c>.</remarks>
    public void Reset(byte[] data)
    {
        buf = data;
        pos = 0;
        endPos = data.Length;
    }

    /// <summary>
    /// Reads a single byte and advances the cursor, throwing <see cref="PbfException"/> past the end.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readByte()</c>.</remarks>
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

    /// <summary>
    /// Reads a field tag (a varint combining field number and wire type) and returns it as an int.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readTag()</c>.</remarks>
    public int ReadTag()
    {
        return (int)ReadVarint();
    }

    /// <summary>
    /// Reads a variable-length unsigned integer, advancing the cursor. Throws
    /// <see cref="PbfException"/> on a malformed varint or read past the end.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readVarint()</c>.</remarks>
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
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.countVarInts()</c>.</remarks>
    public int CountVarInts()
    {
        return CountVarInts(pos);
    }

    /// <summary>
    /// Counts the varint values between <paramref name="start"/> and the end of the window without
    /// moving the cursor, by counting bytes that lack the continuation bit.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.countVarInts(int)</c>.</remarks>
    public int CountVarInts(int start)
    {
        var count = 0;
        for (var i = start; i < endPos; i++)
        {
            if ((sbyte)buf![i] >= 0) count++;
        }
        return count;
    }

    // TODO: can this fail if a non-canonical encoding is used?
    //  e.g. 0x81 0x00 instead of 0x01
    /// <summary>
    /// Counts varint values starting at <paramref name="start"/> until a zero byte terminator (or the
    /// end of the window) is reached, without moving the cursor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.countVarIntsUntilZero(int)</c>.</remarks>
    public int CountVarIntsUntilZero(int start)
    {
        var count = 0;
        for (var i = start; i < endPos; i++)
        {
            if (buf![i] == 0) break;
            if ((sbyte)buf[i] >= 0) count++;
        }
        return count;
    }

    /// <summary>
    /// Reads a zig-zag-encoded signed varint and decodes it back to a signed value.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readSignedVarint()</c>.</remarks>
    public long ReadSignedVarint()
    {
        var val = ReadVarint();
        return (val >> 1) ^ -(val & 1);
    }

    /// <summary>
    /// Reads a fixed-width 32-bit integer in little-endian byte order, advancing the cursor by 4 bytes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readFixed32()</c>.</remarks>
    public int ReadFixed32()
    {
        try
        {
            var val = (buf![pos] & 0xff) |
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

    /// <summary>
    /// Reads a fixed-width 64-bit integer in little-endian byte order as two fixed 32-bit halves.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readFixed64()</c>.</remarks>
    public long ReadFixed64()
    {
        return ((long)ReadFixed32() & 0xffff_ffffL) |
            ((long)ReadFixed32() << 32);
    }

    /// <summary>
    /// Reads a single-precision float from its fixed 32-bit IEEE-754 bit pattern.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readFloat()</c>.</remarks>
    public float ReadFloat()
    {
        return BitConverter.Int32BitsToSingle(ReadFixed32());
    }

    /// <summary>
    /// Reads a double-precision float from its fixed 64-bit IEEE-754 bit pattern.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readDouble()</c>.</remarks>
    public double ReadDouble()
    {
        var d = BitConverter.Int64BitsToDouble(ReadFixed64());
        return d;
    }

    /// <summary>
    /// Reads a 32-bit integer in network byte order
    /// (big endian) -- this is not how fixed 32-bit values
    /// are encoded, use readFixed32 instead
    ///
    /// TODO: move out of this class, used only by OsmReader
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readFixedIntNBO()</c>.</remarks>
    public int ReadFixedIntNbo()
    {
        try
        {
            var val = ((sbyte)buf![0] << 24) | ((sbyte)buf[1] << 16) | ((sbyte)buf[2] << 8) | (sbyte)buf[3];
            pos += 4;
            return val;
        }
        catch (IndexOutOfRangeException)
        {
            throw new PbfException("Attempt to read past end of buffer.");
        }
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string: a varint byte count followed by that many UTF-8 bytes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readString()</c>.</remarks>
    public string ReadString()
    {
        var len = (int)ReadVarint();
        return ReadString(len);
    }

    /// <summary>
    /// Reads exactly <paramref name="len"/> bytes and decodes them as a UTF-8 string, advancing the
    /// cursor. Wraps decoding errors in a <see cref="PbfException"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readString(int)</c>.</remarks>
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

    /// <summary>
    /// True if the cursor has not yet reached the end of the window.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.hasMore()</c>.</remarks>
    public bool HasMore()
    {
        return pos < endPos;
    }

    /// <summary>
    /// Advances the cursor forward by <paramref name="len"/> bytes without reading them.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.skip(int)</c>.</remarks>
    public void Skip(int len)
    {
        pos += len;
    }

    /// <summary>
    /// Skips over the value of a field given its tag <paramref name="marker"/>, consuming the
    /// appropriate number of bytes based on its wire type. Throws <see cref="PbfException"/> for an
    /// unknown wire type.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.skipEntity(int)</c>.</remarks>
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
                var len = (int)ReadVarint();
                Skip(len);
                break;
            case PbfType.Fixed32:
                Skip(4);
                break;
            default:
                throw new PbfException("Unknown type: " + (marker & 7));
        }
    }

    /// <summary>
    /// Moves the cursor to the end of the window, marking the buffer as fully consumed.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.end()</c>.</remarks>
    public void End()
    {
        pos = endPos;
    }

    /// <summary>
    /// Prints the bytes in the range <paramref name="from"/>..<paramref name="to"/> to the console in
    /// both decimal and hexadecimal form, for debugging.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.dump(int, int)</c>.</remarks>
    public void Dump(int from, int to)
    {
        var b = new StringBuilder();
        var bHex = new StringBuilder();
        for (var i = from; i < buf!.Length && i < to; i++)
        {
            b.Append(" ");
            b.Append((buf[i] & 0xff).ToString());
            bHex.Append(" ");
            bHex.Append((buf[i] & 0xff).ToString("x"));
        }
        Console.WriteLine("Remaining bytes: " + b.ToString());
        Console.WriteLine("         as hex: " + bHex.ToString());
    }

    /// <summary>
    /// Reads a length-prefixed nested message and returns a new <see cref="PbfBuffer"/> windowed over
    /// its bytes, advancing this buffer's cursor past the message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.readMessage()</c>.</remarks>
    public PbfBuffer ReadMessage()
    {
        var len = (int)ReadVarint();
        var msg = new PbfBuffer(buf!, pos, len);
        pos += len;
        return msg;
    }

    /// <summary>
    /// Moves the cursor to the given absolute position within the backing array.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.seek(int)</c>.</remarks>
    public void Seek(int newPos)
    {
        pos = newPos;
    }

    /// <summary>
    /// Returns the unsigned value of the byte at the current position without advancing, or 0 if the
    /// cursor is at the end.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.peek()</c>.</remarks>
    public int Peek()
    {
        if (pos < endPos) return buf![pos] & 0xff;
        return 0;
    }

    /// <summary>
    /// Returns the unsigned value of the byte at absolute position <paramref name="p"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfBuffer.getByte(int)</c>.</remarks>
    public int GetByte(int p)
    {
        return buf![p] & 0xff;
    }

}
