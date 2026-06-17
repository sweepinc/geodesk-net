/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * Test-only port of the "soar" struct-output-archive framework.
 */

namespace Clarisma.Common.Soar;

public abstract class Struct
{
    private int location;
    private int size;
    private int anchorAndAlignment;
    private Struct? next;

    public int Location()
    {
        return location;
    }

    public void SetLocation(int location)
    {
        this.location = location;
    }

    public int Size()
    {
        return size;
    }

    protected void SetSize(int size)
    {
        this.size = size;
    }

    public int Anchor()
    {
        return anchorAndAlignment & 0x0fff_ffff;
    }

    public int AnchorLocation()
    {
        return Location() + Anchor();
    }

    protected void SetAnchor(int anchor)
    {
        anchorAndAlignment = (int)((uint)anchorAndAlignment & 0xf000_0000) | anchor;
    }

    public int Alignment()
    {
        return (int)((uint)anchorAndAlignment >> 28);
    }

    public void SetAlignment(int alignment)
    {
        anchorAndAlignment = (anchorAndAlignment & 0x0fff_ffff) | (alignment << 28);
    }

    public int AlignedLocation(int pos)
    {
        int alignment = Alignment();
        int alignBytes = 1 << alignment;
        int alignMask = 0xffff >> (16 - alignment);
        return pos + ((alignBytes - (pos & alignMask)) & alignMask);
    }

    public Struct? Next()
    {
        return next;
    }

    public void SetNext(Struct? s)
    {
        next = s;
    }

    public abstract void WriteTo(StructOutputStream @out);

    public override string ToString()
    {
        return base.ToString() ?? "";
    }
}
