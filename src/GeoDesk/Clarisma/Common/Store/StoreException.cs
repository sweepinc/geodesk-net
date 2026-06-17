/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace Clarisma.Common.Store;

// In Java the path type is java.nio.file.Path; here it is a string.
public class StoreException : Exception
{
    private readonly string? path;

    public StoreException(string msg, string path)
        : this(msg, path, null)
    {
    }

    public StoreException(string msg, Exception? cause)
        : base(msg, cause)
    {
    }

    public StoreException(string msg, string path, Exception? cause)
        : base(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", path, msg), cause)
    {
        this.path = path;
    }
}
