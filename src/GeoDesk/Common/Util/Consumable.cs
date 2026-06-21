/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Util;

/// <summary>
/// A lightweight Iterator-like data structure that can only be traversed once.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.util.Consumable</c>.</remarks>
internal interface IConsumable
{

    /// <summary>
    /// Returns true if no further elements remain to be consumed.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.util.Consumable.isEmpty()</c>.</remarks>
    bool IsEmpty();

}
