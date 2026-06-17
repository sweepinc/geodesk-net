/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace GeoDesk.Feature.Match;

public class QueryException : Exception
{
    public QueryException(string msg)
        : base(msg)
    {
    }

    public QueryException(string msg, Exception ex)
        : base(msg, ex)
    {
    }
}
