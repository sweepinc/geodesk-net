/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature;

/// <summary>
/// An <see cref="IFeature"/> that represents a single OSM node — a point at one coordinate. A node
/// may be a standalone feature or a vertex shared by one or more ways.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Node</c>.</remarks>
public interface INode : IFeature
{

}
