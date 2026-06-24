/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Benchmarks;

/// <summary>
/// A seed point with a radius, used by the "enclosing" benchmarks. Only <see cref="X"/>/<see cref="Y"/>
/// drive the query (<c>ContainingXY</c>); <see cref="Radius"/> is carried for parity with the upstream
/// shape cache.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.benchmark.Circle</c>.</remarks>
internal readonly struct Circle
{
    public readonly int X;
    public readonly int Y;
    public readonly int Radius;

    public Circle(int x, int y, int radius)
    {
        X = x;
        Y = y;
        Radius = radius;
    }
}
