# The GOL File Format

This document describes the on-disk format of a **GOL** (Geographic Object Library) file as
understood by this codebase. It is organized as a stack of layers, because the format genuinely
*is* layered: a generic paged blob container at the bottom, a feature/tile model in the middle, and
content-addressed feature records at the top. Each layer only knows about the one below it.

> **Provenance.** This is a port of the Java [GeoDesk](https://github.com/clarisma/geodesk) library;
> the format is defined by `com.clarisma.common.store.*` (the container) and `com.geodesk.feature.*`
> (the feature model). Constant names below match the source (`BlobStoreConstants`, `FeatureStore`,
> `FeatureFlags`, `TagValues`).

## Conventions used throughout

- **Endianness.** Everything is **little-endian**. The one exception is the crash-recovery
  *journal*, which is big-endian — but the journal is not part of the live data structure, so it
  does not appear below.
- **Relative pointers.** Almost nothing in the file uses absolute offsets. A pointer is a 32-bit
  *delta* stored at some location `pp`; the target is `pp + delta`. We write `pp` for a
  pointer-to-a-pointer (the location holding the delta) and `p` for the resolved target. This makes
  whole blobs relocatable and position-independent.
- **Tagged pointers.** Because pointers are 4-byte-aligned, the low 1–3 bits are reused as flags
  (e.g. "this points to a child level", "this is the last entry"). Callers mask them off
  (`& ~3`) before following.
- **Anchors.** Several records are referenced by a pointer that lands in the *middle* of the
  record, not its start (see the feature record, Layer 5). Fixed-size fields that vary by type live
  *behind* the anchor; the shared fields live *ahead* of it.

---

## Layer 0 — Physical: the mapped file, segments, pages, blocks

A GOL is a single file accessed via memory mapping.

| Unit | Size | Role |
|------|------|------|
| **Segment** | 1 GB | One `mmap` window. The whole file is mapped as a series of 1-GB segments. A `Segment` in the code is exactly one such window. |
| **Page** | power-of-2 × 4 KB (default 4 KB) | The allocation/addressing unit. Blobs are identified by their **32-bit starting page number**. The page size is fixed per file. |
| **Block** | 4 KB | The smallest **journaled** unit (the granularity at which modifications are tracked for crash recovery). |

Page ↔ segment math (`pageSizeShift` = log2 of the page size, default 12):

```
segment(page) = page >> (30 - pageSizeShift)        // which 1-GB window
offsetInSeg(page) = (page << pageSizeShift) & 0x3fff_ffff
```

**Hard rule:** a contiguous run of bytes (a blob) must never straddle a 1-GB segment boundary.
The allocator pads to the next segment rather than cross one. This is what lets any blob be handed
to a reader as a single contiguous `Memory<byte>` slice of one segment.

---

## Layer 1 — Container: the BlobStore

A **BlobStore** turns the paged file into a heap of variable-size **blobs**. (Class hierarchy:
`Store` → `BlobStore` → `FreeStore` → `FeatureStore`. The lower two are generic; the GOL-specific
behavior is in the upper two.)

### The blob

A blob is a run of contiguous pages, ≤ 1 GB including its header. It begins with a single **4-byte
header word**:

```
bit 31      FREE_BLOB_FLAG            this blob is on the free list
bit 30      PRECEDING_BLOB_FREE_FLAG  the blob immediately before this one is free
bits 0..29  PAYLOAD_SIZE_MASK         payload size in bytes (excludes the 4-byte header)
```

→ cursor: [`BlobHeader`](src/GeoDesk/Common/Store/Format/BlobHeader.cs).

### The store header (offset 0)

The **only** structure in the entire file with a fixed, absolute layout. It sits at offset 0 of the
first block (and is part of the *metadata section*, see below).

| Offset | Field | Notes |
|-------:|-------|-------|
| 0 | `MAGIC` | `0x7ADA0BB1` for a raw BlobStore (overridden by `FeatureStore`, Layer 2) |
| 4 | `VERSION_OFS` | `1_000_000` = 1.0 |
| 8 | `TIMESTAMP_OFS` | creation/modification time (ms since epoch), 8 bytes |
| 16 | `TOTAL_PAGES_OFS` | size of the store in pages |
| 20 | `PAGE_SIZE_OFS` | page-size code (0 = 4K … 15 = 128 MB) |
| 24 | `METADATA_SIZE_OFS` | length of the metadata section in bytes |
| 28 | `TRUNK_FT_RANGE_BITS_OFS` | which 16-slot ranges of the trunk free-table are in use |
| 44 | `INDEX_PTR_OFS` | relative pointer to the user index (0 ⇒ store is empty) |
| 64 | `TRUNK_FREE_TABLE_OFS` | trunk free-table: 512 × 4-byte slots (`FREE_TABLE_LEN` = 2048) |
| 64+2048 | `GUID_OFS` | 16-byte store GUID (temporary location per source) |

→ cursor: [`StoreHeader`](src/GeoDesk/Common/Store/Format/StoreHeader.cs).

### The metadata section

A reserved region at the start of the file (`METADATA_SIZE` bytes) holding the header, the free
tables, and a user-defined **index**. The index maps a blob **id** → starting page:

```
indexEntry(id) = baseMapping.GetInt(indexPointer + id * 4)
```

For a `FeatureStore`, "id" is a **TIP** and the index is the tile index (Layer 3).

### Free space management

Freed blobs are tracked so they can be reused. This is the most intricate part of the container,
but readers never touch it — it only matters to the writer.

- Free blobs are bucketed by size (in pages) into a **two-level free table**: a *trunk* table in the
  root block (512 slots, one per 512-page range) → each slot points to a *leaf* free-table that is
  itself hosted inside one of the free blobs of that size range.
- Within a size class, free blobs form a **doubly-linked list** (`PREV_FREE_BLOB_OFS` = +4,
  `NEXT_FREE_BLOB_OFS` = +8). Range-occupancy bitmasks (`*_FT_RANGE_BITS`) let the allocator skip
  empty ranges quickly.
- The last block of a free blob ends with a **trailer** (`TRAILER_OFS` = block_len − 4) holding the
  blob's length in pages, so a freed blob can find and coalesce with the *preceding* blob.
- On free, adjacent free blobs are **coalesced** — but never across a 1-GB segment boundary.

→ cursors: [`FreeBlob`](src/GeoDesk/Common/Store/Format/FreeBlob.cs) (free-list links + leaf
free-table) and the trunk table via `StoreHeader.TrunkFreeTablePage`. The trunk/leaf *traversal*
itself (range-bit walking) is an algorithm in `BlobStore`, not a record cursor.

### Concurrency & durability

- **Journaling.** Before modifying a 4-KB block, the writer copies the original to a journal
  (big-endian), so an interrupted write can be rolled back / forward. This is the `Store` base
  class's job.
- **Locks.** Multiple processes may read concurrently; a writer may *append* blobs and alter
  metadata while readers run, but *deleting/modifying* existing blobs needs an exclusive lock.
- **Dual snapshots** (Layer 2) let a reader keep using a consistent view while a writer publishes a
  new one.

---

## Layer 2 — FeatureStore: snapshots, string table, schema, zoom

`FeatureStore` is a `BlobStore` whose blobs are **tiles** and whose index is the **tile index**.

```
MAGIC   = 0x1CE50D6E   ("geodesic")
VERSION = 1_000_000
```

It adds its own fields to the header block:

| Offset | Field | Notes |
|-------:|-------|-------|
| 84 | `STRING_TABLE_PTR_OFS` | → global string table (GST) |
| 88 | `INDEX_SCHEMA_PTR_OFS` | → key-to-category map (drives spatial-index bucketing) |
| 92 | `PROPERTIES_PTR_OFS` | → properties blob |
| 96 | `ZOOM_LEVELS_OFS` | bitmask of which zoom levels the tile pyramid uses |
| 128 + n·64 | **snapshot records** | 64 bytes each; `ActiveSnapshot()` selects the live one |

- **Snapshots.** Two 64-byte snapshot records (at `128 + n*64`) implement the dual-view scheme. The
  active snapshot holds `SNAPSHOT_TILE_INDEX_OFS` (+24, the tile-index page) and
  `SNAPSHOT_TILE_COUNT_OFS` (+28). A writer fills the inactive snapshot and flips the active flag.
- **Global String Table (GST).** A PBF-encoded list of strings (`count` then varint-prefixed
  strings). Common tag keys/values are stored as small integer **codes** into this table rather than
  as inline strings.
- **Index schema.** Maps tag-key codes → *category* bits. A feature's set of indexed-key categories
  becomes the key under which it is filed in a tile's spatial index (Layer 4).
- **Zoom levels.** A GOL is a tile **pyramid**; `ZoomLevels` says which zooms exist, and the tile
  index tree has one level per zoom step.

---

## Layer 3 — The tile index

The container's "index" (Layer 1) is, for a GOL, a flat array of **4-byte tile-index entries**,
indexed by **TIP** (Tile Index Pointer — really just a tile number):

```
entry(tip) = tileIndexBuf.GetInt(tileIndexOfs + tip * 4)
```

Each entry packs a page and 2 low flag bits:

```
bits 0..1   00/10/11 → entry names a tile; 01 → pointer to a child level
bit 1       set ⇒ tile is loaded and current (IsTileLoadedAndCurrent)
bits 2..31  tile page  (entry >>> 2)        when not a child pointer
```

The same array is *also* a **tree**: an entry with low bits `01` is a relative pointer to a child
level. A node with children is followed by a **64-bit child-tile mask** (which cells of the child
matrix actually contain tiles — empty cells are skipped) and then the packed child entries. The
walker descends parent → child, deriving each tile's TIP as `(pEntry − tileIndexOfs) / 4` and its
geographic position from the matrix coordinates.

→ traversal: `TileIndexWalker`; entry cursor:
[`TileIndexEntry`](src/GeoDesk/Feature/Store/Format/TileIndexEntry.cs).

---

## Layer 4 — Inside a tile: the four spatial indexes

Each tile is one blob. After its blob header, the tile header holds pointers to **four independent
spatial indexes**, one per feature category, plus the exports table:

| Tile offset | Contents |
|------------:|----------|
| 0 | blob header word (Layer 1) |
| +8 | node index (relative pointer) |
| +12 | way index (non-area ways) |
| +16 | area index (ways *and* relations that are areas) |
| +20 | relation index (non-area relations) |
| +24 | exports table (relative pointer) — see Layer 7 |

→ cursor: [`Tile`](src/GeoDesk/Feature/Store/Format/Tile.cs).

This 4-way split — **nodes / non-area ways / areas / non-area relations** — is the physical
partition behind GOQL's `n`/`w`/`a`/`r` selectors. (Note: a feature's 2-bit *type code* is only
node/way/relation; "area" is a flag, so areas are physically pulled into their own index regardless
of whether they are ways or relations.)

