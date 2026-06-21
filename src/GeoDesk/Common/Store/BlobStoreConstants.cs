/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace GeoDesk.Common.Store;

/// <summary>
/// Fixed byte offsets and flag constants for the BlobStore on-disk format: the header
/// fields, the free-table layout, and the per-blob marker bits.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreConstants</c>.</remarks>
internal static class BlobStoreConstants
{

    // === GENERAL ===

    /// <summary>
    /// Block length (4K). A Block is the smallest unit of data that is journaled.
    /// The Page Size is always a power-of-2 multiple of the Block Size.
    /// </summary>
    public const int BLOCK_LEN = 4096;

    /// <summary>
    /// Size of each free table (in bytes). Each free table has 512 slots (4 bytes each).
    /// </summary>
    public const int FREE_TABLE_LEN = 2048;

    // === HEADER ===

    public static int MAGIC = 0x7ADA0BB1; // B10BDA7A = "BlobData"
    public static int VERSION = 1_000_000;

    /// <summary>Offset where the file format version is stored.</summary>
    public const int VERSION_OFS = 4;
    public const int TIMESTAMP_OFS = 8;
    public const int INDEX_PTR_OFS = 44; // TODO: not part of blobStore

    /// <summary>Offset where the total size of the Blob Store (in pages) is stored.</summary>
    public const int TOTAL_PAGES_OFS = 16;

    /// <summary>
    /// Offset where the page size is stored. Valid page size values are from 0 (4K) to 15 (128 MB).
    /// </summary>
    public const int PAGE_SIZE_OFS = 20;

    /// <summary>
    /// Offset where the length of the metadata section is stored. The length is in bytes
    /// and includes all header fields.
    /// </summary>
    public const int METADATA_SIZE_OFS = 24;

    /// <summary>
    /// Offset of the bitmask that stores which ranges of the trunk free-table are in use.
    /// Each range covers 16 slots.
    /// </summary>
    public const int TRUNK_FT_RANGE_BITS_OFS = 28;

    /// <summary>
    /// Offset of the trunk free-table. (This offset must be evenly divisible by 64)
    /// </summary>
    public const int TRUNK_FREE_TABLE_OFS = 64; // must be divisible by 64

    /// <summary>
    /// TODO: temporary location of GUID until new file spec is finalized.
    /// For now, we place it right after the trunk freetable.
    /// </summary>
    public const int GUID_OFS = TRUNK_FREE_TABLE_OFS + FREE_TABLE_LEN;
    public const int GUID_LEN = 16;

    public const int DEFAULT_METADATA_SIZE = TRUNK_FREE_TABLE_OFS + FREE_TABLE_LEN + GUID_LEN;

    // === BLOB ===

    /// <summary>
    /// Flag to indicate that a Blob is free. Stored in the header word of a Blob.
    /// </summary>
    public const int FREE_BLOB_FLAG = 1 << 31;

    /// <summary>
    /// Flag to indicate that the Blob immediately preceding this Blob is free.
    /// Stored in the header word.
    /// </summary>
    public const int PRECEDING_BLOB_FREE_FLAG = 1 << 30;

    /// <summary>
    /// A bit mask that, when applied to a Blob's header word, yields the size of the
    /// Blob's payload (max. 1 GB - 4).
    /// </summary>
    public const int PAYLOAD_SIZE_MASK = 0x3fff_ffff;

    // === FREE BLOB ===

    /// <summary>Offset where the page number of the previous free blob is stored.</summary>
    public const int PREV_FREE_BLOB_OFS = 4;

    /// <summary>Offset where the page number of the next free blob is stored.</summary>
    public const int NEXT_FREE_BLOB_OFS = 8;

    /// <summary>
    /// Offset of the bitmask that stores which ranges of the leaf free-table are in use.
    /// Each range covers 16 slots.
    /// </summary>
    public const int LEAF_FT_RANGE_BITS_OFS = 12;

    /// <summary>
    /// Offset of the leaf free-table. (This offset must be evenly divisible by 64)
    /// </summary>
    public const int LEAF_FREE_TABLE_OFS = 64; // must be divisible by 64

    /// <summary>
    /// Offset where the free-blob trailer is stored (relative to the start of the free
    /// blob's last block). The trailer is a single word that contains the length of the
    /// free blob (in pages).
    /// </summary>
    public const int TRAILER_OFS = BLOCK_LEN - 4;

    public const int FREE_BLOB_TRAILER_LEN = 4;

    // === EXPORTED BLOB ===

    public static int EXPORTED_MAGIC = 0x0BB11DC0; // C01DB10B = "Cold blob"

    public static int EXPORTED_HEADER_LEN = 32; // TODO: review
    public static int EXPORTED_HEADER_GUID = 8; // 16-byte GUID of origin BlobStore
    public static int EXPORTED_BLOB_ID = 24; // TODO
    public static int EXPORTED_ORIGINAL_LEN_OFS = 28; // TODO

}
