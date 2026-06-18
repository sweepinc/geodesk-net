/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using GeoDesk.Feature.Store;
using GeoDesk.Util;
using NetTopologySuite.Geometries;

namespace GeoDesk.Geom;

internal static class Tile
{
    public static int Row(int tile)
    {
        return (tile >> 12) & 0xfff;
    }

    public static int Column(int tile)
    {
        return tile & 0xfff;
    }

    public static int Zoom(int tile)
    {
        return (int)((uint)tile >> 24) & 0xf; // mask off nibble to allow top nibble to be used for flags
    }

    public static int TilesAtZoom(int zoom)
    {
        return (1 << zoom) << zoom;
    }

    /// <summary>
    /// Returns the width/height of a tile at the given zoom level
    /// </summary>
    /// <param name="zoom">a valid zoom level</param>
    /// <returns>width/height in pixels</returns>
    public static long SizeAtZoom(int zoom)
    {
        return 1L << (32 - zoom);
        // This must be a long since int overflows for zoom 0 and zoom 1
    }

    public static int ZoomFromSize(long size)
    {
        return BitOperations.LeadingZeroCount((ulong)size) - 31;
    }

    /// <summary>
    /// Creates a tile number. This method does not check whether
    /// the given column, row, and zoom level are valid.
    /// </summary>
    public static int FromColumnRowZoom(int col, int row, int zoom)
    {
        return (zoom << 24) | (row << 12) | col;
    }

    /// <summary>
    /// Determines the tile to which a coordinate belongs;
    /// coordinates must be in the projection used by the Mercator class.
    /// </summary>
    public static int FromXYZ(int x, int y, int zoom)
    {
        int col = ColumnFromXZ(x, zoom);
        int row = RowFromYZ(y, zoom);
        return FromColumnRowZoom(col, row, zoom);
    }

    public static int ColumnFromXZ(int x, int zoom)
    {
        return (int)(((long)x + (1L << 31)) >> (32 - zoom));
    }

    public static int RowFromYZ(int y, int zoom)
    {
        return (int)(((long)int.MaxValue - y) >> (32 - zoom));
    }

    public static int FromXYZ(double x, double y, int zoom)
    {
        return FromXYZ((int)System.Math.Floor(x + 0.5), (int)System.Math.Floor(y + 0.5), zoom);
    }

    /// <summary>
    /// Checks if the given number represents a valid tile number
    /// </summary>
    public static bool IsValid(int tile)
    {
        int zoom = Zoom(tile);
        if (zoom > 12) return false;
        int maxRowCols = 1 << zoom;
        return Column(tile) < maxRowCols && Row(tile) < maxRowCols;
    }

    /// <summary>
    /// Returns the leftmost (lowest) x-coordinate that lies within the given tile
    /// </summary>
    public static int LeftX(int tile)
    {
        int zoom = Zoom(tile);
        int col = Column(tile);
        return (col - (1 << (zoom - 1))) << (32 - zoom);
    }

    /// <summary>
    /// Returns the rightmost (highest) x-coordinate that lies within the given tile
    /// </summary>
    public static int RightX(int tile)
    {
        int left = LeftX(tile);
        int zoom = Zoom(tile);
        long extent = 1L << (32 - zoom);
        return (int)(left + extent - 1);
    }

    /// <summary>
    /// Returns the bottom (lowest) y-coordinate that lies within the given tile.
    /// Remember, going from top to bottom, tile rows *increase*, while
    /// y-coordinates *decrease*.
    /// </summary>
    public static int BottomY(int tile)
    {
        return int.MinValue - (int)((long)(Row(tile) + 1) << (32 - Zoom(tile)));
            // << 32 wraps around for int, that's why we cast to long
    }

    public static int TopY(int tile)
    {
        return int.MaxValue - (Row(tile) << (32 - Zoom(tile)));
    }

    /// <summary>
    /// Returns the tile that contains this tile at the specified
    /// (lower) zoom level. If the zoom level is the same, the
    /// tile itself is returned.
    /// </summary>
    public static int ZoomedOut(int tile, int zoom)
    {
        int currentZoom = Zoom(tile);
        Debug.Assert(currentZoom >= zoom, string.Format(CultureInfo.InvariantCulture, "Can't zoom out from {0} to {1}", currentZoom, zoom));
        int delta = currentZoom - zoom;
        return FromColumnRowZoom(Column(tile) >> delta, Row(tile) >> delta, zoom);
    }

    /// <summary>
    /// Returns the tile number of an adjacent tile that lies
    /// in the specified direction.
    /// </summary>
    public static int Neighbor(int fromTile, Heading direction)
    {
        int zoom = Zoom(fromTile);
        int col = Column(fromTile);
        int row = Row(fromTile);
        int mask = (1 << zoom) - 1;
        col = (col + direction.EastFactor) & mask;
        row = (row - direction.NorthFactor) & mask;
            // Heading assumes planar coordinates (north increases),
            // while tiles use screen coordinates (north decreases)
        return FromColumnRowZoom(col, row, zoom);
    }