### A spatial index = buckets keyed by tag category

An index is a list of **8-byte buckets**. Each bucket holds an R-tree root and a set of accepted-tag
*key bits*; a query ANDs its required categories against the bucket's key bits to decide whether to
descend. A bucket whose first word is 0 means the index is empty.

```
bucket word 0:  bits 2..31 = relative pointer to R-tree root,  bit 0 = last bucket
bucket word 1:  key bits (tag categories filed under this bucket)
```

→ cursors: [`SpatialIndex`](src/GeoDesk/Feature/Store/Format/SpatialIndex.cs),
[`IndexBucket`](src/GeoDesk/Feature/Store/Format/IndexBucket.cs).

### The R-tree

Each bucket's features are organized in an R-tree of **trunk** and **leaf** nodes.

**Trunk entry — 20 bytes:**

```
word 0:  bits 2..31 = relative pointer to child (trunk or leaf)
         bit 0 = last entry,  bit 1 = child is a leaf
+4 .. +19:  child bounding box (minX, minY, maxX, maxY — four LE ints)
```

**Leaf entry (way / area / relation) — 32 bytes:** this is where features are **embedded** (see
Layer 5). The first 16 bytes are the feature's bounding box; the next 16 are the feature's forward
header.

**Leaf entry (node):** variable length. Layout is `x, y` (8 bytes) then the node's forward header;
stride is `20 + (flags & 4)` (the +4 is an optional relation-table pointer). Nodes carry a point,
not a box.

