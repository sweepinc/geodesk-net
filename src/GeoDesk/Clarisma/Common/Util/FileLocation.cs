/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Clarisma.Common.Util;

// TODO: change to TextLocation, move to ccc.text?
public interface IFileLocation
{
    string GetFile();
    int GetLine();
    int GetColumn() => -1;
}
