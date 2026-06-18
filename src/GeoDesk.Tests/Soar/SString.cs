/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * Test-only port of the "soar" struct-output-archive framework.
 */

using System;
using System.Text;

using GeoDesk.Common.Pbf;

namespace Clarisma.Common.Soar;

public class SString : SharedStruct, IComparable<SString>
{
    private readonly string str;
    private readonly byte[] bytes;

    public SString(string s)
    {
        str = s;
        bytes = Encoding.UTF8.GetBytes(s);
        int lenLength = PbfEncoder.VarintLength(bytes.Length);
        SetSize(bytes.Length + lenLength);
        SetAlignment(0);
    }

    public override bool Equals(object? other)
    {
        if (other is not SString o) return false;
        return str.Equals(o.str);
    }

    public override int GetHashCode()
    {
        return str.GetHashCode();
    }

    public override string ToString()
    {
        return str;
    }

    public int CompareTo(SString? other)
    {
        return string.CompareOrdinal(str, other!.str);
    }

    public override void WriteTo(StructOutputStream @out)
    {
        @out.WriteVarint(bytes.Length);
        @out.Write(bytes);
    }
}
