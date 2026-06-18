/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Geom;

namespace GeoDesk.Tests.Tests;

/// <remarks>Ported from Java <c>com.geodesk.tests.BoxMaker</c>.</remarks>
public class BoxMaker
{

    readonly System.Random _random;
    readonly IBounds _bounds;
    readonly double _scale;

    /// <remarks>Ported from Java <c>com.geodesk.tests.BoxMaker(Bounds)</c>.</remarks>
    public BoxMaker(IBounds bounds)
    {
        _bounds = bounds;
        _random = new System.Random();
        _scale = Mercator.MetersAtY(bounds.CenterY);
    }

    /// <remarks>Ported from Java <c>com.geodesk.tests.BoxMaker.random(int, int)</c>.</remarks>
    public Box Random(int minMeters, int maxMeters)
    {
        double widthMeters = _random.Next(maxMeters - minMeters) + minMeters;
        double heightMeters = _random.Next(maxMeters - minMeters) + minMeters;
        var w = (int)(widthMeters / _scale);
        var h = (int)(widthMeters / _scale);
        var xDelta = _random.NextInt64(_bounds.Width - w);
        var yDelta = _random.NextInt64(_bounds.Height - h);
        return Box.OfXYWidthHeight((int)(_bounds.MinX + xDelta), (int)(_bounds.MinY + yDelta), w, h);
    }

}
