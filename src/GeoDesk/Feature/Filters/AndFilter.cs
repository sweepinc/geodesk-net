/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using GeoDesk.Feature;
using GeoDesk.Geom;
using NetTopologySuite.Geometries;

namespace GeoDesk.Feature.Filters;

/// <summary>
/// A Filter that combines two Filters.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter</c>.</remarks>
internal class AndFilter : IFilter
{

    /// <summary>
    /// Combines two filters into one that accepts features only when both accept,
    /// merging their strategies, accepted types, and bounding boxes. Returns
    /// <see cref="FalseFilter"/> when the combination can never match.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter.create(Filter, Filter)</c>.</remarks>
    public static IFilter Create(IFilter left, IFilter right)
    {
        var leftStrategy = left.Strategy;
        var rightStrategy = right.Strategy;
        var leftStrictBounds = leftStrategy & FilterStrategy.StrictBbox;
        var rightStrictBounds = rightStrategy & FilterStrategy.StrictBbox;

        // combine all strategy flags, except strict-bbox; the combined filter is only strict-bbox
        // if both are strict-bbox
        // TODO: what about needs-geometry?
        var combinedStrategy = ((leftStrategy | rightStrategy) & ~FilterStrategy.StrictBbox) |
            (leftStrictBounds & rightStrictBounds);

        var acceptedTypes = left.AcceptedTypes & right.AcceptedTypes;
        if (acceptedTypes == 0) return FalseFilter.Instance;

        IBounds bounds;
        if ((combinedStrategy & FilterStrategy.UsesBbox) != 0)
        {
            // TODO: check for null bbox or enforce filter.bounds() returning World bbox if bbox
            //  not in use
            var leftBounds = left.Bounds;
            var rightBounds = right.Bounds;

            if ((leftStrictBounds | rightStrictBounds) != 0)
            {
                bounds = Box.Intersection(leftBounds, rightBounds);
                if (Box.IsNullBounds(bounds)) return FalseFilter.Instance;
            }
            else
            {
                bounds = Box.Smaller(leftBounds, rightBounds);
            }
        }
        else
        {
            bounds = Box.OfWorld();
        }
        return new AndFilter(left, right, combinedStrategy, bounds, acceptedTypes);
    }

    readonly IFilter _left;
    readonly IFilter _right;
    readonly int _strategy;
    readonly int _acceptedTypes;
    readonly IBounds _bounds;

    /// <summary>
    /// Creates the combined filter from its two operands and their pre-computed merged
    /// strategy, bounds, and accepted types.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter(Filter, Filter, int, Bounds, int)</c>.</remarks>
    public AndFilter(IFilter left, IFilter right, int strategy, IBounds bounds, int acceptedTypes)
    {
        _left = left;
        _right = right;
        _strategy = strategy;
        _acceptedTypes = acceptedTypes;
        _bounds = bounds;
    }

    /// <summary>
    /// Returns true if both filters accept the feature, materializing its geometry
    /// only when the combined strategy requires it.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter.accept(Feature)</c>.</remarks>
    public bool Accept(IFeature feature)
    {
        if ((_strategy & FilterStrategy.NeedsGeometry) != 0) return Accept(feature, feature.ToGeometry());
        return Accept(feature, null!);
    }

    /// <summary>
    /// Returns true if both filters accept the feature with the given materialized
    /// geometry.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter.accept(Feature, Geometry)</c>.</remarks>
    public bool Accept(IFeature feature, Geometry geom)
    {
        return _left.Accept(feature, geom) && _right.Accept(feature, geom);
    }

    /// <summary>The merged filter strategy flags.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter.strategy()</c>.</remarks>
    public int Strategy => _strategy;

    /// <summary>The intersection of the two filters' accepted feature types.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter.acceptedTypes()</c>.</remarks>
    public int AcceptedTypes => _acceptedTypes;

    /// <summary>The combined bounding box that candidates must fall within.</summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter.bounds()</c>.</remarks>
    public IBounds Bounds => _bounds;

    /// <summary>
    /// Returns a per-tile specialization of this filter by specializing both operands;
    /// collapses to false, to a single operand, or to a new combined filter as
    /// appropriate.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.feature.filter.AndFilter.filterForTile(int, Polygon)</c>.</remarks>
    public IFilter? FilterForTile(int tile, Polygon tileGeometry)
    {
        var newLeft = _left.FilterForTile(tile, tileGeometry);
        if (newLeft == FalseFilter.Instance) return FalseFilter.Instance;
        var newRight = _right.FilterForTile(tile, tileGeometry);
        if (newRight == FalseFilter.Instance) return FalseFilter.Instance;
        if (newLeft == null) return newRight;
        if (newRight == null) return newLeft;
        if (newLeft == _left && newRight == _right) return this;
        return Create(newLeft, newRight);
            // TODO: don't need to AND types and bbox, since these are only used at beginning of
            //  filtering (not applied on a per-tile basis)
    }

}
