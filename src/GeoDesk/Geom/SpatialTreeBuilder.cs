/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

namespace GeoDesk.Geom;

// NOTE: Java declares the parameter as ArrayList<? extends Bounds>. C# lacks
// use-site covariance for mutable lists, so this straight port uses IList<Bounds>.
/// <summary>
/// Abstraction for an algorithm that bulk-loads a spatial tree from a collection of
/// bounding boxes and returns its root node.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeBuilder</c>.</remarks>
internal interface ISpatialTreeBuilder<B> where B : IBounds
{

    /// <summary>
    /// Builds the spatial tree over the given items and returns its root node.
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeBuilder.build(ArrayList)</c>.</remarks>
    B Build(IList<IBounds> items);

}
