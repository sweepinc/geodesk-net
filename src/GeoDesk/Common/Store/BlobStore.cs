/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Numerics;

using GeoDesk.Buffers;
using GeoDesk.Common.Store.Format;

using static GeoDesk.Common.Store.BlobStoreConstants;

namespace GeoDesk.Common.Store;

/// <summary>
/// A Blob Store is a file containing a large numbers of individual binary
/// objects (blobs) that span multiple contiguous file pages. Page size is
/// configurable and must be a power-of-2 multiple of 4 KB.
///
/// Blobs are identified by their starting page (a 32-bit integer).
/// The maximum size of a Blob Store is dependent on its page size;
/// at the 4 KB default, the file can grow to 16 TB.
///
/// The maximum size of each blob (including its 4-byte header) is 1 GB
/// (regardless of page size). Assuming 4 KB pages, a blob can contain up
/// to 256K pages.
///
/// There is no restriction on the structure and type of content of the blobs,
/// except for the following: The first 4 bytes contain the size of the blob
/// and two marker bits. The user must not modify this header. Apart from the
/// blobs, the Blob Store file contains metadata that maintains allocation
/// statistics and free lists. An additional user-defined metadata section
/// can be used to store an index.
///
/// Blob Stores allow concurrent access by multiple processes, with some
/// restrictions. Write access is mediated through the use of locks.
/// A process may add blobs and alter metadata while other processes are
/// reading, but deletion or modification of blobs requires an exclusive lock.
///
/// Blob Stores use the journaling mechanism of the Store baseclass to track
/// modifications, in order to prevent data corruption due to abnormal process
/// termination (such as power loss). Using the Journal, a Blob Store can
/// restore itself to a consistent state by either applying the failed
/// modifications, or rolling them back.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore</c>.</remarks>
internal class BlobStore : Store
{

    /// <summary>
    /// The number of bits to shift left to turn number of pages into number of bytes
    /// (Page size is always a power of two).
    /// </summary>
    protected internal int pageSizeShift = 12; // 4KB default page
    protected Downloader? downloader;

    static int Ushr(int v, int n) => (int)((uint)v >> n);

    /// <summary>
    /// Downloads the blob identified by the given URL into the store and returns its
    /// first page. Not yet implemented.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.downloadBlob(URL)</c>.</remarks>
    protected int DownloadBlob(Uri url)
    {
        return 0; // TODO
    }

    /// <summary>
    /// Configures the remote repository this store fetches missing blobs from by
    /// creating its downloader.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.setRepository(String)</c>.</remarks>
    public void SetRepository(string url)
    {
        Debug.Assert(downloader == null);
        downloader = new Downloader(this, url);
    }

