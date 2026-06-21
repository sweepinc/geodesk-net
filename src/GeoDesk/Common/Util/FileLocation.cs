/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Util;

// TODO: change to TextLocation, move to ccc.text?
/// <summary>
/// Describes a position within a source file: its path, line, and (optionally) column. Used to
/// attach source locations to parse results and diagnostics.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation</c>.</remarks>
internal interface IFileLocation
{

    /// <summary>
    /// Returns the path or name of the source file.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation.getFile()</c>.</remarks>
    string GetFile();

    /// <summary>
    /// Returns the 1-based line number within the file.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation.getLine()</c>.</remarks>
    int GetLine();

    /// <summary>
    /// Returns the column number within the line, or -1 if no column information is available.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.FileLocation.getColumn()</c>.</remarks>
    int GetColumn() => -1;

}
