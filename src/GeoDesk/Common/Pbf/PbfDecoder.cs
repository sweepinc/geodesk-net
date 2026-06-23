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
/// Sequential reader for Protocol Buffers (PBF) data over a <see cref="ReadOnlyMemory{Byte}"/>, tracking
/// a current position and decoding varints, signed (zig-zag) varints, fixed-width integers, and
/// length-prefixed strings (little-endian, via <c>BufferExtensions</c>). The instance methods advance the
/// cursor; the static overloads decode directly from a stream.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder</c>.
/// <para>A mutable value type: the instance methods advance <c>_pos</c> in place. Use it as a local or
/// embed it in a containing type as a non-<c>readonly</c> field; copying it mid-decode (or storing it in a
/// <c>readonly</c> field) snapshots the position, so the original keeps reading from where it was.</para></remarks>
internal struct PbfDecoder
{

    readonly ReadOnlyMemory<byte> _buf;
    int _pos;

    /// <summary>
    /// Creates a decoder reading from the given buffer starting at the given position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder(ByteBuffer, int)</c>.</remarks>
    public PbfDecoder(ReadOnlyMemory<byte> buf, int pos)
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
            b = (sbyte)_buf.Span[_pos++];
            val = b & 0x7f;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 7;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 14;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 21;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 28;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 35;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 42;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 49;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 56;
            if (b >= 0)
                return val;
            b = (sbyte)_buf.Span[_pos++];
            val |= (long)(b & 0x7f) << 63;
            if (b >= 0)
                return val;
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
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 7;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 14;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 21;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 28;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 35;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 42;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 49;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 56;
        if (b >= 0)
            return val;
        b = (sbyte)@in.ReadByte();
        val |= (long)(b & 0x7f) << 63;
        if (b >= 0)
            return val;
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
        _buf.Span.Slice(_pos, len).CopyTo(bytes);
        _pos += len;
        return Encoding.UTF8.GetString(bytes);
    }

    /// <summary>
    /// Reads a length-prefixed UTF-8 string (a varint byte count followed by that many bytes) as a
    /// <em>zero-copy</em> <see cref="ReadOnlyMemory{Byte}"/> slice of the underlying buffer, decoding nothing
    /// and allocating nothing, and advances the cursor past it. Use this instead of <see cref="ReadString"/>
    /// when the raw UTF-8 bytes suffice (byte-wise comparison, deferred/lazy decoding).
    /// </summary>
    /// <remarks>Port-only: enabled by the decoder reading directly over a <see cref="ReadOnlyMemory{Byte}"/>.</remarks>
    public ReadOnlyMemory<byte> ReadString()
    {
        var len = (int)ReadVarint();
        var slice = _buf.Slice(_pos, len);
        _pos += len;
        return slice;
    }

    /// <summary>
    /// Reads a fixed-width 32-bit integer from the current position, advancing the cursor by 4 bytes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readFixed32()</c>.</remarks>
    public int ReadFixed32()
    {
        var v = _buf.Span.GetIntLE(_pos);
        _pos += 4;
        return v;
    }

    /// <summary>
    /// Reads a single byte from the current position, advancing the cursor.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfDecoder.readByte()</c>.</remarks>
    public byte ReadByte()
    {
        var b = _buf.Span[_pos];
        _pos++;
        return b;
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

}
