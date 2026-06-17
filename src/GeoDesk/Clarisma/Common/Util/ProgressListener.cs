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
    void Progress(int units);
    void Finished();
}
