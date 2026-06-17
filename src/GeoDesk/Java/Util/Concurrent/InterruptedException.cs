/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Java.Util.Concurrent;

// PORT: mirror of java.lang.InterruptedException. The .NET-backed blocking
// primitives in this package do not currently raise it, but the type exists so
// the ported call sites (which catch it) remain structurally identical to Java.
public class InterruptedException : Exception
{
    public InterruptedException()
    {
    }

    public InterruptedException(string message)
        : base(message)
    {
    }
}
