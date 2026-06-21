/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature;

/// <summary>
/// An <see cref="IFeature"/> that represents an OSM way — an ordered sequence of nodes forming a
/// linestring, a closed linear ring, or a simple polygon (depending on whether it is closed and
/// tagged as an area).
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Way</c>.</remarks>
public interface IWay : IFeature
{

}
