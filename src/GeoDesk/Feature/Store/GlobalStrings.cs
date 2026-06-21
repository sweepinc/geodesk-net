/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Store;

/// <summary>
/// Fixed global string table codes that every GeoDesk feature library reserves for the most common
/// tag values (the empty string and the keywords <c>no</c>, <c>yes</c>, <c>outer</c>, and <c>inner</c>).
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.GlobalStrings</c>.</remarks>
internal static class GlobalStrings
{

    public const int EMPTY = 0;
    public const int NO = 1;
    public const int YES = 2;
    public const int OUTER = 3;
    public const int INNER = 4;

}
