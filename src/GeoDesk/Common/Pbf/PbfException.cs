/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Common.Pbf;

/// <summary>
/// Exception thrown when decoding Protocol Buffers (PBF) data fails, for example on a malformed
/// varint or a read past the end of the buffer.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfException</c>.</remarks>
internal class PbfException : Exception
{

    /// <summary>
    /// Creates a PBF exception with the given message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfException(String)</c>.</remarks>
    public PbfException(string msg) :
        base(msg)
    {

    }

    /// <summary>
    /// Creates a PBF exception with the given message wrapping an underlying cause.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.pbf.PbfException(String, Exception)</c>.</remarks>
    public PbfException(string msg, Exception root) :
        base(msg, root)
    {

    }

}
