/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature;

/// <summary>
/// Identifies the kind of OpenStreetMap feature: a <c>Node</c> (a single point), a <c>Way</c>
/// (an ordered sequence of nodes), or a <c>Relation</c> (a collection of member features). The
/// ordinal values match the 2-bit type codes that GeoDesk encodes alongside feature IDs.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.FeatureType</c>.</remarks>
public enum FeatureType
{

    Node,
    Way,
    Relation

}
