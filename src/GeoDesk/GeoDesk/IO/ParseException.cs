/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.IO;

// TODO: use a more general class
/// <remarks>Ported from Java <c>com.geodesk.io.ParseException</c>.</remarks>
internal class ParseException : Exception
{

    /// <remarks>Ported from Java <c>com.geodesk.io.ParseException(String)</c>.</remarks>
    public ParseException(string msg)
        : base(msg)
    {
    }

}
