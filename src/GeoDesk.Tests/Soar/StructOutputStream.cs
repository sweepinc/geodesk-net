/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * Test-only port of the "soar" struct-output-archive framework.
 */

using System.Diagnostics;
using System.IO;

namespace Clarisma.Common.Soar;

// In Java this extends OutputStream; here it wraps a Stream.
public class StructOutputStream
{
    private readonly Stream @out;
    private int pos;

    public StructOutputStream(Stream @out)
    {
        this.@out = @out;
    }

    public void WriteVarint(long val)
    {
        while (val >= 0x80 || val < 0)
        {
            Write((int)(val & 0x7f) | 0x80);
            val = (long)((ulong)val >> 7);
        }
        Write((int)val);
    }

    public void Write(int b)
    {
        @out.WriteByte((byte)b);
        pos++;
    }

    public void Write(byte[] b)
    {
        @out.Write(b, 0, b.Length);
        pos += b.Length;
    }

    public void Write(byte[] b, int off, int len)
    {
        @out.Write(b, off, len);
        pos += len;
    }

    public void WriteInt(int v)
    {
        @out.WriteByte((byte)(v & 0xFF));
        @out.WriteByte((byte)((v >> 8) & 0xFF));
        @out.WriteByte((byte)((v >> 16) & 0xFF));
        @out.WriteByte((byte)((v >> 24) & 0xFF));
        pos += 4;
    }

    public void WriteShort(int v)
    {
        @out.WriteByte((byte)(v & 0xFF));
        @out.WriteByte((byte)((v >> 8) & 0xFF));
        pos += 2;
    }

    public void WriteLong(long v)
    {
        WriteInt((int)v);
        WriteInt((int)(v >> 32));
    }

    public int Position()
    {
        return pos;
    }

    public void WritePointer(Struct? target)
    {
        if (target == null)
        {
            WriteInt(0);
            return;
        }
        WriteInt(target.AnchorLocation() - pos);
    }

    public void WritePointer(Struct? target, int flags)
    {
        int p;
        if (target == null)
        {
            p = 0;
        }
        else
        {
            p = target.AnchorLocation() - pos;
        }
        WriteInt(p | flags);
    }

    public void WriteStruct(Struct s)
    {
        Debug.Assert(pos <= s.Location());
        while (pos < s.Location()) Write(0);
        int oldPos = pos;
        s.WriteTo(this);
        Debug.Assert(pos == oldPos + s.Size());
    }

    public void WriteBlank(int len)
    {
        for (int i = 0; i < len; i++) Write(0);
    }

    public void WriteChain(Struct? s)
    {
        while (s != null)
        {
            WriteStruct(s);
            s = s.Next();
        }
    }
}
