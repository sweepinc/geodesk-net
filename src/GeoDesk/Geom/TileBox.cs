/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Diagnostics;

namespace GeoDesk.Geom;

// used only by tile.childrenOfTileAtZoom
/// <summary>
/// A mutable rectangular range of map tiles at a single zoom level, tracked by its top-left and
/// bottom-right tile. Supports expanding to include tiles (zooming out to a common level as needed),
/// querying its dimensions, and iterating over the tiles it covers.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.TileBox</c>.</remarks>
internal class TileBox
{

    protected int topLeft;
    protected int bottomRight;

    /// <summary>
    /// Creates an empty tile box (no tiles included yet).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox()</c>.</remarks>
    public TileBox()
    {
        topLeft = -1;
        bottomRight = -1;
    }

    /// <summary>
    /// The width of the box in tile columns.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.width()</c>.</remarks>
    public int Width => Tile.Column(bottomRight) - Tile.Column(topLeft) + 1;

    /// <summary>
    /// The height of the box in tile rows.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.height()</c>.</remarks>
    public int Height => Tile.Row(bottomRight) - Tile.Row(topLeft) + 1;

    /// <summary>
    /// The leftmost tile column.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.left()</c>.</remarks>
    public int Left => Tile.Column(topLeft);

    /// <summary>
    /// The topmost tile row.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.top()</c>.</remarks>
    public int Top => Tile.Row(topLeft);

    /// <summary>
    /// The rightmost tile column.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.right()</c>.</remarks>
    public int Right => Tile.Column(bottomRight);

    /// <summary>
    /// The bottommost tile row.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.bottom()</c>.</remarks>
    public int Bottom => Tile.Row(bottomRight);

    /// <summary>
    /// The total number of tiles in the box (width times height).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.size()</c>.</remarks>
    public int Size => Width * Height;

    /// <summary>
    /// The zoom level of the tiles in the box.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.zoom()</c>.</remarks>
    public int Zoom => Tile.Zoom(topLeft);

    /// <summary>
    /// Lowers the box to a coarser zoom level, replacing its corner tiles with their ancestors at
    /// <paramref name="newZoom"/>. The target zoom must not be finer than the current one.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.zoomOut(int)</c>.</remarks>
    public void ZoomOut(int newZoom)
    {
        var currentZoom = Zoom;
        Debug.Assert(newZoom <= currentZoom);
        if (topLeft == -1 || newZoom == currentZoom) return;
        topLeft = Tile.ZoomedOut(topLeft, newZoom);
        bottomRight = Tile.ZoomedOut(bottomRight, newZoom);
    }

    /// <summary>
    /// Grows the box so it covers the given tile, zooming the box (or the tile) out to a common level
    /// first if their zoom levels differ.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.expandToInclude(int)</c>.</remarks>
    public void ExpandToInclude(int tile)
    {
        if (topLeft == -1)
        {
            topLeft = tile;
            bottomRight = tile;
            return;
        }
        var zoom = Tile.Zoom(tile);
        var currentZoom = Zoom;
        if (zoom != currentZoom)
        {
            if (zoom < currentZoom)
            {
                ZoomOut(zoom);
            }
            else
            {
                tile = Tile.ZoomedOut(tile, currentZoom);
            }
        }
        var col = Tile.Column(tile);
        var row = Tile.Row(tile);
        var left = Tile.Column(topLeft);
        var top = Tile.Row(topLeft);
        var right = Tile.Column(bottomRight);
        var bottom = Tile.Row(bottomRight);
        topLeft = Tile.FromColumnRowZoom(System.Math.Min(col, left), System.Math.Min(row, top), zoom);
        bottomRight = Tile.FromColumnRowZoom(System.Math.Max(col, right), System.Math.Max(row, bottom), zoom);
    }

    /// <summary>
    /// Resets the box to empty.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.clear()</c>.</remarks>
    public void Clear()
    {
        topLeft = -1;
        bottomRight = -1;
    }

    /// <summary>
    /// Invokes the given action once for every tile in the box, iterating row by row.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.forEach(IntConsumer)</c>.</remarks>
    public void ForEach(Action<int> func)
    {
        var zoom = Tile.Zoom(topLeft);
        var top = Top;
        var left = Left;
        var right = Right;
        var bottom = Bottom;

        for (var row = top; row <= bottom; row++)
        {
            for (var col = left; col <= right; col++)
            {
                func(Tile.FromColumnRowZoom(col, row, zoom));
            }
        }
    }

}
