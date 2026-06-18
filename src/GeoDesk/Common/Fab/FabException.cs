/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Fab;

/// <remarks>Ported from Java <c>com.clarisma.common.fab.FabException</c>.</remarks>
internal class FabException : Exception
{

    /// <remarks>Ported from Java <c>com.clarisma.common.fab.FabException(String)</c>.</remarks>
    public FabException(string msg) :
        base(msg)
    {

    }

}
