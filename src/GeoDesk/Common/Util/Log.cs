/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Common.Util;

/// <summary>
/// Simple thread-safe console logger that writes timestamped, Java-style formatted messages to
/// standard output, with convenience methods for debug, warning, and error severities.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.util.Log</c>.</remarks>
internal static class Log
{

    // Port-only: Java synchronizes on System.out and formats the timestamp with a SimpleDateFormat;
    // here a lock object stands in for the System.out monitor.
    static readonly object SyncRoot = new object();

    /// <summary>
    /// Writes a timestamped, Java-style formatted message to standard output as a single line,
    /// synchronizing so concurrent calls do not interleave.
    /// </summary>
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

    /// <summary>
    /// Logs the string representation of a single object as a debug message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.debug(Object)</c>.</remarks>
    public static void Debug(object? arg)
    {
        Write("%s", arg);
    }

    /// <summary>
    /// Logs a formatted debug message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.debug(String, Object...)</c>.</remarks>
    public static void Debug(string format, params object?[] args)
    {
        Write(format, args);
    }

    /// <summary>
    /// Logs a formatted error message. Currently routed through <see cref="Write"/> without severity
    /// distinction.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.error(String, Object...)</c>.</remarks>
    public static void Error(string format, params object?[] args)
    {
        Write(format, args); // TODO
    }

    /// <summary>
    /// Logs a formatted warning message. Currently routed through <see cref="Write"/> without severity
    /// distinction.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Log.warn(String, Object...)</c>.</remarks>
    public static void Warn(string format, params object?[] args)
    {
        Write(format, args); // TODO
    }

}
