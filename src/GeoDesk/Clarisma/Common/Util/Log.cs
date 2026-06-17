/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace Clarisma.Common.Util;

public static class Log
{
    private static readonly object SyncRoot = new object();

    public static void Write(string format, params object?[] args)
    {
        DateTime date = DateTime.Now;
        lock (SyncRoot)
        {
            Console.Out.Write(date.ToString("HH:mm:ss ", CultureInfo.InvariantCulture));
            Console.Out.Write(JavaFormat.Format(format, args));
            Console.Out.WriteLine();
        }
    }

    public static void Debug(object? arg)
    {
        Write("%s", arg);
    }

    public static void Debug(string format, params object?[] args)
    {
        Write(format, args);
    }

    public static void Error(string format, params object?[] args)
    {
        Write(format, args); // TODO
    }

    public static void Warn(string format, params object?[] args)
    {
        Write(format, args); // TODO
    }
}
