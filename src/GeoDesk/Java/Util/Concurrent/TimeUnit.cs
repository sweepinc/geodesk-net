/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Java.Util.Concurrent;

// PORT: faithful mirror of java.util.concurrent.TimeUnit (subset). Only the units used by the
// port are represented; ToMillis() backs the .NET wait primitives.
/// <remarks>Ported from Java <c>java.util.concurrent.TimeUnit</c>.</remarks>
internal enum TimeUnit
{
    Nanoseconds,
    Microseconds,
    Milliseconds,
    Seconds,
    Minutes,
    Hours,
    Days
}

// PORT-only: C# enums cannot carry methods, so TimeUnit.toMillis(long) is provided here as an
// extension method.
internal static class TimeUnitExtensions
{

    /// <remarks>Ported from Java <c>java.util.concurrent.TimeUnit.toMillis(long)</c>.</remarks>
    public static long ToMillis(this TimeUnit unit, long duration)
    {
        return unit switch
        {
            TimeUnit.Nanoseconds => duration / 1_000_000L,
            TimeUnit.Microseconds => duration / 1_000L,
            TimeUnit.Milliseconds => duration,
            TimeUnit.Seconds => duration * 1_000L,
            TimeUnit.Minutes => duration * 60_000L,
            TimeUnit.Hours => duration * 3_600_000L,
            TimeUnit.Days => duration * 86_400_000L,
            _ => duration,
        };
    }

}
