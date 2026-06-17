/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Geom;

/// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder</c>.</remarks>
public class BoxBuilder : Bounds
{

    int _minX;
    int _minY;
    int _maxX;
    int _maxY;

    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.reset()</c>.</remarks>
    public void Reset()
    {
        _minX = int.MaxValue;
        _minY = int.MaxValue;
        _maxX = int.MinValue;
        _maxY = int.MinValue;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.isNull()</c>.</remarks>
    public bool IsNull()
    {
        return _maxY < _minY;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.minX()</c>.</remarks>
    public int MinX => _minX;

    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.minY()</c>.</remarks>
    public int MinY => _minY;

    /// <remarks>Ported from Java <c>com.geodesk.geom.BoxBuilder.maxX()</c>.</remarks>
    public int MaxX => _maxX;

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
    public void ExpandToInclude(Bounds b)
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
