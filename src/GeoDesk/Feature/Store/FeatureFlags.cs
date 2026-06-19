/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Store;

/// <summary>The bit layout of a feature's flags word (the low bits of its anchor; the id occupies bits 12+).</summary>
/// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureFlags</c> (a constants interface there;
/// a static class here, since nothing implements it).</remarks>
internal static class FeatureFlags
{

    public const int LAST_SPATIAL_ITEM_FLAG = 1;
    public const int AREA_FLAG = 1 << 1;
    public const int RELATION_MEMBER_FLAG = 1 << 2;
    public const int FEATURE_TYPE_BITS = 3; // Bit 3 & 4
    public const int WAYNODE_FLAG = 1 << 5;
    public const int MULTITILE_BITS = 6;
    public const int MULTITILE_WEST_BIT = 6;
    public const int MULTITILE_NORTH_BIT = 7;
    public const int MULTITILE_WEST = 1 << MULTITILE_WEST_BIT;
    public const int MULTITILE_NORTH = 1 << MULTITILE_NORTH_BIT;
    public const int MULTITILE_FLAGS = MULTITILE_WEST | MULTITILE_NORTH;
    public const int SHARED_LOCATION_FLAG = 1 << 8;
    public const int EXCEPTION_NODE_FLAG = 1 << 9;
    public const int UNMODIFIED_FLAG = 1 << 10;
    public const int DELETED_FLAG = 1 << 11;

}
