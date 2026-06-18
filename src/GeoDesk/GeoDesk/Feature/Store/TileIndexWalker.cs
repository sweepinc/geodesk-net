/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Numerics;

using GeoDesk.Feature.Filters;
using GeoDesk.Geom;

using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Feature.Store;

/// <summary>
/// A class that traverses the Tile Index Tree of a FeatureStore in an iterator-like fashion,
/// returning all tiles that intersect a given bounding box.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker</c>.</remarks>
internal class TileIndexWalker
{

    Bounds? _bounds;
    readonly NioBuffer _buf;
    readonly Level _root;
    Level _current;
    int _currentTile;
    int _currentTip;
    Filter? _filter;
    int _northwestFlags;
    HashSet<int>? _acceptedTiles;
    bool _tileBasedAcceleration;
    int _pTileIndex;

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker(ByteBuffer, int, int)</c>.</remarks>
    public TileIndexWalker(NioBuffer buf, int pTileIndex, int zoomLevels)
    {
        _buf = buf;
        _pTileIndex = pTileIndex;

        _current = _root = new Level();
        var level = _root;
        zoomLevels >>= 1;
        var zoom = 0;
        for (; ; )
        {
            var step = BitOperations.TrailingZeroCount((uint)zoomLevels) + 1;
            zoom += step;
            level.topLeftChildTile = Tile.FromColumnRowZoom(0, 0, zoom);
            level.extent = 1 << step;
            zoomLevels = (int)((uint)zoomLevels >> step);
            if (zoomLevels == 0) break;
            var child = new Level();
            level.child = child;
            child.parent = level;
            level = child;
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker(FeatureStore)</c>.</remarks>
    public TileIndexWalker(FeatureStore store)
        : this(store.TileIndexBuf(), store.TileIndexOfs(), store.ZoomLevels())
    {
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.start(Bounds)</c>.</remarks>
    public void Start(Bounds bounds)
    {
        Start(bounds, null);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.start(Bounds, Filter)</c>.</remarks>
    public void Start(Bounds bounds, Filter? filter)
    {
        _bounds = bounds;
        _filter = filter;
        _currentTip = 1;
        _root.Init(_buf, _pTileIndex + 4, 0, bounds, filter);
        _current = _root;
        _acceptedTiles = null;
        if (filter != null)
        {
            var strategy = filter.Strategy();
            if ((strategy & FilterStrategy.FastTileFilter) != 0)
            {
                _tileBasedAcceleration = true;
                if ((strategy & FilterStrategy.StrictBbox) == 0) _acceptedTiles = new HashSet<int>();
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.tileIndexPointer()</c>.</remarks>
    protected int TileIndexPointer()
    {
        return _pTileIndex;
    }

    // PORT: Java's tile() and filter() are renamed CurrentTile()/CurrentFilter() to avoid colliding
    // with the GeoDesk.Geom.Tile type and the Filter type referenced in this file.
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.tile()</c>.</remarks>
    public int CurrentTile()
    {
        return _currentTile;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.tip()</c>.</remarks>
    public int Tip()
    {
        return _currentTip;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.filter()</c>.</remarks>
    public Filter? CurrentFilter()
    {
        return _filter;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.northwestFlags()</c>.</remarks>
    public int NorthwestFlags()
    {
        return _northwestFlags;
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.tilePage()</c>.</remarks>
    public int TilePage()
    {
        var p = TileIndexPointer() + _currentTip * 4;
        var entry = _buf.GetInt(p);
        System.Diagnostics.Debug.Assert((entry & 3) != 1);
        return (int)((uint)entry >> 2);
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.next()</c>.</remarks>
    public bool Next()
    {
        var level = _current;
        var childTileMask = level.childTileMask;
        for (; ; )
        {
            level.currentCol++;
            if (level.currentCol > level.endCol)
            {
                level.currentRow++;
                if (level.currentRow > level.endRow)
                {
                    // we are done with this level
                    _current = level = level.parent!;
                    if (level == null) return false;     // We've completed the root; we are done
                    // continue with parent level
                    childTileMask = level.childTileMask;
                    continue;
                }
                level.currentCol = level.startCol;
            }
            var childNumber = level.currentRow * level.extent + level.currentCol;
            if ((childTileMask & (1L << childNumber)) != 0)
            {
                // If the bit in the childTileMask is set, this means that there is actually a tile
                // at this cell in the matrix. In the tile index, empty cells are skipped; if we have
                // a 4x4 matrix, and the mask bits are 0b0000_0000_0011_0100, this means the record
                // is laid out like this:
                //
                // [parent tile]
                // [childTileMask]
                // [child at row0, col2]
                // [child at row1, col0]
                // [child at row1, col1]

                var childEntry = BitOperations.PopCount((ulong)(childTileMask << (63 - childNumber))) - 1;
                // cannot shift by 64; only the lowest 5 bits count
                // TODO: could avoid -1 by setting pChildEntries one word earlier

                // by counting how many bits are set in the mask before the bit at childNumber, we
                // determine the position of this child's entry (This should be a very fast operation
                // on modern CPUs)

                _currentTile = Tile.Relative(level.topLeftChildTile, level.currentCol, level.currentRow);

                if (_tileBasedAcceleration)
                {
                    // If the Filter allows for tile-based acceleration (rejecting a tile, waiving the
                    // filter, or substituting the filter for a cheaper one), create a polygon for the
                    // current tile and check with the Filter

                    if (level.filter != null && (level.filter.Strategy() & FilterStrategy.FastTileFilter) != 0)
                    {
                        _filter = level.filter.FilterForTile(_currentTile, Tile.Polygon(_currentTile));
                        if (_filter == FalseFilter.Instance) continue;
                    }
                    if (_acceptedTiles != null)
                    {
                        _northwestFlags =
                            (_acceptedTiles.Contains(Tile.Neighbor(_currentTile, Heading.North)) ?
                                IFeatureFlags.MULTITILE_NORTH : 0) |
                            (_acceptedTiles.Contains(Tile.Neighbor(_currentTile, Heading.West)) ?
                                IFeatureFlags.MULTITILE_WEST : 0);
                        _acceptedTiles.Add(_currentTile);
                    }
                    else
                    {
                        // If we're not tracking accepted NW tiles (for filters that use a strict
                        // bbox), pretend that NW tiles exist
                        _northwestFlags = IFeatureFlags.MULTITILE_NORTH | IFeatureFlags.MULTITILE_WEST;
                    }
                }
                else
                {
                    // If we're processing a dense set of tiles, calculate the northwestFlags based
                    // on query bbox
                    // TODO: There's probably a cheaper way to calculate this

                    _northwestFlags =
                        ((_bounds!.MaxY > Tile.TopY(_currentTile)) ? IFeatureFlags.MULTITILE_NORTH : 0) |
                        ((_bounds.MinX < Tile.LeftX(_currentTile)) ? IFeatureFlags.MULTITILE_WEST : 0);
                }
                var pEntry = level.pChildEntries + childEntry * 4;
                var pageOrPtr = _buf.GetInt(pEntry);
                if ((pageOrPtr & 3) == 1)
                {
                    // Changed for v2: The lowest 2 bits are flags. A value of 01 indicates a pointer
                    // to a child level. Current tile has children: prepare to move up to the next
                    // level in the tile tree

                    _current = level = level.child!;
                    pEntry += pageOrPtr ^ 1;
                    level.Init(_buf, pEntry, _currentTile, _bounds!, _filter);
                }
                _currentTip = (pEntry - TileIndexPointer()) / 4;
                return true;
            }
        }
    }

    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.skipChildren()</c>.</remarks>
    public void SkipChildren()
    {
        _current = _current.parent != null ? _current.parent : _current;
    }

    // TODO: could the col/rows be shorts? Performance impact?
    /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.Level</c>.</remarks>
    class Level
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

        /// <remarks>Ported from Java <c>com.geodesk.feature.store.TileIndexWalker.Level.init(ByteBuffer, int, int, Bounds, Filter)</c>.</remarks>
        internal void Init(NioBuffer buf, int pEntry, int parentTile, Bounds bounds, Filter? filter)
        {
            this.filter = filter;
            var zoom = Tile.Zoom(topLeftChildTile);
                // TODO: check this, it has not been initialized?
                //  OK, this is set by TIW's constructor (This could be cleaner)
            var step = zoom - Tile.Zoom(parentTile);
            var tileTop = Tile.Row(parentTile) << step;
            var tileLeft = Tile.Column(parentTile) << step;
            topLeftChildTile = Tile.FromColumnRowZoom(tileLeft, tileTop, zoom);
            var left = Tile.ColumnFromXZ(bounds.MinX, zoom);
            var right = Tile.ColumnFromXZ(bounds.MaxX, zoom);
            var top = Tile.RowFromYZ(bounds.MaxY, zoom);
            var bottom = Tile.RowFromYZ(bounds.MinY, zoom);
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

}