    /// <summary>
    /// Initializes a freshly created store file by writing the magic number, version,
    /// timestamp, metadata size, and initial page count into the header.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.createStore()</c>.</remarks>
    protected override void CreateStore()
    {
        // TODO: should this be inside a transaction?
        var buf = new NioBufferWriter(BaseSegment.Memory);
        buf.PutInt(0, MAGIC);
        buf.PutInt(VERSION_OFS, VERSION);
        buf.PutLong(TIMESTAMP_OFS, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        buf.PutInt(METADATA_SIZE_OFS, DEFAULT_METADATA_SIZE);
        buf.PutInt(TOTAL_PAGES_OFS, 1);
        // TODO: page size
    }

    /// <summary>A cursor over the store header at offset 0 of the base segment.</summary>
    StoreHeader Header => new StoreHeader(BaseSegment.Memory);

    /// <summary>
    /// Returns the store's creation/modification timestamp read from the header.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.getTimestamp()</c>.</remarks>
    protected override long GetTimestamp()
    {
        return Header.Timestamp;
    }

    /// <summary>
    /// Returns the store's globally unique identifier read from the header.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.getGuid()</c>.</remarks>
    public Guid GetGuid()
    {
        return Header.Guid;
    }

    /// <summary>
    /// Validates the store header's magic number and version, throwing a
    /// <see cref="StoreException"/> if the file is not a compatible BlobStore.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.verifyHeader()</c>.</remarks>
    protected override void VerifyHeader()
    {
        var header = Header;
        if (header.Magic != MAGIC)
            throw new StoreException("Not a BlobStore file", Path);
        if (header.Version != VERSION)
            throw new StoreException("Wrong BlobStore version (Requires 1.0)", Path);

        // TODO: page size
    }

    /// <summary>
    /// Checks whether this BlobStore is *empty*. An empty store is valid, but has no contents.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.isEmpty()</c>.</remarks>
    protected internal bool IsEmpty()
    {
        return Header.IsEmpty;
    }

    /// <summary>
    /// Initializes the store after opening; if the store is empty and a downloader is
    /// configured, fetches the metadata blob before use.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.initialize()</c>.</remarks>
    protected override void Initialize()
    {
        base.Initialize();
        if (IsEmpty() && downloader != null)
        {
            var ticket = downloader.Request(Downloader.METADATA_ID, null);
            ticket.AwaitCompletion();
            ticket.ThrowError();
        }
    }

    /// <summary>
    /// Returns the logical size of the store in bytes, computed from the total page
    /// count in the header and the page size.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.getTrueSize()</c>.</remarks>
    protected override long GetTrueSize()
    {
        return ((long)Header.TotalPages) << pageSizeShift;
    }

    /// <summary>The mapped segment containing the given page. The caller wraps its <c>Memory</c> as it needs.</summary>
    protected internal Segment SegmentOfPage(int page)
    {
        return GetSegment(page >> (30 - pageSizeShift));
    }

    /// <summary>
    /// Returns the byte offset of the given page within its containing 1&#160;GB
    /// segment.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.offsetOfPage(int)</c>.</remarks>
    public int OffsetOfPage(int page)
    {
        return (page << pageSizeShift) & 0x3fff_ffff;
    }

    /// <summary>
    /// Returns the absolute byte offset of the given page from the start of the store
    /// file.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.absoluteOffsetOfPage(int)</c>.</remarks>
    public long AbsoluteOffsetOfPage(int page)
    {
        return ((long)page) << pageSizeShift;
    }

    /// <summary>
    /// Returns a writable block view positioned at the start of the given page.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.getBlockOfPage(int)</c>.</remarks>
    protected internal NioBufferWriter GetBlockOfPage(int page)
    {
        Debug.Assert(page >= 0); // TODO: treat page as unsigned int?
        return GetBlock(((long)page) << pageSizeShift);
    }

    /// <summary>
    /// Returns the store's page size in bytes.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.pageSize()</c>.</remarks>
    public int PageSize()
    {
        return 1 << pageSizeShift;
    }

    /// <summary>
    /// Returns the absolute offset of the blob index, resolved from the relative
    /// pointer stored in the header.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.indexPointer()</c>.</remarks>
    public int IndexPointer()
    {
        return BaseSegment.Memory.Span.GetIntLE(INDEX_PTR_OFS) + INDEX_PTR_OFS;
    }

    // TODO: should also make sure page does not lie in meta space
    /// <summary>
    /// Validates that the given page number lies within the store's allocated pages,
    /// throwing a <see cref="StoreException"/> otherwise.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.checkPage(int)</c>.</remarks>
    protected void CheckPage(int page)
    {
        if (page < 0 || page >= BaseSegment.Memory.Span.GetIntLE(TOTAL_PAGES_OFS))
            throw new StoreException("Invalid page: " + page, Path);
    }

    /// <summary>
    /// Determines the number of pages needed to store a blob.
    /// </summary>
    /// <param name="size">the size (excluding 4-byte header) of the blob</param>
    /// <returns>the number of pages needed to store the blob</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.pagesForPayloadSize(int)</c>.</remarks>
    protected internal int PagesForPayloadSize(int size)
    {
        Debug.Assert(size > 0 && size <= ((1 << 30) - 4));
        return (size + (1 << pageSizeShift) + 3) >> pageSizeShift;
    }

    /// <summary>
    /// Determines the number of pages needed to store the given number
    /// of bytes. (The range of bytes is assumed to start at the beginning
    /// of the first page.)
    /// </summary>
    /// <param name="len">the number of bytes</param>
    /// <returns>the number of pages needed to store the bytes</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.bytesToPages(int)</c>.</remarks>
    protected internal int BytesToPages(int len)
    {
        Debug.Assert(len > 0 && len <= (1 << 30));
        return (len + (1 << pageSizeShift) - 1) >> pageSizeShift;
    }

    /// <summary>
    /// Allocates a blob of a given size. If possible, the smallest existing free blob that can
    /// accommodate the requested number of bytes will be reused; otherwise, a new blob will be
    /// appended to the store file.
    ///
    /// TODO: guard against exceeding maximum file size
    /// </summary>
    /// <param name="size">the size of the blob, not including its 4-byte header</param>
    /// <returns>the first page of the blob</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.allocateBlob(int)</c>.</remarks>
    protected internal int AllocateBlob(int size)
    {
        Debug.Assert(size >= 0 && size <= ((1 << 30) - 4));
        var precedingBlobFreeFlag = 0;
        var requiredPages = PagesForPayloadSize(size);
        var rootBlock = GetBlock(0);
        var trunkRanges = rootBlock.GetInt(TRUNK_FT_RANGE_BITS_OFS);
        if (trunkRanges != 0)
        {
            var trunkSlot = (requiredPages - 1) / 512;
            var leafSlot = (requiredPages - 1) % 512;
            var trunkOfs = TRUNK_FREE_TABLE_OFS + trunkSlot * 4;
            var trunkEnd = (trunkOfs & unchecked((int)0xffff_ffc0)) + 64;

            trunkRanges = Ushr(trunkRanges, trunkSlot / 16);

            for (; ; )
            {
                if ((trunkRanges & 1) == 0)
                {
                    if (trunkRanges == 0)
                        break;

                    var rangesToSkip = BitOperations.TrailingZeroCount((uint)trunkRanges);
                    trunkEnd += rangesToSkip * 64;
                    trunkOfs = trunkEnd - 64;

                    leafSlot = 0;
                }
                Debug.Assert(trunkOfs < TRUNK_FREE_TABLE_OFS + FREE_TABLE_LEN);

                for (; trunkOfs < trunkEnd; trunkOfs += 4)
                {
                    var leafTableBlob = rootBlock.GetInt(trunkOfs);
                    if (leafTableBlob == 0)
                        continue;

                    var leafBlock = GetBlockOfPage(leafTableBlob);
                    var leafRanges = leafBlock.GetInt(LEAF_FT_RANGE_BITS_OFS);
                    var leafOfs = LEAF_FREE_TABLE_OFS + leafSlot * 4;
                    var leafEnd = (leafOfs & unchecked((int)0xffff_ffc0)) + 64;

                    Debug.Assert((leafBlock.GetInt(0) & FREE_BLOB_FLAG) != 0, string.Format(CultureInfo.InvariantCulture, "Leaf FB blob {0} must be a free blob", leafTableBlob));

                    leafRanges = Ushr(leafRanges, leafSlot / 16);

                    for (; ; )
                    {
                        if ((leafRanges & 1) == 0)
                        {
                            if (leafRanges == 0)
                                break;

                            var rangesToSkip = BitOperations.TrailingZeroCount((uint)leafRanges);
                            leafEnd += rangesToSkip * 64;
                            leafOfs = leafEnd - 64;
                        }
                        for (; leafOfs < leafEnd; leafOfs += 4)
                        {
                            var freeBlob = leafBlock.GetInt(leafOfs);
                            if (freeBlob == 0)
                                continue;

                            var freePages = ((trunkOfs - TRUNK_FREE_TABLE_OFS) << 7) +
                                ((leafOfs - LEAF_FREE_TABLE_OFS) >> 2) + 1;
                            if (freeBlob == leafTableBlob)
                            {
                                var nextFreeBlob = leafBlock.GetInt(NEXT_FREE_BLOB_OFS);
                                if (nextFreeBlob != 0)
                                    freeBlob = nextFreeBlob;
                            }

                            var freeBlock = GetBlockOfPage(freeBlob);
                            var header = freeBlock.GetInt(0);
                            Debug.Assert((header & FREE_BLOB_FLAG) != 0, string.Format(CultureInfo.InvariantCulture, "Blob {0} is not free", freeBlob));
                            var freeBlobPayloadSize = header & PAYLOAD_SIZE_MASK;
                            Debug.Assert((freeBlobPayloadSize + 4) >> pageSizeShift == freePages);
                            Debug.Assert(freePages >= requiredPages);

                            precedingBlobFreeFlag = header & PRECEDING_BLOB_FREE_FLAG;
                            RemoveFreeBlob(freeBlock);

                            if (freeBlob == leafTableBlob)
                            {
                                var newLeafBlob = RelocateFreeTable(freeBlob, freePages);
                                if (newLeafBlob != 0)
                                {
                                    Debug.Assert(rootBlock.GetInt(trunkOfs) == newLeafBlob);
                                }
                                else
                                {
                                    Debug.Assert(rootBlock.GetInt(trunkOfs) == 0);
                                }
                            }

                            if (freePages > requiredPages)
                            {
                                AddFreeBlob(freeBlob + requiredPages, freePages - requiredPages, 0);
                            }
                            else
                            {
                                var nextBlock = GetBlockOfPage(freeBlob + freePages);
                                var nextSizeAndFlags = nextBlock.GetInt(0);
                                nextBlock.PutInt(0, nextSizeAndFlags & ~PRECEDING_BLOB_FREE_FLAG);
                            }

                            freeBlock.PutInt(0, size | precedingBlobFreeFlag);
                            return freeBlob;
                        }
                        leafRanges = Ushr(leafRanges, 1);
                        leafEnd += 64;
                    }
                    leafSlot = 0;
                }

                trunkRanges = Ushr(trunkRanges, 1);
                trunkEnd += 64;
            }
        }

        // If we weren't able to find a suitable free blob, we'll grow the store

        var totalPages = rootBlock.GetInt(TOTAL_PAGES_OFS);
        var pagesPerSegment = (1 << 30) >> pageSizeShift;
        var remainingPages = pagesPerSegment - (totalPages & (pagesPerSegment - 1));
        if (remainingPages < requiredPages)
        {
            AddFreeBlob(totalPages, remainingPages, 0);
            totalPages += remainingPages;
            precedingBlobFreeFlag = PRECEDING_BLOB_FREE_FLAG;
        }
        rootBlock.PutInt(TOTAL_PAGES_OFS, totalPages + requiredPages);

        var newBlock = GetBlockOfPage(totalPages);
        newBlock.PutInt(0, size | precedingBlobFreeFlag);
        return totalPages;
    }

    /// <summary>
    /// Returns true if the given page is the first page of its 1&#160;GB segment, used
    /// to prevent coalescing free blobs across segment boundaries.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.isFirstPageOfSegment(int)</c>.</remarks>
    bool IsFirstPageOfSegment(int page)
    {
        return (page & ((0x3fff_ffff) >> pageSizeShift)) == 0;
    }

    /// <summary>
    /// Returns the first free blob of the trunk free-table slot covering the given
    /// page count, or zero if none.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.getFreeTableBlob(ByteBuffer, int)</c>.</remarks>
    static int GetFreeTableBlob(NioBufferWriter rootBlock, int pages)
    {
        var trunkSlot = (pages - 1) / 512;
        return rootBlock.GetInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4);
    }

