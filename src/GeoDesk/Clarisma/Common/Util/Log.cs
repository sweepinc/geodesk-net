/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace Clarisma.Common.Util;

/// <remarks>Ported from Java <c>com.clarisma.common.util.Log</c>.</remarks>
public static class Log
{

    // Port-only: Java synchronizes on System.out and formats the timestamp with a SimpleDateFormat;
    // here a lock object stands in for the System.out monitor.
    static readonly object SyncRoot = new object();

    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.log(String, Object...)</c>.</remarks>
    public static void Write(string format, params object?[] args)
    {
        var date = DateTime.Now;
        lock (SyncRoot)
        {
            Console.Out.Write(date.ToString("HH:mm:ss ", CultureInfo.InvariantCulture));
            Console.Out.Write(JavaFormat.Format(format, args));
            Console.Out.WriteLine();
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.debug(Object)</c>.</remarks>
    public static void Debug(object? arg)
    {
        Write("%s", arg);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.debug(String, Object...)</c>.</remarks>
    public static void Debug(string format, params object?[] args)
    {
        Write(format, args);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.error(String, Object...)</c>.</remarks>
    public static void Error(string format, params object?[] args)
    {
        Write(format, args); // TODO
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.warn(String, Object...)</c>.</remarks>
    public static void Warn(string format, params object?[] args)
    {
        Write(format, args); // TODO
    }

}