    public static Box Bounds(int tile)
    {
        int zoom = Zoom(tile);
        int left = LeftX(tile);
        int bottom = BottomY(tile);
        long extent = 1L << (32 - zoom);

        return new Box(left, bottom, (int)(left + extent - 1), (int)(bottom + extent - 1));
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.Tile.polygon(int)</c>.</remarks>
    public static Polygon Polygon(int tile)
    {
        int zoom = Zoom(tile);
        int left = LeftX(tile);
        int bottom = BottomY(tile);
        long extent = 1L << (32 - zoom);
        return GeometryBuilder.Instance.CreatePolygon(
            new BoxCoordinateSequence(left, bottom,
                (int)(left + extent - 1), (int)(bottom + extent - 1)));
    }

    /// <summary>
    /// Returns the top-left tile occupied by a bounding box.
    /// </summary>
    public static int TopLeft(IBounds bbox, int zoom)
    {
        return FromXYZ(bbox.MinX, bbox.MaxY, zoom);
    }

    /// <summary>
    /// Returns the bottom-right tile occupied by a bounding box.
    /// </summary>
    public static int BottomRight(IBounds bbox, int zoom)
    {
        return FromXYZ(bbox.MaxX, bbox.MinY, zoom);
    }

    public static string ToString(int tile)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0}/{1}/{2}", Zoom(tile), Column(tile), Row(tile));
    }

    /// <summary>
    /// Parses a tile number from a String. Valid formats are:
    /// "zoom/column/row" or "column/row" (assumes zoom 12)
    /// </summary>
    /// <returns>the tile number, or -1 if the String does not represent a valid tile number</returns>
    public static int FromString(string s)
    {
        int zoom;
        string colString, rowString;
        int n = s.IndexOf('/');
        if (n < 0) return -1;
        int n2 = s.IndexOf('/', n + 1);
        try
        {
            if (n2 < 0)
            {
                zoom = 12;
                colString = s.Substring(0, n);
                rowString = s.Substring(n + 1);
            }
            else
            {
                zoom = int.Parse(s.Substring(0, n), CultureInfo.InvariantCulture);
                if (zoom < 0 || zoom > 12) return -1;
                colString = s.Substring(n + 1, n2 - (n + 1));
                rowString = s.Substring(n2 + 1);
            }
            int col = int.Parse(colString, CultureInfo.InvariantCulture);
            int row = int.Parse(rowString, CultureInfo.InvariantCulture);
            int extent = (1 << zoom);
            if (col < 0 || col >= extent || row < 0 || row >= extent) return -1;
            return FromColumnRowZoom(col, row, zoom);
        }
        catch (FormatException)
        {
            return -1;
        }
        catch (OverflowException)
        {
            return -1;
        }
    }

    /// <summary>
    /// Checks whether the tile would be black, if we imagine the tile grid being laid
    /// out like a checkerboard (with the top left tile being white).
    /// </summary>
    public static bool IsBlack(int tile)
    {
        return ((tile ^ (tile >> 12)) & 1) != 0;
    }

    public static Envelope Intersection(int tile, Envelope env)
    {
        int extent = 1 << (32 - Zoom(tile));
        int tileMinX = LeftX(tile);
        int tileMinY = BottomY(tile);
        int tileMaxX = tileMinX + extent - 1;
        int tileMaxY = tileMinY + extent - 1;
        return new Envelope(
            tileMinX > env.MinX ? tileMinX : env.MinX,
            tileMaxX < env.MaxX ? tileMaxX : env.MaxX,
            tileMinY > env.MinY ? tileMinY : env.MinY,
            tileMaxY < env.MaxY ? tileMaxY : env.MaxY);
    }

    /// <summary>
    /// Calculates a new BoundingBox that represents the area of overlap
    /// between the given bounds and the bounding box of a tile
    /// </summary>
    public static Box Intersection(int tile, IBounds bounds)
    {
        int extent = 1 << (32 - Zoom(tile));
        int tileMinX = LeftX(tile);
        int tileMinY = BottomY(tile);
        int tileMaxX = tileMinX + extent - 1;
        int tileMaxY = tileMinY + extent - 1;
        return new Box(
            tileMinX > bounds.MinX ? tileMinX : bounds.MinX,
            tileMinY > bounds.MinY ? tileMinY : bounds.MinY,
            tileMaxX < bounds.MaxX ? tileMaxX : bounds.MaxX,
            tileMaxY < bounds.MaxY ? tileMaxY : bounds.MaxY);
    }

    /// <summary>
    /// Returns the tile occupied by the given bounding box, or -1 if its extends
    /// across multiple tiles.
    /// </summary>
    public static int FromBounds(IBounds bbox, int zoom)
    {
        int topLeft = TopLeft(bbox, zoom);
        int bottomRight = BottomRight(bbox, zoom);
        return topLeft == bottomRight ? topLeft : -1;
    }

    // not used
    public static TileBox ChildrenOfTileAtZoom(int tile, int zoom)
    {
        int levels = zoom - Zoom(tile);
        Debug.Assert(levels >= 0);
        int top = Row(tile) << levels;
        int left = Column(tile) << levels;
        int size = 1 << levels;
        TileBox box = new TileBox();
        box.ExpandToInclude(FromColumnRowZoom(left, top, zoom));
        box.ExpandToInclude(FromColumnRowZoom(left + size - 1, top + size - 1, zoom));
        return box;
    }

    // TODO: only works for positive deltas, and does not wrap!
    public static int Relative(int tile, int deltaCol, int deltaRow)
    {
        return tile + (deltaRow << 12) + deltaCol;
    }
}
