/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Feature.Store;

// In Java this extends BlobStoreConstants. Here it is a standalone static class with its
// own constants; the inherited BlobStore constants are referenced via BlobStoreConstants
// directly at the (few) call sites. Note MAGIC/VERSION/VERSION_OFS deliberately differ
// from the BlobStore values.
/// <remarks>Ported from Java <c>com.geodesk.feature.store.FeatureStoreConstants</c>.</remarks>
internal static class FeatureStoreConstants
{

    public const int MAGIC = 0x1CE50D6E; // 6E0DE51C "geodesic/geodesk"
    public const int VERSION = 1_000_000;

    public const int MAGIC_CODE_OFS = 32;
    public const int VERSION_OFS = 36;
    public const int ZOOM_LEVELS_OFS = 40;
    public const int TILE_INDEX_PTR_OFS = 44;
    public const int PROPERTIES_PTR_OFS = 48;
    public const int STRING_TABLE_PTR_OFS = 52;
    public const int INDEX_SCHEMA_PTR_OFS = 56;

}
