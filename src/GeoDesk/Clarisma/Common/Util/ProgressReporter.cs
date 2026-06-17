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
public class ProgressReporter : IProgressListener
{
    private readonly string? progressVerb;
    private readonly string? resultVerb;
    private readonly string? unitsNoun;
    private readonly long totalUnits;
    private readonly long startTime;
    private long unitsProcessed;
    private int percentageReported;

    public ProgressReporter(long totalUnits, string? unitsNoun, string? progressVerb, string? resultVerb)
    {
        this.totalUnits = totalUnits;
        this.progressVerb = progressVerb;
        this.unitsNoun = unitsNoun;
        this.resultVerb = resultVerb;
        startTime = CurrentTimeMillis();
    }

    public void Progress(int units)
    {
        if (progressVerb != null)
        {
            lock (this)
            {
                unitsProcessed += units;
                int percentageCompleted = (int)(unitsProcessed * 100 / totalUnits);
                if (percentageCompleted != percentageReported)
                {
                    Console.Error.Write(JavaFormat.Format("%s... %d%%\r", progressVerb, percentageCompleted));
                    percentageReported = percentageCompleted;
                }
            }
        }
    }

    public void Finished()
    {
        if (resultVerb != null)
        {
            long endTime = CurrentTimeMillis();
            Console.Error.Write(JavaFormat.Format("%s %d %s in %s\n", resultVerb, totalUnits,
                unitsNoun, Format.FormatTimespan(endTime - startTime)));
        }
    }

    private static long CurrentTimeMillis()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
