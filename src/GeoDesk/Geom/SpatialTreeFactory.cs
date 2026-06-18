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
/// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeFactory</c>.</remarks>
internal interface ISpatialTreeFactory<B> where B : IBounds
{

    /// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeFactory.createLeaf(List, int, int)</c>.</remarks>
    B CreateLeaf(IList<IBounds> children, int start, int end);

    /// <remarks>Ported from Java <c>com.geodesk.geom.SpatialTreeFactory.createBranch(List, int, int)</c>.</remarks>
    B CreateBranch(IList<B> children, int start, int end);

}
