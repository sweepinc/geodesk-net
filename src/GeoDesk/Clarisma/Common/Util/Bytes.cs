/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;
using Clarisma.Common.Nio;

namespace Clarisma.Common.Util;

public static class Bytes
{
    // not used
    /// <summary>
    /// Searches a byte array for the first occurrence
    /// of a byte array pattern.
    ///
    /// Implementation of KMP from
    /// http://helpdesk.objects.com.au/java/search-a-byte-array-for-a-byte-sequence
    /// </summary>
    public static int IndexOf(byte[] data, byte[] pattern)
    {
        int[] failure = ComputeFailure(pattern);
        int j = 0;
        for (int i = 0; i < data.Length; i++)
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
    /// Computes the failure function using a boot-strapping process,
    /// where the pattern is matched against itself.
    /// </summary>
    private static int[] ComputeFailure(byte[] pattern)
    {
        int[] failure = new int[pattern.Length];
        int j = 0;
        for (int i = 1; i < pattern.Length; i++)
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

    public static int GetInt(byte[] ba, int pos)
    {
        return
            (ba[pos] & 0xff) |
                ((ba[pos + 1] & 0xff) << 8) |
                ((ba[pos + 2] & 0xff) << 16) |
                ((ba[pos + 3] & 0xff) << 24);
    }

    public static void PutInt(byte[] ba, int pos, int v)
    {
        ba[pos] = (byte)v;
        ba[pos + 1] = (byte)(v >> 8);
        ba[pos + 2] = (byte)(v >> 16);
        ba[pos + 3] = (byte)(v >> 24);
    }

    public static void PutShort(byte[] ba, int pos, int v)
    {
        ba[pos] = (byte)v;
        ba[pos + 1] = (byte)(v >> 8);
    }

    public static long GetLong(byte[] ba, int pos)
    {
        return ((long)GetInt(ba, pos) & 0xffff_ffffL) | ((long)GetInt(ba, pos + 4) << 32);
    }

    public static void PutLong(byte[] ba, int pos, long v)
    {
        PutInt(ba, pos, (int)v);
        PutInt(ba, pos + 4, (int)(v >> 32));
    }

    /// <summary>
    /// Reads a string from a buffer. A String must be in the following format:
    /// one or two bytes (using multi-byte encoding) that indicate the length,
    /// followed by the UTF-8 encoded content of the string.
    ///
    /// Note that only string lengths up to 32K are supported.
    /// </summary>
    public static string ReadString(ByteBuffer buf, int p)
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

        byte[] chars = new byte[len];
        buf.Get(p, chars);
        return Encoding.UTF8.GetString(chars);
    }

    /// <summary>
    /// Compares an ASCII string stored in a buffer to a match string.
    /// </summary>
    public static bool StringEqualsAscii(ByteBuffer buf, int p, string s)
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
        for (int i = 0; i < len; i++)
        {
            if (s[i] != (char)buf.Get(p++)) return false;
        }
        return true;
    }

    public static bool StringEquals(ByteBuffer buf, int p, string s)
    {
        return ReadString(buf, p).Equals(s);
    }
}
