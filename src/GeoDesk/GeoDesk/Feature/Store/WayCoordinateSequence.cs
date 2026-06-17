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
public class WayCoordinateSequence : CoordinateSequence
{
    private readonly int[] coordinates; // pairs of x/y

    public WayCoordinateSequence(int[] coords)
        : base(coords.Length / 2, 2, 0)
    {
        this.coordinates = coords;
    }

    public int X(int n)
    {
        return coordinates[n * 2];
    }

    public int Y(int n)
    {
        return coordinates[n * 2 + 1];
    }

    public override Envelope ExpandEnvelope(Envelope env)
    {
        for (int i = 0; i < Count; i++)
        {
            env.ExpandToInclude(X(i), Y(i));
        }
        return env;
    }

    public override Coordinate GetCoordinate(int n)
    {
        return new Coordinate(X(n), Y(n));
    }

    public override void GetCoordinate(int n, Coordinate c)
    {
        c.X = X(n);
        c.Y = Y(n);
    }

    public override Coordinate GetCoordinateCopy(int n)
    {
        return GetCoordinate(n);
    }

    public override double GetOrdinate(int n, int ordinateIndex)
    {
        return coordinates[n * 2 + ordinateIndex];
    }

    public override double GetX(int n)
    {
        return X(n);
    }

    public override double GetY(int n)
    {
        return Y(n);
    }

    public override void SetOrdinate(int n, int ordinateIndex, double value)
    {
        throw new InvalidOperationException("Coordinates of a Way are immutable.");
    }

    public override Coordinate[] ToCoordinateArray()
    {
        Coordinate[] c = new Coordinate[Count];
        for (int i = 0; i < Count; i++)
        {
            c[i] = GetCoordinate(i);
        }
        return c;
    }

    public override CoordinateSequence Copy()
    {
        return new CoordinateArraySequence(ToCoordinateArray());
    }
}
