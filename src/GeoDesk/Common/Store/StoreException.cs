/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Common.Store;

// In Java the path type is java.nio.file.Path; here it is a string.
/// <summary>
/// Exception describing a failure accessing a blob store, optionally carrying the path of the store
/// file involved. When a path is supplied it is prefixed onto the message.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.StoreException</c>.</remarks>
internal class StoreException : Exception
{

    readonly string? path;

    /// <summary>
    /// Creates a store exception for the given message and store path.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.StoreException(String, Path)</c>.</remarks>
    public StoreException(string msg, string path) :
        this(msg, path, null)
    {

    }

    /// <summary>
    /// Creates a store exception with the given message wrapping an underlying cause, without a path.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.StoreException(String, Throwable)</c>.</remarks>
    public StoreException(string msg, Exception? cause) :
        base(msg, cause)
    {

    }

    /// <summary>
    /// Creates a store exception for the given message and store path, wrapping an underlying cause.
    /// The path is prepended to the message text.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.StoreException(String, Path, Throwable)</c>.</remarks>
    public StoreException(string msg, string path, Exception? cause) :
        base(string.Format(CultureInfo.InvariantCulture, "{0}: {1}", path, msg), cause)
    {
        this.path = path;
    }

}
