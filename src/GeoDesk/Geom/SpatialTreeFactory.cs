/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;

namespace GeoDesk.Geom;

// NOTE: Java declares createLeaf's parameter as List<? extends Bounds>. C# lacks
// use-site covariance for mutable lists, so this straight port uses IList<Bounds>.
/// <summary>
/// Factory abstraction used by spatial-tree builders to create the leaf and branch
/// nodes of the tree, decoupling the packing algorithm from the concrete node type.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeFactory</c>.</remarks>
internal interface ISpatialTreeFactory<B> where B : IBounds
{

    /// <summary>
    /// Creates a leaf node covering the items in the given list over the half-open
    /// range [<paramref name="start"/>, <paramref name="end"/>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeFactory.createLeaf(List, int, int)</c>.</remarks>
    B CreateLeaf(IList<IBounds> children, int start, int end);

    /// <summary>
    /// Creates a branch node grouping the child nodes in the given list over the
    /// half-open range [<paramref name="start"/>, <paramref name="end"/>).
    /// </summary>
    /// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeFactory.createBranch(List, int, int)</c>.</remarks>
    B CreateBranch(IList<B> children, int start, int end);

}
