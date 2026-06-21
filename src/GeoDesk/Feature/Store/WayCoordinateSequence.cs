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
internal class WayCoordinateSequence : CoordinateSequence
{

    readonly int[] _coordinates; // pairs of x/y

    /// <summary>
    /// Creates a coordinate sequence over the given flat array of X/Y integer pairs.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence(int[])</c>.</remarks>
    public WayCoordinateSequence(int[] coords) :
        base(coords.Length / 2, 2, 0)
    {
        _coordinates = coords;
    }

    /// <summary>
    /// Returns the X ordinate of the n-th coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.x(int)</c>.</remarks>
    public int X(int n)
    {
        return _coordinates[n * 2];
    }

    /// <summary>
    /// Returns the Y ordinate of the n-th coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.y(int)</c>.</remarks>
    public int Y(int n)
    {
        return _coordinates[n * 2 + 1];
    }

    /// <summary>
    /// Expands the given envelope to include every coordinate in this sequence.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.expandEnvelope(Envelope)</c>.</remarks>
    public override Envelope ExpandEnvelope(Envelope env)
    {
        for (var i = 0; i < Count; i++)
        {
            env.ExpandToInclude(X(i), Y(i));
        }
        return env;
    }

    /// <summary>
    /// Returns a new <see cref="Coordinate"/> for the n-th position.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getCoordinate(int)</c>.</remarks>
    public override Coordinate GetCoordinate(int n)
    {
        return new Coordinate(X(n), Y(n));
    }

    /// <summary>
    /// Copies the n-th position's ordinates into the supplied coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getCoordinate(int, Coordinate)</c>.</remarks>
    public override void GetCoordinate(int n, Coordinate c)
    {
        c.X = X(n);
        c.Y = Y(n);
    }

    /// <summary>
    /// Returns a fresh copy of the n-th coordinate; the sequence is immutable so no
    /// distinct backing storage exists.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getCoordinateCopy(int)</c>.</remarks>
    public override Coordinate GetCoordinateCopy(int n)
    {
        return GetCoordinate(n);
    }

    /// <summary>
    /// Returns the requested ordinate (0 = X, 1 = Y) of the n-th coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getOrdinate(int, int)</c>.</remarks>
    public override double GetOrdinate(int n, int ordinateIndex)
    {
        return _coordinates[n * 2 + ordinateIndex];
    }

    /// <summary>
    /// Returns the X ordinate of the n-th coordinate as a double.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getX(int)</c>.</remarks>
    public override double GetX(int n)
    {
        return X(n);
    }

    /// <summary>
    /// Returns the Y ordinate of the n-th coordinate as a double.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.getY(int)</c>.</remarks>
    public override double GetY(int n)
    {
        return Y(n);
    }

    /// <summary>
    /// Always throws; a way's coordinate sequence is immutable.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.setOrdinate(int, int, double)</c>.</remarks>
    public override void SetOrdinate(int n, int ordinateIndex, double value)
    {
        throw new InvalidOperationException("Coordinates of a Way are immutable.");
    }

    /// <summary>
    /// Materializes the sequence as an array of coordinates.
    /// </summary>
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

    /// <summary>
    /// Returns a mutable, array-backed copy of this coordinate sequence.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.WayCoordinateSequence.copy()</c>.</remarks>
    public override CoordinateSequence Copy()
    {
        return new CoordinateArraySequence(ToCoordinateArray());
    }

}
