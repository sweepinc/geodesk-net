/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Util;

// TODO: change to TextLocation, move to ccc.text?
/// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation</c>.</remarks>
internal interface IFileLocation
{

    /// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation.getFile()</c>.</remarks>
    string GetFile();

    /// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation.getLine()</c>.</remarks>
    int GetLine();

    /// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation.getColumn()</c>.</remarks>
    int GetColumn() => -1;

}
