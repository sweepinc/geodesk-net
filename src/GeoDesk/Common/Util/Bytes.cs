/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;

using GeoDesk.Buffers;

namespace GeoDesk.Common.Util;

/// <summary>
/// Low-level byte-array helpers: little-endian reads and writes of 16-, 32-, and 64-bit integers, a
/// KMP byte-pattern search, and reading or comparing length-prefixed UTF-8 strings stored in a buffer.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes</c>.</remarks>
internal static class Bytes
{

    // not used
    /// <summary>
    /// Searches a byte array for the first occurrence of a byte array pattern.
    ///
    /// Implementation of KMP from
    /// http://helpdesk.objects.com.au/java/search-a-byte-array-for-a-byte-sequence
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.indexOf(byte[], byte[])</c>.</remarks>
    public static int IndexOf(byte[] data, byte[] pattern)
    {
        var failure = ComputeFailure(pattern);
        var j = 0;
        for (var i = 0; i < data.Length; i++)
        {
            while (j > 0 && pattern[j] != data[i])
            {
                j = failure[j - 1];
            }
            if (pattern[j] == data[i])
            {
                j++;
            }
            if (j == pattern.Length)
            {
                return i - pattern.Length + 1;
            }
        }
        return -1;
    }

    // not used
    /// <summary>
    /// Computes the failure function using a boot-strapping process, where the pattern is matched
    /// against itself.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.computeFailure(byte[])</c>.</remarks>
    static int[] ComputeFailure(byte[] pattern)
    {
        var failure = new int[pattern.Length];
        var j = 0;
        for (var i = 1; i < pattern.Length; i++)
        {
            while (j > 0 && pattern[j] != pattern[i])
            {
                j = failure[j - 1];
            }
            if (pattern[j] == pattern[i])
            {
                j++;
            }
            failure[i] = j;
        }
        return failure;
    }

    /// <summary>
    /// Reads a little-endian 32-bit integer from the byte array at the given position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.getInt(byte[], int)</c>.</remarks>
    public static int GetInt(byte[] ba, int pos)
    {
        return
            (ba[pos] & 0xff) |
                ((ba[pos + 1] & 0xff) << 8) |
                ((ba[pos + 2] & 0xff) << 16) |
                ((ba[pos + 3] & 0xff) << 24);
    }

    /// <summary>
    /// Writes a 32-bit integer in little-endian byte order to the byte array at the given position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.putInt(byte[], int, int)</c>.</remarks>
    public static void PutInt(byte[] ba, int pos, int v)
    {
        ba[pos] = (byte)v;
        ba[pos + 1] = (byte)(v >> 8);
        ba[pos + 2] = (byte)(v >> 16);
        ba[pos + 3] = (byte)(v >> 24);
    }

    /// <summary>
    /// Writes the low 16 bits of a value in little-endian byte order to the byte array at the given
    /// position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.putShort(byte[], int, int)</c>.</remarks>
    public static void PutShort(byte[] ba, int pos, int v)
    {
        ba[pos] = (byte)v;
        ba[pos + 1] = (byte)(v >> 8);
    }

    /// <summary>
    /// Reads a little-endian 64-bit integer from the byte array at the given position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.getLong(byte[], int)</c>.</remarks>
    public static long GetLong(byte[] ba, int pos)
    {
        return ((long)GetInt(ba, pos) & 0xffff_ffffL) | ((long)GetInt(ba, pos + 4) << 32);
    }

    /// <summary>
    /// Writes a 64-bit integer in little-endian byte order to the byte array at the given position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.putLong(byte[], int, long)</c>.</remarks>
    public static void PutLong(byte[] ba, int pos, long v)
    {
        PutInt(ba, pos, (int)v);
        PutInt(ba, pos + 4, (int)(v >> 32));
    }

    /// <summary>
    /// Reads a string from a buffer. A String must be in the following format: one or two bytes (using
    /// multi-byte encoding) that indicate the length, followed by the UTF-8 encoded content of the
    /// string. Note that only string lengths up to 32K are supported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.readString(ByteBuffer, int)</c>.</remarks>
    public static string ReadString(NioBufferReader buf, int p)
    {
        // TODO: This may overrun if string is zero-length
        int len = buf.GetChar(p);
        if ((len & 0x80) != 0)
        {
            len = (len & 0x7f) | (len >> 1) & 0xff80;
            p += 2;
        }
        else
        {
            len &= 0x7f;
            p++;
        }

        var chars = new byte[len];
        buf.Get(p, chars);
        return Encoding.UTF8.GetString(chars);
    }

    /// <summary>Compares an ASCII string stored in a buffer to a match string.</summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.stringEqualsAscii(ByteBuffer, int, String)</c>.</remarks>
    public static bool StringEqualsAscii(NioBufferReader buf, int p, string s)
    {
        int len = buf.GetChar(p);
        if ((len & 0x80) != 0)
        {
            len = (len & 0x7f) | (len >> 1) & 0xff80;
            p += 2;
        }
        else
        {
            len &= 0x7f;
            p++;
        }
        if (len != s.Length) return false;
        for (var i = 0; i < len; i++)
        {
            if (s[i] != (char)buf.Get(p++)) return false;
        }
        return true;
    }

    /// <summary>
    /// Reads the length-prefixed UTF-8 string at the given buffer position and returns whether it
    /// equals <paramref name="s"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Bytes.stringEquals(ByteBuffer, int, String)</c>.</remarks>
    public static bool StringEquals(NioBufferReader buf, int p, string s)
    {
        return ReadString(buf, p).Equals(s);
    }

}