→ cursors: [`TrunkEntry`](src/GeoDesk/Feature/Store/Format/TrunkEntry.cs),
[`LeafEntry`](src/GeoDesk/Feature/Store/Format/LeafEntry.cs),
[`NodeEntry`](src/GeoDesk/Feature/Store/Format/NodeEntry.cs),
[`Bounds`](src/GeoDesk/Feature/Store/Format/TileLayout.cs).

---

## Layer 5 — The feature record

A feature is referenced by a pointer to its **anchor** — the flags/id word. The record straddles
the anchor:

```
        ┌─ geometry header (behind the anchor, size varies by type) ─┐
        │  way/area/relation: 16-byte bbox   |   node: 8-byte x/y     │
 ptr-16/ptr-8 ───────────────────────────────────────────────────────┘
 ptr ──►┌─ shared forward header (same for every type) ──────────────┐
        │  +0   flags (low 12 bits) + id (high 52 bits)   [8 bytes]   │
        │  +8   relative pointer to the tag table                     │
        │  +12  relative pointer to the body                          │
        └────────────────────────────────────────────────────────────┘
```

The anchor points into the middle on purpose: the geometry header behind it is *type-discriminated
and variable size* (you cannot know how far back it starts until you have read the type at the
anchor), while the shared fields ahead of it are at fixed positive offsets for every type. **This is
also why the R-tree leaf entry and the feature record overlap:** the leaf's 16-byte bbox *is* the
feature's geometry header, and the feature anchor is `leafEntry + 16`.

→ forward cursor: [`FeatureHeader`](src/GeoDesk/Feature/Store/Format/FeatureHeader.cs). It only ever
reads forward; positioning a `Bounds` (or node x/y) *behind* the anchor is the owning code's job,
because only the owner holds the full segment memory and knows the type.

### The flags word (low bits of `+0`)