    /// <summary>
    /// Deallocates a blob. Any adjacent free blobs are coalesced, provided that they are
    /// located in the same 1-GB segment.
    /// </summary>
    /// <param name="firstPage">the first page of the blob</param>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.freeBlob(int)</c>.</remarks>
    protected void FreeBlob(int firstPage)
    {
        var rootBlock = GetBlock(0);
        var block = GetBlockOfPage(firstPage);
        var sizeAndFlags = block.GetInt(0);
        var freeFlag = sizeAndFlags & FREE_BLOB_FLAG;
        var precedingBlobFree = sizeAndFlags & PRECEDING_BLOB_FREE_FLAG;

        if (freeFlag != 0)
            throw new StoreException("Attempt to free blob that is already marked as free", Path);

        var totalPages = rootBlock.GetInt(TOTAL_PAGES_OFS);

        var pages = PagesForPayloadSize(sizeAndFlags & PAYLOAD_SIZE_MASK);
        var prevBlob = 0;
        var nextBlob = firstPage + pages;
        var prevPages = 0;
        var nextPages = 0;

        if (precedingBlobFree != 0 && !IsFirstPageOfSegment(firstPage))
        {
            var prevTailBlock = GetBlockOfPage(firstPage - 1);
            prevPages = prevTailBlock.GetInt(TRAILER_OFS);
            prevBlob = firstPage - prevPages;
            var prevBlock = GetBlockOfPage(prevBlob);

            precedingBlobFree = prevBlock.GetInt(0) & PRECEDING_BLOB_FREE_FLAG;
            RemoveFreeBlob(prevBlock);
        }

        if (nextBlob < totalPages && !IsFirstPageOfSegment(nextBlob))
        {
            var nextBlock = GetBlockOfPage(nextBlob);
            var nextSizeAndFlags = nextBlock.GetInt(0);
            if ((nextSizeAndFlags & FREE_BLOB_FLAG) != 0)
            {
                nextPages = PagesForPayloadSize(nextSizeAndFlags & PAYLOAD_SIZE_MASK);
                RemoveFreeBlob(nextBlock);
            }
        }

        if (prevPages != 0)
        {
            var prevFreeTableBlob = GetFreeTableBlob(rootBlock, prevPages);
            if (prevFreeTableBlob == prevBlob)
            {
                RelocateFreeTable(prevFreeTableBlob, prevPages);
            }
        }
        if (nextPages != 0)
        {
            var nextFreeTableBlob = GetFreeTableBlob(rootBlock, nextPages);
            if (nextFreeTableBlob == nextBlob)
            {
                RelocateFreeTable(nextFreeTableBlob, nextPages);
            }
        }

        pages += prevPages + nextPages;
        firstPage -= prevPages;

        if (firstPage + pages == totalPages)
        {
            totalPages = firstPage;
            while (precedingBlobFree != 0)
            {
                var prevTailBlock = GetBlockOfPage(totalPages - 1);
                prevPages = prevTailBlock.GetInt(TRAILER_OFS);
                totalPages -= prevPages;
                prevBlob = totalPages;
                var prevBlock = GetBlockOfPage(prevBlob);
                RemoveFreeBlob(prevBlock);

                var prevFreeTableBlob = GetFreeTableBlob(rootBlock, prevPages);
                if (prevFreeTableBlob == prevBlob)
                {
                    RelocateFreeTable(prevBlob, prevPages);
                }

                if (!IsFirstPageOfSegment(totalPages))
                    break;
                var prevSizeAndFlags = prevBlock.GetInt(0);
                precedingBlobFree = prevSizeAndFlags & PRECEDING_BLOB_FREE_FLAG;
            }
            rootBlock.PutInt(TOTAL_PAGES_OFS, totalPages);
        }
        else
        {
            AddFreeBlob(firstPage, pages, precedingBlobFree);
            var nextBlock = GetBlockOfPage(firstPage + pages);
            var nextSizeAndFlags = nextBlock.GetInt(0);
            nextBlock.PutInt(0, nextSizeAndFlags | PRECEDING_BLOB_FREE_FLAG);
        }
    }

