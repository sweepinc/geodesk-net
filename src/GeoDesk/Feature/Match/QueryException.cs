/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Feature.Match;

/// <summary>
/// Exception thrown when a feature query cannot be compiled or executed, for example due to a
/// malformed query expression.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.match.QueryException</c>.</remarks>
public class QueryException : Exception
{

    /// <summary>
    /// Creates a query exception with the given message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.QueryException(String)</c>.</remarks>
    public QueryException(string msg) :
        base(msg)
    {

    }

    /// <summary>
    /// Creates a query exception with the given message wrapping an underlying cause.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.match.QueryException(String, Exception)</c>.</remarks>
    public QueryException(string msg, Exception ex) :
        base(msg, ex)
    {

    }

}
