/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Pbf;

/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfException</c>.</remarks>
public class PbfException : Exception
{
    public PbfException(string msg)
        : base(msg)
    {
    }

    public PbfException(string msg, Exception root)
        : base(msg, root)
    {
    }
}
