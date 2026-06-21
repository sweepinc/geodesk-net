/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Store;

/// <summary>
/// Constants shared by the feature-table iterators: the starting TIP and the
/// starting TEX values for relation members, parent relations, and way feature-nodes.
/// </summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureConstants</c>.</remarks>
internal static class FeatureConstants
{

    /// <summary>
    /// The initial TIP used in iterators for relation members, relation tables, and
    /// feature nodes of ways. We start at this value (rather than 0) so a range of TIPs
    /// from 0 to 32,767 can be addressed with a narrow (15-bit) TIP delta.
    /// </summary>
    public const int START_TIP = 0x4000; // 16,384
    public const int MEMBERS_START_TEX = 0x400;
    public const int RELATIONS_START_TEX = 0x800;
    public const int WAYNODES_START_TEX = 0x800;

}
