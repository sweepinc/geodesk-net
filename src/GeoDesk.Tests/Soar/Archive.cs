/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * Test-only port of the "soar" struct-output-archive framework (subset used by tests).
 */

using System.Diagnostics;

namespace Clarisma.Common.Soar;

public class Archive
{

    Struct? header;
    Struct? last;
    int pos;
    int pageSpaceRemaining;
    readonly int pageSize = 4096;
    readonly int pageSizeMask = (int)((uint)0xffff_ffff >> (32 - 12));

    public int Size()
    {
        return pos;
    }

    public Struct? Header()
    {
        return header;
    }

    public Struct? Last()
    {
        return last;
    }

    public void SetHeader(Struct header)
    {
        Debug.Assert(this.header == null);
        this.header = header;
        last = header;
        pos = header.Size();
        pageSpaceRemaining = pageSize - pos;
    }

    public void Place(Struct s)
    {
        Debug.Assert(header != null, "Must set header before adding other structs");
        Debug.Assert(s.Location() <= 0, "Struct has already been placed");
        pos = s.AlignedLocation(pos);
        s.SetLocation(pos);
        last!.SetNext(s);
        pos += s.Size();
        last = s;
        pageSpaceRemaining = pageSize - (pos & pageSizeMask);
    }
}
