/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Clarisma.Common.Fab;

public class FabException : Exception
{
    public FabException(string msg)
        : base(msg)
    {
    }
}
