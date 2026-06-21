/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature;

/// <summary>
/// An <see cref="IFeature"/> that represents an OSM relation — an ordered collection of member
/// features (each with an optional role), used to model logical groupings or multi-polygon areas.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.Relation</c>.</remarks>
public interface IRelation : IFeature
{

}
