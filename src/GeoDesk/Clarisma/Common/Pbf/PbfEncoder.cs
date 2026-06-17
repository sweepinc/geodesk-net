/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Numerics;
using Java.Nio;

namespace Clarisma.Common.Pbf;

public static class PbfEncoder
{
    public static void WriteVarint(ByteBuffer buf, long val)
    {
        while (val >= 0x80 || val < 0)
        {
            buf.Put((byte)((val & 0x7f) | 0x80));
            val = (long)((ulong)val >> 7);
        }
        buf.Put((byte)val);
    }

    public static int WriteVarint(byte[] buf, int pos, long val)
    {
        int len = 1;
        while (val >= 0x80 || val < 0)
        {
            buf[pos] = (byte)((val & 0x7f) | 0x80);
            val = (long)((ulong)val >> 7);
            len++;
            pos++;
        }
        buf[pos] = (byte)val;
        return len;
    }

    public static int VarintLength(int val)
    {
        if (val == 0) return 1;
        return 5 - (BitOperations.LeadingZeroCount((uint)val) + 3) / 7;
    }
}
