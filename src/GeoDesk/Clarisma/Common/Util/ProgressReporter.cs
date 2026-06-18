/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Clarisma.Common.Text;

namespace Clarisma.Common.Util;

/// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressReporter</c>.</remarks>
internal class ProgressReporter : IProgressListener
{

    readonly string? _progressVerb;
    readonly string? _resultVerb;
    readonly string? _unitsNoun;
    readonly long _totalUnits;
    readonly long _startTime;
    long _unitsProcessed;
    int _percentageReported;

    /// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressReporter(long, String, String, String)</c>.</remarks>
    public ProgressReporter(long totalUnits, string? unitsNoun, string? progressVerb, string? resultVerb)
    {
        _totalUnits = totalUnits;
        _progressVerb = progressVerb;
        _unitsNoun = unitsNoun;
        _resultVerb = resultVerb;
        _startTime = CurrentTimeMillis();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressReporter.progress(int)</c>.</remarks>
    public void Progress(int units)
    {
        if (_progressVerb != null)
        {
            lock (this)
            {
                _unitsProcessed += units;
                var percentageCompleted = (int)(_unitsProcessed * 100 / _totalUnits);
                if (percentageCompleted != _percentageReported)
                {
                    Console.Error.Write(JavaFormat.Format("%s... %d%%\r", _progressVerb, percentageCompleted));
                    _percentageReported = percentageCompleted;
                }
            }
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressReporter.finished()</c>.</remarks>
    public void Finished()
    {
        if (_resultVerb != null)
        {
            var endTime = CurrentTimeMillis();
            Console.Error.Write(JavaFormat.Format("%s %d %s in %s\n", _resultVerb, _totalUnits,
                _unitsNoun, Format.FormatTimespan(endTime - _startTime)));
        }
    }

    // Port-only helper standing in for Java's System.currentTimeMillis().
    static long CurrentTimeMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

}
