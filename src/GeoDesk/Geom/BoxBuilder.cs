/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

/// <summary>
/// A mutable accumulator for building a bounding box incrementally by expanding it to include points
/// or other boxes. Implements <see cref="IBounds"/> so it can be read as a box once populated. Results
/// are undefined for boxes that straddle the antimeridian.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder</c>.</remarks>
internal class BoxBuilder : IBounds
{

    int _minX;
    int _minY;
    int _maxX;
    int _maxY;

    /// <summary>
    /// Resets the builder to the empty (null) state, so the next expansion establishes the box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.reset()</c>.</remarks>
    public void Reset()
    {
        _minX = int.MaxValue;
        _minY = int.MaxValue;
        _maxX = int.MinValue;
        _maxY = int.MinValue;
    }

    /// <summary>
    /// Returns true if the builder is empty (no points have been included since the last reset).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.isNull()</c>.</remarks>
    public bool IsNull()
    {
        return _maxY < _minY;
    }

    /// <summary>
    /// The current minimum X coordinate of the accumulated box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.minX()</c>.</remarks>
    public int MinX => _minX;

    /// <summary>
    /// The current minimum Y coordinate of the accumulated box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.minY()</c>.</remarks>
    public int MinY => _minY;

    /// <summary>
    /// The current maximum X coordinate of the accumulated box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.maxX()</c>.</remarks>
    public int MaxX => _maxX;

    /// <summary>
    /// The current maximum Y coordinate of the accumulated box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.maxY()</c>.</remarks>
    public int MaxY => _maxY;

    /// <summary>
    /// Checks if this bounding box includes the given coordinate, and expands it if necessary. If this
    /// bounding box straddles the Antimeridian, the results of this method are undefined (as it cannot
    /// tell in which direction the box should be expanded).
    /// </summary>
    /// <param name="x">X-coordinate</param>
    /// <param name="y">Y-coordinate</param>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.expandToInclude(int, int)</c>.</remarks>
    public void ExpandToInclude(int x, int y)
    {
        if (x < _minX) _minX = x;
        if (x > _maxX) _maxX = x;
        if (y < _minY) _minY = y;
        if (y > _maxY) _maxY = y;
    }

    /// <summary>
    /// Checks if this bounding box includes another bounding box, and expands it if necessary. If
    /// either bounding box straddles the Antimeridian, the results of this method are undefined (as it
    /// cannot tell in which direction the box should be expanded).
    /// </summary>
    /// <param name="b">the bounding box to include into this</param>
    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.expandToInclude(Bounds)</c>.</remarks>
    public void ExpandToInclude(IBounds b)
    {
        var otherMinX = b.MinX;
        var otherMinY = b.MinY;
        var otherMaxX = b.MaxX;
        var otherMaxY = b.MaxY;
        if (otherMinX < _minX) _minX = otherMinX;
        if (otherMinY < _minY) _minY = otherMinY;
        if (otherMaxX > _maxX) _maxX = otherMaxX;
        if (otherMaxY > _maxY) _maxY = otherMaxY;
    }

}
