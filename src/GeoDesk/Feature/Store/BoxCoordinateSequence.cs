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
/// <summary>
/// A read-only <see cref="CoordinateSequence"/> that represents an axis-aligned
/// bounding box as the five corner points of a closed ring, computed on demand from
/// the stored extent. Used to expose a box as a polygon geometry without allocating
/// coordinate arrays.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence</c>.</remarks>
internal class BoxCoordinateSequence : CoordinateSequence
{

    readonly int _minX;
    readonly int _minY;
    readonly int _maxX;
    readonly int _maxY;

    /// <summary>
    /// Creates a coordinate sequence spanning the box defined by the given minimum
    /// and maximum extents.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence(int, int, int, int)</c>.</remarks>
    public BoxCoordinateSequence(int minX, int minY, int maxX, int maxY)
        : base(5, 2, 0)
    {
        _minX = minX;
        _minY = minY;
        _maxX = maxX;
        _maxY = maxY;
    }

    /// <summary>
    /// Creates a coordinate sequence spanning the extent of the given bounds.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence(Bounds)</c>.</remarks>
    public BoxCoordinateSequence(IBounds b)
        : this(b.MinX, b.MinY, b.MaxX, b.MaxY)
    {
    }

    /// <summary>
    /// Returns the X ordinate of the n-th corner of the box ring, cycling through the
    /// corners so that the ring closes back to its start.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.x(int)</c>.</remarks>
    public int X(int n)
    {
        return (((n + 1) & 2) != 0) ? _minX : _maxX;
    }

    /// <summary>
    /// Returns the Y ordinate of the n-th corner of the box ring, cycling through the
    /// corners so that the ring closes back to its start.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.y(int)</c>.</remarks>
    public int Y(int n)
    {
        return ((n & 2) != 0) ? _minY : _maxY;
    }

    /// <summary>
    /// Expands the given envelope to include both corners of this box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.expandEnvelope(Envelope)</c>.</remarks>
    public override Envelope ExpandEnvelope(Envelope env)
    {
        env.ExpandToInclude(_minX, _minY);
        env.ExpandToInclude(_maxX, _maxY);
        return env;
    }

    /// <summary>
    /// Returns a new <see cref="Coordinate"/> for the n-th corner of the box ring.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.getCoordinate(int)</c>.</remarks>
    public override Coordinate GetCoordinate(int n)
    {
        return new Coordinate(X(n), Y(n));
    }

    /// <summary>
    /// Copies the n-th corner's ordinates into the supplied coordinate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.getCoordinate(int, Coordinate)</c>.</remarks>
    public override void GetCoordinate(int n, Coordinate c)
    {
        c.X = X(n);
        c.Y = Y(n);
    }

    /// <summary>
    /// Returns a fresh copy of the n-th corner coordinate; the box is immutable so no
    /// distinct backing storage exists.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.getCoordinateCopy(int)</c>.</remarks>
    public override Coordinate GetCoordinateCopy(int n)
    {
        return GetCoordinate(n);
    }

    /// <summary>
    /// Returns either the X (ordinate index 0) or Y ordinate of the n-th corner.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.getOrdinate(int, int)</c>.</remarks>
    public override double GetOrdinate(int n, int ordinateIndex)
    {
        return ordinateIndex == 0 ? X(n) : Y(n);
    }

    /// <summary>
    /// Returns the X ordinate of the n-th corner as a double.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.getX(int)</c>.</remarks>
    public override double GetX(int n)
    {
        return X(n);
    }

    /// <summary>
    /// Returns the Y ordinate of the n-th corner as a double.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.getY(int)</c>.</remarks>
    public override double GetY(int n)
    {
        return Y(n);
    }

    /// <summary>
    /// Always throws; the box coordinate sequence is immutable and does not support
    /// ordinate assignment.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.setOrdinate(int, int, double)</c>.</remarks>
    public override void SetOrdinate(int n, int ordinateIndex, double value)
    {
        throw new InvalidOperationException("Coordinates are immutable.");
    }

    /// <summary>
    /// Materializes the box ring as an array of coordinates.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.toCoordinateArray()</c>.</remarks>
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
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.BoxCoordinateSequence.copy()</c>.</remarks>
    public override CoordinateSequence Copy()
    {
        return new CoordinateArraySequence(ToCoordinateArray());
    }

}
