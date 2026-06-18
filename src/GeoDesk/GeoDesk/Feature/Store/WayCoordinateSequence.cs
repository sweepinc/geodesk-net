/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A <see cref="CoordinateSequence"/> that provides integer-based coordinates in a compact format.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence</c>.</remarks>
public class WayCoordinateSequence : CoordinateSequence
{

    readonly int[] _coordinates; // pairs of x/y

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence(int[])</c>.</remarks>
    public WayCoordinateSequence(int[] coords)
        : base(coords.Length / 2, 2, 0)
    {
        _coordinates = coords;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.x(int)</c>.</remarks>
    public int X(int n)
    {
        return _coordinates[n * 2];
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.y(int)</c>.</remarks>
    public int Y(int n)
    {
        return _coordinates[n * 2 + 1];
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.expandEnvelope(Envelope)</c>.</remarks>
    public override Envelope ExpandEnvelope(Envelope env)
    {
        for (var i = 0; i < Count; i++)
        {
            env.ExpandToInclude(X(i), Y(i));
        }
        return env;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getCoordinate(int)</c>.</remarks>
    public override Coordinate GetCoordinate(int n)
    {
        return new Coordinate(X(n), Y(n));
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getCoordinate(int, Coordinate)</c>.</remarks>
    public override void GetCoordinate(int n, Coordinate c)
    {
        c.X = X(n);
        c.Y = Y(n);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getCoordinateCopy(int)</c>.</remarks>
    public override Coordinate GetCoordinateCopy(int n)
    {
        return GetCoordinate(n);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getOrdinate(int, int)</c>.</remarks>
    public override double GetOrdinate(int n, int ordinateIndex)
    {
        return _coordinates[n * 2 + ordinateIndex];
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getX(int)</c>.</remarks>
    public override double GetX(int n)
    {
        return X(n);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getY(int)</c>.</remarks>
    public override double GetY(int n)
    {
        return Y(n);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.setOrdinate(int, int, double)</c>.</remarks>
    public override void SetOrdinate(int n, int ordinateIndex, double value)
    {
        throw new InvalidOperationException("Coordinates of a Way are immutable.");
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.toCoordinateArray()</c>.</remarks>
    public override Coordinate[] ToCoordinateArray()
    {
        var c = new Coordinate[Count];
        for (var i = 0; i < Count; i++)
        {
            c[i] = GetCoordinate(i);
        }
        return c;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.copy()</c>.</remarks>
    public override CoordinateSequence Copy()
    {
        return new CoordinateArraySequence(ToCoordinateArray());
    }

}
