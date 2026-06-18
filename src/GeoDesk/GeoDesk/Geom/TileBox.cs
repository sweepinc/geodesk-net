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
/// <remarks>Ported from Java <c>com.geodesk.geom.TileBox</c>.</remarks>
public class TileBox
{

    protected int topLeft;
    protected int bottomRight;

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox()</c>.</remarks>
    public TileBox()
    {
        topLeft = -1;
        bottomRight = -1;
    }

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.width()</c>.</remarks>
    public int Width => Tile.Column(bottomRight) - Tile.Column(topLeft) + 1;

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.height()</c>.</remarks>
    public int Height => Tile.Row(bottomRight) - Tile.Row(topLeft) + 1;

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.left()</c>.</remarks>
    public int Left => Tile.Column(topLeft);

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.top()</c>.</remarks>
    public int Top => Tile.Row(topLeft);

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.right()</c>.</remarks>
    public int Right => Tile.Column(bottomRight);

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.bottom()</c>.</remarks>
    public int Bottom => Tile.Row(bottomRight);

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.size()</c>.</remarks>
    public int Size => Width * Height;

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.zoom()</c>.</remarks>
    public int Zoom => Tile.Zoom(topLeft);

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.zoomOut(int)</c>.</remarks>
    public void ZoomOut(int newZoom)
    {
        var currentZoom = Zoom;
        Debug.Assert(newZoom <= currentZoom);
        if (topLeft == -1 || newZoom == currentZoom) return;
        topLeft = Tile.ZoomedOut(topLeft, newZoom);
        bottomRight = Tile.ZoomedOut(bottomRight, newZoom);
    }

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

    /// <remarks>Ported from Java <c>com.geodesk.geom.TileBox.clear()</c>.</remarks>
    public void Clear()
    {
        topLeft = -1;
        bottomRight = -1;
    }

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
