/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Util;

/// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressListener</c>.</remarks>
public interface IProgressListener
{

    /// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressListener.progress(int)</c>.</remarks>
    void Progress(int units);

    /// <remarks>Ported from Java <c>com.clarisma.common.util.ProgressListener.finished()</c>.</remarks>
    void Finished();

}