```
bit 0   LAST_SPATIAL_ITEM_FLAG   last entry in its spatial-index node
bit 1   AREA_FLAG                feature is an area
bit 2   RELATION_MEMBER_FLAG     feature belongs to ≥1 relation
bits 3-4 FEATURE_TYPE_BITS       0 = node, 1 = way, 2 = relation
bit 5   WAYNODE_FLAG             way has feature-nodes / node is a way-node
bit 6   MULTITILE_WEST           feature continues into the tile to the west
bit 7   MULTITILE_NORTH          feature continues into the tile to the north
bit 8   SHARED_LOCATION_FLAG
bit 9   EXCEPTION_NODE_FLAG
bit 10  UNMODIFIED_FLAG
bit 11  DELETED_FLAG
bits 12-63  id
```

### Tags (the `+8` table)

The tag-table pointer's **low bit** flags the presence of *uncommon* keys; it is stripped to get the
table pointer. The table has two sections growing in opposite directions from the pointer:

- **Common keys** — builtin keys (code ≤ `MAX_COMMON_KEY` = 8190), stored *forward*, sorted by key,
  4 (narrow) or 6 (wide) bytes each.
- **Uncommon keys** — arbitrary string keys, stored *backward* from the pointer, 6 or 8 bytes each,
  with a "last" flag bit.

A value is encoded in 2 type/size bits: **narrow vs wide** × **number vs string**. Narrow values sit
inline in the high 16 bits; wide values are a relative pointer to the payload. Global strings are a
code into the GST; local strings are a relative pointer. An empty table is the single marker word
`EMPTY_TABLE_MARKER` = `0x8001`. Numbers are biased by `MIN_NUMBER` = −256.

(There is no dedicated cursor for the tag table yet — the walk/decode lives in `StoredFeature`.)

### Body (the `+12` pointer)

Type-specific, and often with structures hanging *just before* the body pointer:

- **Way** → a packed list of node coordinates (delta-encoded varints; `XYIterator`), optionally
  interleaved with references to *feature* way-nodes.
- **Relation** → a **member table**: a list of (typed, possibly foreign) member references with
  roles. → handle: [`MemberTable`](src/GeoDesk/Feature/Store/Format/MemberTable.cs).
- **Relation membership** → a **relation (parent) table**, found at `body − 4`, listing the
  relations a feature belongs to (when `RELATION_MEMBER_FLAG` is set).

---

## Layer 6 — Cross-tile references (TIP / TEX)

Because the world is split into tiles, a feature in one tile often references a feature in another
(a way-node, a relation member, a parent relation). These **foreign** references are resolved in two
hops:

1. **TIP** — which tile (an index into the tile index, Layer 3). Encoded as a *delta* from a running
   TIP, with a narrow/wide form.
2. **TEX** — Tile-local EXport index. Each tile has an **exports table** (pointer at tile `+24`)
   mapping a TEX → a relative pointer to the exported feature within that tile:

   ```
   pExported  = exportsBase + tex * 4
   featurePtr = pExported + foreignBuf.GetInt(pExported)
   ```

Foreign references carry flag bits selecting local vs foreign, narrow vs wide TEX, and
same-tile vs different-tile (which triggers re-reading the TIP delta). The encoded entry advances a
2- or 4-byte cursor accordingly. This logic lives in the way-node and member iterators.

---

## Appendix — format structures ↔ cursor types

| Format structure | Layer | Cursor (`*/Format/`) |
|------------------|:-----:|----------------------|
| Store header (offset 0) | 1 | `StoreHeader` |
| Blob header word | 1 | `BlobHeader` |
| Free blob (links + leaf free-table) | 1 | `FreeBlob` |
| Tile-index entry | 3 | `TileIndexEntry` |
| Tile header (4 index roots) | 4 | `Tile` |
| Spatial index (bucket list) | 4 | `SpatialIndex` |
| Index bucket | 4 | `IndexBucket` |
| R-tree trunk entry | 4 | `TrunkEntry` |
| R-tree leaf entry (W/A/R) | 4/5 | `LeafEntry` |
| R-tree leaf entry (node) | 4/5 | `NodeEntry` |
| Bounding box | 4/5 | `Bounds` |
| Feature forward header | 5 | `FeatureHeader` |
| Relation member table | 5 | `MemberTable` |

**Not yet cursored (stateful decoders / algorithms):** the tag-table walk + value decode, the
way-node iterator, the relation-member iterator, and the free-table trunk/leaf traversal. These are
better expressed as iterators alongside their call sites than as fixed-layout record cursors.

All record cursors follow one convention: they hold a `ReadOnlyMemory<byte>` already **sliced to the
record's start**, read fields at fixed *relative* offsets via the little-endian accessors in
`BufferExtensions`, and navigate by slicing further (never by carrying a separate offset).
