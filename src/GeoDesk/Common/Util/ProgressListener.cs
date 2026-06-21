/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Util;

/// <summary>
/// Callback interface for reporting incremental progress of a long-running task: implementations
/// receive units of work as they complete and a final notification when the task finishes.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressListener</c>.</remarks>
internal interface IProgressListener
{

    /// <summary>
    /// Reports that <paramref name="units"/> additional units of work have been completed.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressListener.progress(int)</c>.</remarks>
    void Progress(int units);

    /// <summary>
    /// Reports that the task has finished and no further progress will be reported.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressListener.finished()</c>.</remarks>
    void Finished();

}
