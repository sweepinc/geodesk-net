/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;

namespace GeoDesk.Feature;

/// <remarks>Ported from Java <c>com.geodesk.feature.MissingTileException</c>.</remarks>
public class MissingTileException : Exception
{

    readonly int _tip;

    /// <remarks>Ported from Java <c>com.geodesk.feature.MissingTileException(int)</c>.</remarks>
    public MissingTileException(int tip) :
        base(string.Format(CultureInfo.InvariantCulture, "Missing tile: {0:X6}", tip))
    {
        _tip = tip;
    }

}
