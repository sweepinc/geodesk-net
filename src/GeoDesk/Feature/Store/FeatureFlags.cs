/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Store;

/// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureFlags</c>.</remarks>
internal interface IFeatureFlags
{

    const int LAST_SPATIAL_ITEM_FLAG = 1;
    const int AREA_FLAG = 1 << 1;
    const int RELATION_MEMBER_FLAG = 1 << 2;
    const int FEATURE_TYPE_BITS = 3; // Bit 3 & 4
    const int WAYNODE_FLAG = 1 << 5;
    const int MULTITILE_BITS = 6;
    const int MULTITILE_WEST_BIT = 6;
    const int MULTITILE_NORTH_BIT = 7;
    const int MULTITILE_WEST = 1 << MULTITILE_WEST_BIT;
    const int MULTITILE_NORTH = 1 << MULTITILE_NORTH_BIT;
    const int MULTITILE_FLAGS = MULTITILE_WEST | MULTITILE_NORTH;
    const int SHARED_LOCATION_FLAG = 1 << 8;
    const int EXCEPTION_NODE_FLAG = 1 << 9;
    const int UNMODIFIED_FLAG = 1 << 10;
    const int DELETED_FLAG = 1 << 11;

}
