/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Numerics;
using GeoDesk.Feature;
using GeoDesk.Feature.Filters;
using GeoDesk.Geom;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A class that traverses the Tile Index Tree of a FeatureStore in an
/// iterator-like fashion, returning all tiles that intersect a given
/// bounding box.
/// </summary>
// TODO: no need to create top level (leaf)
public class TileIndexWalker
{
    private Bounds? bounds;
    private readonly NioBuffer buf;
    private readonly Level root;
    private Level current;
    private int currentTile;
    private int currentTip;
    private Filter? filter;
    private int northwestFlags;
    private HashSet<int>? acceptedTiles;
    private bool tileBasedAcceleration;
    private int pTileIndex;

    // TODO: could the col/rows be shorts? Performance impact?
    private class Level
    {
        internal Level? parent;
        internal Level? child;
        internal long childTileMask;
        internal int pChildEntries;
        internal int topLeftChildTile;
        internal int extent;     // TODO: do we need to store this?
        internal int startCol;
        internal int startRow;   // TODO: could drop this
        internal int endCol;
        internal int endRow;
        internal int currentCol;
        internal int currentRow;
        internal Filter? filter;

        internal void Init(NioBuffer buf, int pEntry, int parentTile, Bounds bounds, Filter? filter)
        {
            this.filter = filter;
            int zoom = Tile.Zoom(topLeftChildTile);
                // TODO: check this, it has not been initialized?
                //  OK, this is set by TIW's constructor (This could be cleaner)
            int step = zoom - Tile.Zoom(parentTile);
            // int extent = 1 << step;     // TODO: could take it from Level object
            int tileTop = Tile.Row(parentTile) << step;
            int tileLeft = Tile.Column(parentTile) << step;
            topLeftChildTile = Tile.FromColumnRowZoom(tileLeft, tileTop, zoom);
            int left = Tile.ColumnFromXZ(bounds.MinX, zoom);
            int right = Tile.ColumnFromXZ(bounds.MaxX, zoom);
            int top = Tile.RowFromYZ(bounds.MaxY, zoom);
            int bottom = Tile.RowFromYZ(bounds.MinY, zoom);
            startCol = System.Math.Max(left - tileLeft, 0);
            startRow = System.Math.Max(top - tileTop, 0);
            endCol = System.Math.Min(right - tileLeft, extent - 1);
            endRow = System.Math.Min(bottom - tileTop, extent - 1);
            currentCol = startCol - 1;
            currentRow = startRow;
            childTileMask = buf.GetLong(pEntry + 4);
                // TODO: This fails before gol-tool#7 is fixed
            pChildEntries = pEntry + (extent == 8 ? 12 : 8);
        }
    }

    public TileIndexWalker(NioBuffer buf, int pTileIndex, int zoomLevels)
    {
        this.buf = buf;
        this.pTileIndex = pTileIndex;

        current = root = new Level();
        Level level = root;
        zoomLevels >>= 1;
        int zoom = 0;
        for (; ; )
        {
            int step = BitOperations.TrailingZeroCount((uint)zoomLevels) + 1;
            zoom += step;
            level.topLeftChildTile = Tile.FromColumnRowZoom(0, 0, zoom);
            level.extent = 1 << step;
            zoomLevels = (int)((uint)zoomLevels >> step);
            if (zoomLevels == 0) break;
            Level child = new Level();
            level.child = child;
            child.parent = level;
            level = child;
        }
    }

    public TileIndexWalker(FeatureStore store)
        : this(store.TileIndexBuf(), store.TileIndexOfs(), store.ZoomLevels())
    {
    }

    public void Start(Bounds bounds)
    {
        Start(bounds, null);
    }

    public void Start(Bounds bounds, Filter? filter)
    {
        this.bounds = bounds;
        this.filter = filter;
        currentTip = 1;
        root.Init(buf, pTileIndex + 4, 0, bounds, filter);
        current = root;
        acceptedTiles = null;
        if (filter != null)
        {
            int strategy = filter.Strategy();
            if ((strategy & FilterStrategy.FAST_TILE_FILTER) != 0)
            {
                tileBasedAcceleration = true;
                if ((strategy & FilterStrategy.STRICT_BBOX) == 0)
                {
                    acceptedTiles = new HashSet<int>();
                }
            }
        }
    }

    protected int TileIndexPointer()
    {
        return pTileIndex;
    }

    // PORT: Java's tile() and filter() are renamed CurrentTile()/CurrentFilter() to avoid
    // colliding with the GeoDesk.Geom.Tile type and the Filter type referenced in this file.
    public int CurrentTile()
    {
        return currentTile;
    }

    public int Tip()
    {
        return currentTip;
    }

    public Filter? CurrentFilter()
    {
        return filter;
    }

