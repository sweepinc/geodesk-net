/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Parser;

/// <summary>
/// Exception thrown when input cannot be parsed by the common parser infrastructure.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.parser.ParserException</c>.</remarks>
internal class ParserException : Exception
{

    /// <summary>
    /// Creates a parser exception with the given message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.parser.ParserException(String)</c>.</remarks>
    public ParserException(string msg)
        : base(msg)
    {
    }

}
