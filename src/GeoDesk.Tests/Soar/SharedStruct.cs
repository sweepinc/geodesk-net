/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * Test-only port of the "soar" struct-output-archive framework.
 */

namespace Clarisma.Common.Soar;

public abstract class SharedStruct : Struct
{

    int userCount;
    float usage;

    public float Usage()
    {
        return usage;
    }

    public int UserCount()
    {
        return userCount;
    }

    public void AddUsage(float u)
    {
        usage += u;
        userCount++;
    }

    public bool IsShared()
    {
        return userCount > 1;
    }
}