    /// <summary>
    /// Removes a free blob from its freetable. If this blob is the last free blob in a given size
    /// range, removes the leaf freetable from the trunk freetable. If this free blob contains the
    /// leaf freetable, and this freetable is still needed, it is the responsibility of the caller
    /// to copy it to another free blob in the same size range.
    ///
    /// This method does not affect the PRECEDING_BLOB_FREE_FLAG of the successor blob; it is the
    /// responsibility of the caller to clear the flag, if necessary.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.removeFreeBlob(ByteBuffer)</c>.</remarks>
    void RemoveFreeBlob(NioBufferWriter freeBlock)
    {
        var prevBlob = freeBlock.GetInt(PREV_FREE_BLOB_OFS);
        var nextBlob = freeBlock.GetInt(NEXT_FREE_BLOB_OFS);

        if (nextBlob != 0)
        {
            var nextBlock = GetBlockOfPage(nextBlob);
            nextBlock.PutInt(PREV_FREE_BLOB_OFS, prevBlob);
        }
        if (prevBlob != 0)
        {
            var prevBlock = GetBlockOfPage(prevBlob);
            prevBlock.PutInt(NEXT_FREE_BLOB_OFS, nextBlob);
            return;
        }

        var payloadSize = freeBlock.GetInt(0) & 0x3fff_ffff;
        Debug.Assert(((payloadSize + 4) & Ushr(unchecked((int)0xffff_ffff), 32 - pageSizeShift)) == 0,
            "Payload size + 4 of a free blob must be multiple of page size");
        var pages = (payloadSize + 4) >> pageSizeShift;
        var trunkSlot = (pages - 1) / 512;
        var leafSlot = (pages - 1) % 512;

        var rootBlock = GetBlock(0);
        var trunkOfs = TRUNK_FREE_TABLE_OFS + trunkSlot * 4;
        var leafOfs = LEAF_FREE_TABLE_OFS + leafSlot * 4;
        var leafBlob = rootBlock.GetInt(trunkOfs);

        Debug.Assert(leafBlob != 0);

        var leafBlock = GetBlockOfPage(leafBlob);
        leafBlock.PutInt(leafOfs, nextBlob);
        if (nextBlob != 0)
            return;

        var leafRange = leafSlot / 16;
        Debug.Assert(leafRange >= 0 && leafRange < 32);

        leafOfs = LEAF_FREE_TABLE_OFS + (leafRange * 64);
        var leafEnd = leafOfs + 64;
        for (; leafOfs < leafEnd; leafOfs += 4)
        {
            if (leafBlock.GetInt(leafOfs) != 0)
                return;
        }

        var leafRangeBits = leafBlock.GetInt(LEAF_FT_RANGE_BITS_OFS);
        leafRangeBits &= ~(1 << leafRange);
        leafBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, leafRangeBits);
        if (leafRangeBits != 0)
            return;

