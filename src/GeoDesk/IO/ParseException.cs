/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.IO;

// TODO: use a more general class
/// <summary>
/// Exception thrown when an input file (such as a poly file) cannot be parsed.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.io.ParseException</c>.</remarks>
internal class ParseException : Exception
{

    /// <summary>
    /// Creates a parse exception with the given message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.io.ParseException(String)</c>.</remarks>
    public ParseException(string msg)
        : base(msg)
    {
    }

}
