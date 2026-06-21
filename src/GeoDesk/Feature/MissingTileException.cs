/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Feature;

/// <summary>
/// Thrown when a query needs a tile that is not present in the feature library, identified by its
/// tile index pointer (TIP). This typically indicates an incomplete or partially downloaded library.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.MissingTileException</c>.</remarks>
public class MissingTileException : Exception
{

    readonly int _tip;

    /// <summary>
    /// Creates a new <see cref="MissingTileException"/> for the tile identified by the given tile
    /// index pointer (TIP), formatting the TIP as a 6-digit hexadecimal value in the message.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.MissingTileException(int)</c>.</remarks>
    public MissingTileException(int tip) :
        base(string.Format(CultureInfo.InvariantCulture, "Missing tile: {0:X6}", tip))
    {
        _tip = tip;
    }

}