        rootBlock.PutInt(trunkOfs, 0);

        var trunkRange = trunkSlot / 16;
        Debug.Assert(trunkRange >= 0 && trunkRange < 32);

        trunkOfs = TRUNK_FREE_TABLE_OFS + (trunkRange * 64);
        var trunkEnd = trunkOfs + 64;
        for (; trunkOfs < trunkEnd; trunkOfs += 4)
        {
            if (rootBlock.GetInt(trunkOfs) != 0)
                return;
        }

        var trunkRangeBits = rootBlock.GetInt(TRUNK_FT_RANGE_BITS_OFS);
        trunkRangeBits &= ~(1 << trunkRange);
        rootBlock.PutInt(TRUNK_FT_RANGE_BITS_OFS, trunkRangeBits);
    }

    /// <summary>
    /// Adds a blob to the freetable, and sets its size, header flags and trailer.
    ///
    /// This method does not affect the PRECEDING_BLOB_FREE_FLAG of the successor blob; it is the
    /// responsibility of the caller to set the flag, if necessary.
    /// </summary>
    /// <param name="firstPage">the first page of the blob</param>
    /// <param name="pages">the number of pages of this blob</param>
    /// <param name="freeFlags">PRECEDING_BLOB_FREE_FLAG or 0</param>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.addFreeBlob(int, int, int)</c>.</remarks>
    void AddFreeBlob(int firstPage, int pages, int freeFlags)
    {
        Debug.Assert(freeFlags == 0 || freeFlags == PRECEDING_BLOB_FREE_FLAG);
        var firstBlock = GetBlockOfPage(firstPage);
        var payloadSize = (pages << pageSizeShift) - 4;
        firstBlock.PutInt(0, payloadSize | FREE_BLOB_FLAG | freeFlags);
        firstBlock.PutInt(PREV_FREE_BLOB_OFS, 0);
        var lastBlock = GetBlockOfPage(firstPage + pages - 1);
        lastBlock.PutInt(TRAILER_OFS, pages);
        var rootBlock = GetBlock(0);
        var trunkSlot = (pages - 1) / 512;
        var leafBlob = rootBlock.GetInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4);
        NioBufferWriter leafBlock;
        if (leafBlob == 0)
        {
            firstBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, 0);
            for (var i = 0; i < 2048; i += 4)
            {
                firstBlock.PutInt(LEAF_FREE_TABLE_OFS + i, 0);
            }
            var trunkRanges = rootBlock.GetInt(TRUNK_FT_RANGE_BITS_OFS);
            trunkRanges |= 1 << (trunkSlot / 16);
            rootBlock.PutInt(TRUNK_FT_RANGE_BITS_OFS, trunkRanges);
            rootBlock.PutInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4, firstPage);
            leafBlock = firstBlock;
        }
        else
        {
            leafBlock = GetBlockOfPage(leafBlob);
        }

        var leafSlot = (pages - 1) % 512;
        var leafOfs = LEAF_FREE_TABLE_OFS + leafSlot * 4;
        var nextBlob = leafBlock.GetInt(leafOfs);
        if (nextBlob != 0)
        {
            var nextBlock = GetBlockOfPage(nextBlob);
            nextBlock.PutInt(PREV_FREE_BLOB_OFS, firstPage);
        }
        firstBlock.PutInt(NEXT_FREE_BLOB_OFS, nextBlob);

        leafBlock.PutInt(leafOfs, firstPage);
        var leafRanges = leafBlock.GetInt(LEAF_FT_RANGE_BITS_OFS);
        leafRanges |= 1 << (leafSlot / 16);
        leafBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, leafRanges);
    }

    /// <summary>
    /// Copies a blob's free table to another free blob. The original blob's free table and the
    /// free-range bits must be valid, all other data is allowed to have been modified at this point.
    /// </summary>
    /// <param name="page">the first page of the original blob</param>
    /// <param name="sizeInPages">the blob's size in pages</param>
    /// <returns>
    /// the page of the blob to which the free table has been assigned, or 0 if the table has not
    /// been relocated.
    /// </returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.relocateFreeTable(int, int)</c>.</remarks>
    int RelocateFreeTable(int page, int sizeInPages)
    {
        var block = GetBlockOfPage(page);
        var ranges = block.GetInt(LEAF_FT_RANGE_BITS_OFS);
        var originalRanges = ranges;
        var p = LEAF_FREE_TABLE_OFS;
        while (ranges != 0)
        {
            if ((ranges & 1) != 0)
            {
                var pEnd = p + 64;
                for (; p < pEnd; p += 4)
                {
                    var otherPage = block.GetInt(p);
                    if (otherPage != 0 && otherPage != page)
                    {
                        var otherBlock = GetBlockOfPage(otherPage);
                        Debug.Assert((otherBlock.GetInt(0) & FREE_BLOB_FLAG) != 0,
                            string.Format(CultureInfo.InvariantCulture, "Found allocated blob (First page = {0}) in FT", otherPage));

                        for (var i = LEAF_FREE_TABLE_OFS;
                             i < LEAF_FREE_TABLE_OFS + FREE_TABLE_LEN; i += 4)
                        {
                            otherBlock.PutInt(i, block.GetInt(i));
                        }
                        otherBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, originalRanges);
                        var rootBlock = GetBlock(0);
                        var trunkSlot = (sizeInPages - 1) / 512;
                        rootBlock.PutInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4, otherPage);

                        return otherPage;
                    }
                }
                p = pEnd;
                ranges = Ushr(ranges, 1);
            }
            else
            {
                var rangesToSkip = BitOperations.TrailingZeroCount((uint)ranges);
                ranges = Ushr(ranges, rangesToSkip);
                p += rangesToSkip * 64;
            }
        }
        return 0;
    }

    // TODO: remove
    /// <summary>
    /// Exports the blob starting at the given page to a gzip-compressed file, masking
    /// the store-internal flag bits out of its header.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.export(int, Path)</c>.</remarks>
    public void Export(int page, string path)
    {
        var buf = new NioBufferReader(SegmentOfPage(page).Memory);
        var p = OffsetOfPage(page);
        var len = buf.GetInt(p) & 0x3fff_ffff;
        const int BUF_SIZE = 64 * 1024;
        var b = new byte[BUF_SIZE];
        var bytesRemaining = len;
        using FileStream fout = new FileStream(path, FileMode.Create, FileAccess.Write);
        using GZipStream @out = new GZipStream(fout, CompressionMode.Compress);
        byte flagMask = 0x3f;
        while (bytesRemaining > 0)
        {
            var chunkSize = System.Math.Min(bytesRemaining, BUF_SIZE);
            buf.Get(p, b, 0, chunkSize);
            b[3] &= flagMask;
            flagMask = 0xff;
            @out.Write(b, 0, chunkSize);
            bytesRemaining -= chunkSize;
            p += chunkSize;
        }
    }

    /// <summary>
    /// Returns the first page of the blob with the given id from the blob index, or
    /// zero if the blob is not present.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.getIndexEntry(int)</c>.</remarks>
    protected internal int GetIndexEntry(int id)
    {
        return BaseSegment.Memory.Span.GetIntLE(IndexPointer() + id * 4);
    }

    /// <summary>
    /// Sets the blob index entry for the given id to point at the given first page.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.setIndexEntry(int, int)</c>.</remarks>
    protected internal void SetIndexEntry(int id, int page)
    {
        var pIndexEntry = IndexPointer() + id * 4;
        var indexBlock = GetBlock(pIndexEntry & unchecked((int)0xffff_f000)); // TODO: assumes block length 4096
        indexBlock.PutInt(pIndexEntry % BLOCK_LEN, page);
    }

    /// <summary>
    /// Returns the first page of the blob with the given id, downloading it from the
    /// configured repository if it is not already present. Throws when the blob is
    /// missing and no repository is configured.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.fetchBlob(int)</c>.</remarks>
    public int FetchBlob(int id)
    {
        var page = GetIndexEntry(id);
        if (page != 0)
            return page;
        if (downloader == null)
        {
            throw new StoreException(string.Format(CultureInfo.InvariantCulture,
                "Cannot download {0:X6}; repository URL must be specified", id), Path);
        }
        var ticket = downloader.Request(id, null);
        ticket.AwaitCompletion();
        ticket.ThrowError();
        return ticket.Page();
    }

    /// <summary>
    /// Shuts down the downloader if present and closes the underlying store.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.close()</c>.</remarks>
    public new void Close()
    {
        if (downloader != null)
            downloader.Shutdown();
        base.Close();
    }

    /// <summary>
    /// Frees and unindexes every blob produced by the given iterator within a single
    /// exclusive transaction.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.removeBlobs(IntIterator)</c>.</remarks>
    public void RemoveBlobs(IEnumerator<int> iter)
    {
        BeginTransaction(LOCK_EXCLUSIVE);
        while (iter.MoveNext())
        {
            var id = iter.Current;
            var page = GetIndexEntry(id);
            FreeBlob(page);
            SetIndexEntry(id, 0);
        }
        Commit();
        EndTransaction();
    }

    /// <summary>
    /// Resets the metadata section to a blank state (so it can be copied or exported). This method
    /// is *never* applied to the BlobStore's live metadata, but always a copy.
    ///
    /// The base implementation clears the free-blob table and sets the total page count to zero
    /// (to use the metadata in a new BlobStore, this count will need to be recalculated to the
    /// number of pages occupied by the metadata section, based on the new BlobStore's page size).
    /// </summary>
    /// <param name="buf">the buffer containing a copy of the BlobStore's metadata</param>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.resetMetadata(ByteBuffer)</c>.</remarks>
    protected virtual void ResetMetadata(NioBufferWriter buf)
    {
        buf.PutInt(TRUNK_FT_RANGE_BITS_OFS, 0);
        for (var i = 0; i < 512; i++)
            buf.PutInt(TRUNK_FREE_TABLE_OFS + i, 0);
        buf.PutInt(TOTAL_PAGES_OFS, 0);
    }

    /// <summary>
    /// Creates a new, empty store file at the given path that contains only this
    /// store's reset metadata section, sized to the pages the metadata occupies.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStore.createCopy(Path)</c>.</remarks>
    public void CreateCopy(string newPath)
    {
        var metadataSize = BaseSegment.Memory.Span.GetIntLE(METADATA_SIZE_OFS);
        var bytes = new byte[metadataSize];
        BaseSegment.Memory.Span.Slice(0, metadataSize).CopyTo(bytes);
        var buf = new NioBufferWriter(bytes);
        ResetMetadata(buf);
        buf.PutInt(TOTAL_PAGES_OFS, BytesToPages(metadataSize));

        using FileStream channel = new FileStream(newPath, FileMode.Create, FileAccess.Write);
        channel.Write(bytes, 0, metadataSize);
    }

}
