/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;

namespace GeoDesk.Feature.Store;

// In Java this extends Box and implements CoordinateSequence. Because NTS's CoordinateSequence
// is an abstract class (not an interface), C# single inheritance forces composition: the box
// extent is stored directly. (BoxCoordinateSequence is only used as a CoordinateSequence.)
public class BoxCoordinateSequence : CoordinateSequence
{
    private readonly int minX;
    private readonly int minY;
    private readonly int maxX;
    private readonly int maxY;

    public BoxCoordinateSequence(int minX, int minY, int maxX, int maxY)
        : base(5, 2, 0)
    {
        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;
    }

    public BoxCoordinateSequence(Bounds b)
        : this(b.MinX, b.MinY, b.MaxX, b.MaxY)
    {
    }

    public int X(int n)
    {
        return (((n + 1) & 2) != 0) ? minX : maxX;
    }

    public int Y(int n)
    {
        return ((n & 2) != 0) ? minY : maxY;
    }

    public override Envelope ExpandEnvelope(Envelope env)
    {
        env.ExpandToInclude(minX, minY);
        env.ExpandToInclude(maxX, maxY);
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
        return ordinateIndex == 0 ? X(n) : Y(n);
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
        throw new InvalidOperationException("Coordinates are immutable.");
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