    public int NorthwestFlags()
    {
        return northwestFlags;
    }

    public int TilePage()
    {
        int p = TileIndexPointer() + currentTip * 4;
        int entry = buf.GetInt(p);
        System.Diagnostics.Debug.Assert((entry & 3) != 1);
        return (int)((uint)entry >> 2);
    }

    public bool Next()
    {
        Level level = current;
        long childTileMask = level.childTileMask;
        for (; ; )
        {
            level.currentCol++;
            if (level.currentCol > level.endCol)
            {
                level.currentRow++;
                if (level.currentRow > level.endRow)
                {
                    // we are done with this level
                    current = level = level.parent!;
                    if (level == null)
                    {
                        // We've completed the root; we are done
                        return false;
                    }
                    // continue with parent level
                    childTileMask = level.childTileMask;
                    continue;
                }
                level.currentCol = level.startCol;
            }
            int childNumber = level.currentRow * level.extent + level.currentCol;
            if ((childTileMask & (1L << childNumber)) != 0)
            {
                // If the bit in the childTileMask is set,
                // this means that there is actually a tile
                // at this cell in the matrix
                // In the tile index, empty cells are skipped;
                // if we have a 4x4 matrix, and the mask bits
                // are 0b0000_0000_0011_0100, this means the
                // record is laid out like this:
                //
                // [parent tile]
                // [childTileMask]
                // [child at row0, col2]
                // [child at row1, col0]
                // [child at row1, col1]

                int childEntry = BitOperations.PopCount(
                    (ulong)(childTileMask << (63 - childNumber))) - 1;
                // cannot shift by 64; only the lowest 5 bits count
                // TODO: could avoid -1 by setting pChildEntries one word earlier

                // by counting how many bits are set in the
                // mask before the bit at childNumber, we
                // determine the position of this child's
                // entry (This should be a very fast operation
                // on modern CPUs)

                currentTile = Tile.Relative(level.topLeftChildTile,
                    level.currentCol, level.currentRow);
                // Log.debug("TIW: Current tile %s, Filter = %s", Tile.toString(currentTile), filter);

                if (tileBasedAcceleration)
                {
                    // If the Filter allows for tile-based acceleration (rejecting
                    // a tile, waiving the filter, or substituting the filter for
                    // a cheaper one), create a polygon for the current tile and
                    // check with the Filter

                    if (level.filter != null && (level.filter.Strategy() & FilterStrategy.FAST_TILE_FILTER) != 0)
                    {
                        filter = level.filter.FilterForTile(currentTile, GeoDesk.Geom.Tile.Polygon(currentTile));
                        if (filter == FalseFilter.INSTANCE) continue;
                    }
                    if (acceptedTiles != null)
                    {
                        northwestFlags =
                            (acceptedTiles.Contains(GeoDesk.Geom.Tile.Neighbor(currentTile, Heading.North)) ?
                                IFeatureFlags.MULTITILE_NORTH : 0) |
                            (acceptedTiles.Contains(GeoDesk.Geom.Tile.Neighbor(currentTile, Heading.West)) ?
                                IFeatureFlags.MULTITILE_WEST : 0);
                        acceptedTiles.Add(currentTile);
                    }
                    else
                    {
                        // If we're not tracking accepted NW tiles (for filters that
                        // use a strict bbox), pretend that NW tiles exist
                        northwestFlags = IFeatureFlags.MULTITILE_NORTH | IFeatureFlags.MULTITILE_WEST;
                    }
                }
                else
                {
                    // If we're processing a dense set of tiles, calculate the
                    // northwestFlags based on query bbox
                    // TODO: There's probably a cheaper way to calculate this

                    northwestFlags =
                        ((bounds!.MaxY > GeoDesk.Geom.Tile.TopY(currentTile)) ?
                            IFeatureFlags.MULTITILE_NORTH : 0) |
                        ((bounds.MinX < GeoDesk.Geom.Tile.LeftX(currentTile)) ?
                            IFeatureFlags.MULTITILE_WEST : 0);
                }
                int pEntry = level.pChildEntries + childEntry * 4;
                int pageOrPtr = buf.GetInt(pEntry);
                if ((pageOrPtr & 3) == 1)
                {
                    // Changed for v2: The lowest 2 bits
                    //  are flags. A value of 01 indicates a pointer
                    //  to a child level

                    // current tile has children: prepare to move up to the
                    // next level in the tile tree

                    current = level = level.child!;
                    pEntry += pageOrPtr ^ 1;
                    level.Init(buf, pEntry, currentTile, bounds!, filter);
                }
                currentTip = (pEntry - TileIndexPointer()) / 4;
                return true;
            }
        }
    }

    public void SkipChildren()
    {
        current = current.parent != null ? current.parent : current;
    }
}
