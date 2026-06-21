/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Feature.Store;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A shortcut filter that spatial predicates can supply for tiles that are properly contained
/// within the test geometry, in order to avoid more complex topological tests. If a feature lies
/// completely within the tile (i.e. it is not multi-tile), it can be quickly accepted/rejected;
/// for multi-tile features, the original Filter is applied.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.FastTileFilter</c>.</remarks>
// TODO: don't apply to nodes (Nodes can never be multi-tile)
internal class FastTileFilter : IFilter
{

    readonly bool _fastAccept;
    readonly IFilter _slowFilter;
    readonly int _tileMaxX;
    readonly int _tileMinY;

    /// <summary>
    /// Creates a fast tile filter for the given tile: single-tile features are accepted
    /// or rejected per <paramref name="fastAccept"/>, while multi-tile features fall
    /// back to <paramref name="slowFilter"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.FastTileFilter(int, boolean, Filter)</c>.</remarks>
    public FastTileFilter(int tile, bool fastAccept, IFilter slowFilter)
    {
        _fastAccept = fastAccept;
        _slowFilter = slowFilter;
        _tileMaxX = Tile.RightX(tile);
        _tileMinY = Tile.BottomY(tile);
    }

    /// <summary>Returns true if the feature is accepted, materializing geometry only if needed.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.FastTileFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        return Accept(feature, null!);
    }

    /// <summary>
    /// Returns the fast verdict for a feature lying entirely within the tile, otherwise
    /// delegates to the slow filter with the feature's geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.FastTileFilter.accept(Feature, Geometry)</c>.</remarks>
    public bool Accept(IFeature feature, Geometry geom)
    {
        // TODO: Would be useful if `Feature` implemented `Bounds` for this
        var sf = (StoredFeature)feature;
        if ((sf.Flags() & FeatureFlags.MULTITILE_FLAGS) == 0)
        {
            var b = sf.Bounds;
            if (b.MinY >= _tileMinY && b.MaxX <= _tileMaxX) return _fastAccept;
        }
        if (geom == null) geom = feature.ToGeometry();
        return _slowFilter.Accept(feature, geom);
    }

}
