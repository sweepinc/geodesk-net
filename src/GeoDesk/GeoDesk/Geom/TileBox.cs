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

    public TileBox()
    {
        topLeft = -1;
        bottomRight = -1;
    }

    public int Width => Tile.Column(bottomRight) - Tile.Column(topLeft) + 1;

    public int Height => Tile.Row(bottomRight) - Tile.Row(topLeft) + 1;

    public int Left => Tile.Column(topLeft);

    public int Top => Tile.Row(topLeft);

    public int Right => Tile.Column(bottomRight);

    public int Bottom => Tile.Row(bottomRight);

    public int Size => Width * Height;

    public int Zoom => Tile.Zoom(topLeft);

    public void ZoomOut(int newZoom)
    {
        int currentZoom = Zoom;
        Debug.Assert(newZoom <= currentZoom);
        if (topLeft == -1 || newZoom == currentZoom) return;
        topLeft = Tile.ZoomedOut(topLeft, newZoom);
        bottomRight = Tile.ZoomedOut(bottomRight, newZoom);
    }

    public void ExpandToInclude(int tile)
    {
        if (topLeft == -1)
        {
            topLeft = tile;
            bottomRight = tile;
            return;
        }
        int zoom = Tile.Zoom(tile);
        int currentZoom = Zoom;
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
        int col = Tile.Column(tile);
        int row = Tile.Row(tile);
        int left = Tile.Column(topLeft);
        int top = Tile.Row(topLeft);
        int right = Tile.Column(bottomRight);
        int bottom = Tile.Row(bottomRight);
        topLeft = Tile.FromColumnRowZoom(System.Math.Min(col, left), System.Math.Min(row, top), zoom);
        bottomRight = Tile.FromColumnRowZoom(System.Math.Max(col, right), System.Math.Max(row, bottom), zoom);
    }

    public void Clear()
    {
        topLeft = -1;
        bottomRight = -1;
    }

    public void ForEach(Action<int> func)
    {
        int zoom = Tile.Zoom(topLeft);
        int top = Top;
        int left = Left;
        int right = Right;
        int bottom = Bottom;

        for (int row = top; row <= bottom; row++)
        {
            for (int col = left; col <= right; col++)
            {
                func(Tile.FromColumnRowZoom(col, row, zoom));
            }
        }
    }
}
