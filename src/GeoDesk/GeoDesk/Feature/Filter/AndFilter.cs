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
public class AndFilter : Filter
{
    private readonly Filter left;
    private readonly Filter right;
    private readonly int strategy;
    private readonly int acceptedTypes;
    private readonly Bounds bounds;

    public AndFilter(Filter left, Filter right, int strategy, Bounds bounds, int acceptedTypes)
    {
        this.left = left;
        this.right = right;
        this.strategy = strategy;
        this.acceptedTypes = acceptedTypes;
        this.bounds = bounds;
    }

    public bool Accept(Feature feature)
    {
        if ((strategy & FilterStrategy.NEEDS_GEOMETRY) != 0)
        {
            return Accept(feature, feature.ToGeometry());
        }
        return Accept(feature, null!);
    }

    public bool Accept(Feature feature, Geometry geom)
    {
        return left.Accept(feature, geom) && right.Accept(feature, geom);
    }

    public int Strategy()
    {
        return strategy;
    }

    public int AcceptedTypes()
    {
        return acceptedTypes;
    }

    public Bounds Bounds() { return bounds; }

    public Filter? FilterForTile(int tile, Polygon tileGeometry)
    {
        Filter? newLeft = left.FilterForTile(tile, tileGeometry);
        if (newLeft == FalseFilter.INSTANCE) return FalseFilter.INSTANCE;
        Filter? newRight = right.FilterForTile(tile, tileGeometry);
        if (newRight == FalseFilter.INSTANCE) return FalseFilter.INSTANCE;
        if (newLeft == null) return newRight;
        if (newRight == null) return newLeft;
        if (newLeft == left && newRight == right) return this;
        return Create(newLeft, newRight);
            // TODO: don't need to AND types and bbox, since these are only
            //  used at beginning of filtering (not applied on a per-tile basis)
    }

    public static Filter Create(Filter left, Filter right)
    {
        int leftStrategy = left.Strategy();
        int rightStrategy = right.Strategy();
        int leftStrictBounds = leftStrategy & FilterStrategy.STRICT_BBOX;
        int rightStrictBounds = rightStrategy & FilterStrategy.STRICT_BBOX;

        // combine all strategy flags, except strict-bbox
        // the combined filter is only strict-bbox if both are strict-bbox
        // TODO: what about needs-geometry?
        int combinedStrategy = ((leftStrategy | rightStrategy)
            & ~FilterStrategy.STRICT_BBOX) |
            (leftStrictBounds & rightStrictBounds);

        int acceptedTypes = left.AcceptedTypes() & right.AcceptedTypes();
        if (acceptedTypes == 0) return FalseFilter.INSTANCE;

        Bounds bounds;
        if ((combinedStrategy & FilterStrategy.USES_BBOX) != 0)
        {
            // TODO: check for null bbox or enforce filter.bounds() returning World bbox
            //  if bbox not in use

            Bounds leftBounds = left.Bounds();
            Bounds rightBounds = right.Bounds();

            if ((leftStrictBounds | rightStrictBounds) != 0)
            {
                bounds = Box.Intersection(leftBounds, rightBounds);
                if (Box.IsNullBounds(bounds)) return FalseFilter.INSTANCE;
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
        // Log.debug("Combining %s and %s", left, right);
        return new AndFilter(left, right, combinedStrategy, bounds, acceptedTypes);
    }
}
