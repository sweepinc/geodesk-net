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
using ByteOrder = Java.Nio.ByteOrder;
using NioBuffer = Java.Nio.ByteBuffer;
using static Clarisma.Common.Store.BlobStoreConstants;

namespace Clarisma.Common.Store;

/// <summary>
/// A Blob Store is a file containing a large number of individual binary objects (blobs)
/// that span multiple contiguous file pages. See the Java original for the full format
/// description. Uses the journaling mechanism of <see cref="Store"/>.
/// </summary>
public class BlobStore : Store
{
    /// <summary>
    /// The number of bits to shift left to turn number of pages into number of bytes
    /// (Page size is always a power of two).
    /// </summary>
    protected internal int pageSizeShift = 12; // 4KB default page
    protected Downloader? downloader;

    private static int Ushr(int v, int n) => (int)((uint)v >> n);

    protected int DownloadBlob(Uri url)
    {
        return 0; // TODO
    }

    public void SetRepository(string url)
    {
        Debug.Assert(downloader == null);
        downloader = new Downloader(this, url);
    }

    protected override void CreateStore()
    {
        // TODO: should this be inside a transaction?
        NioBuffer buf = baseMapping!;
        buf.PutInt(0, MAGIC);
        buf.PutInt(VERSION_OFS, VERSION);
        buf.PutLong(TIMESTAMP_OFS, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        buf.PutInt(METADATA_SIZE_OFS, DEFAULT_METADATA_SIZE);
        buf.PutInt(TOTAL_PAGES_OFS, 1);
        // TODO: page size
    }

    protected override long GetTimestamp()
    {
        return baseMapping!.GetLong(TIMESTAMP_OFS);
    }

    // PORT NOTE: .NET Guid byte layout differs from Java UUID(high,low); this reads the
    // 16 raw GUID bytes. Not exercised by tests.
    public Guid GetGuid()
    {
        byte[] bytes = new byte[GUID_LEN];
        baseMapping!.Get(GUID_OFS, bytes);
        return new Guid(bytes);
    }

    protected override void VerifyHeader()
    {
        NioBuffer buf = baseMapping!;
        if (buf.GetInt(0) != MAGIC)
        {
            throw new StoreException("Not a BlobStore file", Path);
        }
        if (buf.GetInt(VERSION_OFS) != VERSION)
        {
            throw new StoreException(
                "Wrong BlobStore version (Requires 1.0)", Path);
        }
        // TODO: page size
    }

    /// <summary>
    /// Checks whether this BlobStore is *empty*. An empty store is valid, but has no contents.
    /// </summary>
    protected internal bool IsEmpty()
    {
        return baseMapping!.GetInt(INDEX_PTR_OFS) == 0; // TODO
    }

    protected override void Initialize()
    {
        base.Initialize();
        if (IsEmpty() && downloader != null)
        {
            Downloader.Ticket ticket = downloader.Request(Downloader.METADATA_ID, null);
            ticket.AwaitCompletion();
            ticket.ThrowError();
        }
    }

    protected override long GetTrueSize()
    {
        return ((long)baseMapping!.GetInt(TOTAL_PAGES_OFS)) << pageSizeShift;
    }

    // TODO: naming
    public NioBuffer BaseMapping()
    {
        return baseMapping!;
    }

    public NioBuffer BufferOfPage(int page)
    {
        return GetMapping(page >> (30 - pageSizeShift));
    }

    public int OffsetOfPage(int page)
    {
        return (page << pageSizeShift) & 0x3fff_ffff;
    }

    public long AbsoluteOffsetOfPage(int page)
    {
        return ((long)page) << pageSizeShift;
    }

    protected internal NioBuffer GetBlockOfPage(int page)
    {
        Debug.Assert(page >= 0); // TODO: treat page as unsigned int?
        return GetBlock(((long)page) << pageSizeShift);
    }

    public int PageSize()
    {
        return 1 << pageSizeShift;
    }

    public int IndexPointer()
    {
        return baseMapping!.GetInt(INDEX_PTR_OFS) + INDEX_PTR_OFS;
    }

    // TODO: should also make sure page does not lie in meta space
    protected void CheckPage(int page)
    {
        if (page < 0 || page >= baseMapping!.GetInt(TOTAL_PAGES_OFS))
        {
            throw new StoreException("Invalid page: " + page, Path);
        }
    }

    /// <summary>
    /// Determines the number of pages needed to store a blob.
    /// </summary>
    protected internal int PagesForPayloadSize(int size)
    {
        Debug.Assert(size > 0 && size <= ((1 << 30) - 4));
        return (size + (1 << pageSizeShift) + 3) >> pageSizeShift;
    }

    /// <summary>
    /// Determines the number of pages needed to store the given number of bytes.
    /// </summary>
    protected internal int BytesToPages(int len)
    {
        Debug.Assert(len > 0 && len <= (1 << 30));
        return (len + (1 << pageSizeShift) - 1) >> pageSizeShift;
    }

    /// <summary>
    /// Allocates a blob of a given size.
    /// </summary>
    /// <param name="size">the size of the blob, not including its 4-byte header</param>
    /// <returns>the first page of the blob</returns>
    protected internal int AllocateBlob(int size)
    {
        Debug.Assert(size >= 0 && size <= ((1 << 30) - 4));
        int precedingBlobFreeFlag = 0;
        int requiredPages = PagesForPayloadSize(size);
        NioBuffer rootBlock = GetBlock(0);
        int trunkRanges = rootBlock.GetInt(TRUNK_FT_RANGE_BITS_OFS);
        if (trunkRanges != 0)
        {
            int trunkSlot = (requiredPages - 1) / 512;
            int leafSlot = (requiredPages - 1) % 512;
            int trunkOfs = TRUNK_FREE_TABLE_OFS + trunkSlot * 4;
            int trunkEnd = (trunkOfs & unchecked((int)0xffff_ffc0)) + 64;

            trunkRanges = Ushr(trunkRanges, trunkSlot / 16);

            for (; ; )
            {
                if ((trunkRanges & 1) == 0)
                {
                    if (trunkRanges == 0) break;

                    int rangesToSkip = BitOperations.TrailingZeroCount((uint)trunkRanges);
                    trunkEnd += rangesToSkip * 64;
                    trunkOfs = trunkEnd - 64;

                    leafSlot = 0;
                }
                Debug.Assert(trunkOfs < TRUNK_FREE_TABLE_OFS + FREE_TABLE_LEN);

                for (; trunkOfs < trunkEnd; trunkOfs += 4)
                {
                    int leafTableBlob = rootBlock.GetInt(trunkOfs);
                    if (leafTableBlob == 0) continue;

                    NioBuffer leafBlock = GetBlockOfPage(leafTableBlob);
                    int leafRanges = leafBlock.GetInt(LEAF_FT_RANGE_BITS_OFS);
                    int leafOfs = LEAF_FREE_TABLE_OFS + leafSlot * 4;
                    int leafEnd = (leafOfs & unchecked((int)0xffff_ffc0)) + 64;

                    Debug.Assert((leafBlock.GetInt(0) & FREE_BLOB_FLAG) != 0,
                        string.Format(CultureInfo.InvariantCulture, "Leaf FB blob {0} must be a free blob", leafTableBlob));

                    leafRanges = Ushr(leafRanges, leafSlot / 16);

                    for (; ; )
                    {
                        if ((leafRanges & 1) == 0)
                        {
                            if (leafRanges == 0) break;
                            int rangesToSkip = BitOperations.TrailingZeroCount((uint)leafRanges);
                            leafEnd += rangesToSkip * 64;
                            leafOfs = leafEnd - 64;
                        }
                        for (; leafOfs < leafEnd; leafOfs += 4)
                        {
                            int freeBlob = leafBlock.GetInt(leafOfs);
                            if (freeBlob == 0) continue;

                            int freePages = ((trunkOfs - TRUNK_FREE_TABLE_OFS) << 7) +
                                ((leafOfs - LEAF_FREE_TABLE_OFS) >> 2) + 1;
                            if (freeBlob == leafTableBlob)
                            {
                                int nextFreeBlob = leafBlock.GetInt(NEXT_FREE_BLOB_OFS);
                                if (nextFreeBlob != 0)
                                {
                                    freeBlob = nextFreeBlob;
                                }
                            }

                            NioBuffer freeBlock = GetBlockOfPage(freeBlob);
                            int header = freeBlock.GetInt(0);
                            Debug.Assert((header & FREE_BLOB_FLAG) != 0,
                                string.Format(CultureInfo.InvariantCulture, "Blob {0} is not free", freeBlob));
                            int freeBlobPayloadSize = header & PAYLOAD_SIZE_MASK;
                            Debug.Assert((freeBlobPayloadSize + 4) >> pageSizeShift == freePages);
                            Debug.Assert(freePages >= requiredPages);

                            precedingBlobFreeFlag = header & PRECEDING_BLOB_FREE_FLAG;
                            RemoveFreeBlob(freeBlock);

                            if (freeBlob == leafTableBlob)
                            {
                                int newLeafBlob = RelocateFreeTable(freeBlob, freePages);
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
                                NioBuffer nextBlock = GetBlockOfPage(freeBlob + freePages);
                                int nextSizeAndFlags = nextBlock.GetInt(0);
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

        int totalPages = rootBlock.GetInt(TOTAL_PAGES_OFS);
        int pagesPerSegment = (1 << 30) >> pageSizeShift;
        int remainingPages = pagesPerSegment - (totalPages & (pagesPerSegment - 1));
        if (remainingPages < requiredPages)
        {
            AddFreeBlob(totalPages, remainingPages, 0);
            totalPages += remainingPages;
            precedingBlobFreeFlag = PRECEDING_BLOB_FREE_FLAG;
        }
        rootBlock.PutInt(TOTAL_PAGES_OFS, totalPages + requiredPages);

        NioBuffer newBlock = GetBlockOfPage(totalPages);
        newBlock.PutInt(0, size | precedingBlobFreeFlag);
        return totalPages;
    }

    private bool IsFirstPageOfSegment(int page)
    {
        return (page & ((0x3fff_ffff) >> pageSizeShift)) == 0;
    }

    private static int GetFreeTableBlob(NioBuffer rootBlock, int pages)
    {
        int trunkSlot = (pages - 1) / 512;
        return rootBlock.GetInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4);
    }

    /// <summary>
    /// Deallocates a blob. Any adjacent free blobs are coalesced, provided that they are
    /// located in the same 1-GB segment.
    /// </summary>
    protected void FreeBlob(int firstPage)
    {
        NioBuffer rootBlock = GetBlock(0);
        NioBuffer block = GetBlockOfPage(firstPage);
        int sizeAndFlags = block.GetInt(0);
        int freeFlag = sizeAndFlags & FREE_BLOB_FLAG;
        int precedingBlobFree = sizeAndFlags & PRECEDING_BLOB_FREE_FLAG;

        if (freeFlag != 0)
        {
            throw new StoreException(
                "Attempt to free blob that is already marked as free", Path);
        }

        int totalPages = rootBlock.GetInt(TOTAL_PAGES_OFS);

        int pages = PagesForPayloadSize(sizeAndFlags & PAYLOAD_SIZE_MASK);
        int prevBlob = 0;
        int nextBlob = firstPage + pages;
        int prevPages = 0;
        int nextPages = 0;

        if (precedingBlobFree != 0 && !IsFirstPageOfSegment(firstPage))
        {
            NioBuffer prevTailBlock = GetBlockOfPage(firstPage - 1);
            prevPages = prevTailBlock.GetInt(TRAILER_OFS);
            prevBlob = firstPage - prevPages;
            NioBuffer prevBlock = GetBlockOfPage(prevBlob);

            precedingBlobFree = prevBlock.GetInt(0) & PRECEDING_BLOB_FREE_FLAG;
            RemoveFreeBlob(prevBlock);
        }

        if (nextBlob < totalPages && !IsFirstPageOfSegment(nextBlob))
        {
            NioBuffer nextBlock = GetBlockOfPage(nextBlob);
            int nextSizeAndFlags = nextBlock.GetInt(0);
            if ((nextSizeAndFlags & FREE_BLOB_FLAG) != 0)
            {
                nextPages = PagesForPayloadSize(nextSizeAndFlags & PAYLOAD_SIZE_MASK);
                RemoveFreeBlob(nextBlock);
            }
        }

        if (prevPages != 0)
        {
            int prevFreeTableBlob = GetFreeTableBlob(rootBlock, prevPages);
            if (prevFreeTableBlob == prevBlob)
            {
                RelocateFreeTable(prevFreeTableBlob, prevPages);
            }
        }
        if (nextPages != 0)
        {
            int nextFreeTableBlob = GetFreeTableBlob(rootBlock, nextPages);
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
                NioBuffer prevTailBlock = GetBlockOfPage(totalPages - 1);
                prevPages = prevTailBlock.GetInt(TRAILER_OFS);
                totalPages -= prevPages;
                prevBlob = totalPages;
                NioBuffer prevBlock = GetBlockOfPage(prevBlob);
                RemoveFreeBlob(prevBlock);

                int prevFreeTableBlob = GetFreeTableBlob(rootBlock, prevPages);
                if (prevFreeTableBlob == prevBlob)
                {
                    RelocateFreeTable(prevBlob, prevPages);
                }

                if (!IsFirstPageOfSegment(totalPages)) break;
                int prevSizeAndFlags = prevBlock.GetInt(0);
                precedingBlobFree = prevSizeAndFlags & PRECEDING_BLOB_FREE_FLAG;
            }
            rootBlock.PutInt(TOTAL_PAGES_OFS, totalPages);
        }
        else
        {
            AddFreeBlob(firstPage, pages, precedingBlobFree);
            NioBuffer nextBlock = GetBlockOfPage(firstPage + pages);
            int nextSizeAndFlags = nextBlock.GetInt(0);
            nextBlock.PutInt(0, nextSizeAndFlags | PRECEDING_BLOB_FREE_FLAG);
        }
    }

    /// <summary>
    /// Removes a free blob from its freetable.
    /// </summary>
    private void RemoveFreeBlob(NioBuffer freeBlock)
    {
        int prevBlob = freeBlock.GetInt(PREV_FREE_BLOB_OFS);
        int nextBlob = freeBlock.GetInt(NEXT_FREE_BLOB_OFS);

        if (nextBlob != 0)
        {
            NioBuffer nextBlock = GetBlockOfPage(nextBlob);
            nextBlock.PutInt(PREV_FREE_BLOB_OFS, prevBlob);
        }
        if (prevBlob != 0)
        {
            NioBuffer prevBlock = GetBlockOfPage(prevBlob);
            prevBlock.PutInt(NEXT_FREE_BLOB_OFS, nextBlob);
            return;
        }

        int payloadSize = freeBlock.GetInt(0) & 0x3fff_ffff;
        Debug.Assert(((payloadSize + 4) & Ushr(unchecked((int)0xffff_ffff), 32 - pageSizeShift)) == 0,
            "Payload size + 4 of a free blob must be multiple of page size");
        int pages = (payloadSize + 4) >> pageSizeShift;
        int trunkSlot = (pages - 1) / 512;
        int leafSlot = (pages - 1) % 512;

        NioBuffer rootBlock = GetBlock(0);
        int trunkOfs = TRUNK_FREE_TABLE_OFS + trunkSlot * 4;
        int leafOfs = LEAF_FREE_TABLE_OFS + leafSlot * 4;
        int leafBlob = rootBlock.GetInt(trunkOfs);

        Debug.Assert(leafBlob != 0);

        NioBuffer leafBlock = GetBlockOfPage(leafBlob);
        leafBlock.PutInt(leafOfs, nextBlob);
        if (nextBlob != 0) return;

        int leafRange = leafSlot / 16;
        Debug.Assert(leafRange >= 0 && leafRange < 32);

        leafOfs = LEAF_FREE_TABLE_OFS + (leafRange * 64);
        int leafEnd = leafOfs + 64;
        for (; leafOfs < leafEnd; leafOfs += 4)
        {
            if (leafBlock.GetInt(leafOfs) != 0) return;
        }

        int leafRangeBits = leafBlock.GetInt(LEAF_FT_RANGE_BITS_OFS);
        leafRangeBits &= ~(1 << leafRange);
        leafBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, leafRangeBits);
        if (leafRangeBits != 0) return;

        rootBlock.PutInt(trunkOfs, 0);

        int trunkRange = trunkSlot / 16;
        Debug.Assert(trunkRange >= 0 && trunkRange < 32);

        trunkOfs = TRUNK_FREE_TABLE_OFS + (trunkRange * 64);
        int trunkEnd = trunkOfs + 64;
        for (; trunkOfs < trunkEnd; trunkOfs += 4)
        {
            if (rootBlock.GetInt(trunkOfs) != 0) return;
        }

        int trunkRangeBits = rootBlock.GetInt(TRUNK_FT_RANGE_BITS_OFS);
        trunkRangeBits &= ~(1 << trunkRange);
        rootBlock.PutInt(TRUNK_FT_RANGE_BITS_OFS, trunkRangeBits);
    }

    /// <summary>
    /// Adds a blob to the freetable, and sets its size, header flags and trailer.
    /// </summary>
    private void AddFreeBlob(int firstPage, int pages, int freeFlags)
    {
        Debug.Assert(freeFlags == 0 || freeFlags == PRECEDING_BLOB_FREE_FLAG);
        NioBuffer firstBlock = GetBlockOfPage(firstPage);
        int payloadSize = (pages << pageSizeShift) - 4;
        firstBlock.PutInt(0, payloadSize | FREE_BLOB_FLAG | freeFlags);
        firstBlock.PutInt(PREV_FREE_BLOB_OFS, 0);
        NioBuffer lastBlock = GetBlockOfPage(firstPage + pages - 1);
        lastBlock.PutInt(TRAILER_OFS, pages);
        NioBuffer rootBlock = GetBlock(0);
        int trunkSlot = (pages - 1) / 512;
        int leafBlob = rootBlock.GetInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4);
        NioBuffer leafBlock;
        if (leafBlob == 0)
        {
            firstBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, 0);
            for (int i = 0; i < 2048; i += 4)
            {
                firstBlock.PutInt(LEAF_FREE_TABLE_OFS + i, 0);
            }
            int trunkRanges = rootBlock.GetInt(TRUNK_FT_RANGE_BITS_OFS);
            trunkRanges |= 1 << (trunkSlot / 16);
            rootBlock.PutInt(TRUNK_FT_RANGE_BITS_OFS, trunkRanges);
            rootBlock.PutInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4, firstPage);
            leafBlock = firstBlock;
        }
        else
        {
            leafBlock = GetBlockOfPage(leafBlob);
        }

        int leafSlot = (pages - 1) % 512;
        int leafOfs = LEAF_FREE_TABLE_OFS + leafSlot * 4;
        int nextBlob = leafBlock.GetInt(leafOfs);
        if (nextBlob != 0)
        {
            NioBuffer nextBlock = GetBlockOfPage(nextBlob);
            nextBlock.PutInt(PREV_FREE_BLOB_OFS, firstPage);
        }
        firstBlock.PutInt(NEXT_FREE_BLOB_OFS, nextBlob);

        leafBlock.PutInt(leafOfs, firstPage);
        int leafRanges = leafBlock.GetInt(LEAF_FT_RANGE_BITS_OFS);
        leafRanges |= 1 << (leafSlot / 16);
        leafBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, leafRanges);
    }

    /// <summary>
    /// Copies a blob's free table to another free blob.
    /// </summary>
    /// <returns>the page of the blob to which the free table has been assigned, or 0.</returns>
    private int RelocateFreeTable(int page, int sizeInPages)
    {
        NioBuffer block = GetBlockOfPage(page);
        int ranges = block.GetInt(LEAF_FT_RANGE_BITS_OFS);
        int originalRanges = ranges;
        int p = LEAF_FREE_TABLE_OFS;
        while (ranges != 0)
        {
            if ((ranges & 1) != 0)
            {
                int pEnd = p + 64;
                for (; p < pEnd; p += 4)
                {
                    int otherPage = block.GetInt(p);
                    if (otherPage != 0 && otherPage != page)
                    {
                        NioBuffer otherBlock = GetBlockOfPage(otherPage);
                        Debug.Assert((otherBlock.GetInt(0) & FREE_BLOB_FLAG) != 0,
                            string.Format(CultureInfo.InvariantCulture, "Found allocated blob (First page = {0}) in FT", otherPage));

                        for (int i = LEAF_FREE_TABLE_OFS;
                             i < LEAF_FREE_TABLE_OFS + FREE_TABLE_LEN; i += 4)
                        {
                            otherBlock.PutInt(i, block.GetInt(i));
                        }
                        otherBlock.PutInt(LEAF_FT_RANGE_BITS_OFS, originalRanges);
                        NioBuffer rootBlock = GetBlock(0);
                        int trunkSlot = (sizeInPages - 1) / 512;
                        rootBlock.PutInt(TRUNK_FREE_TABLE_OFS + trunkSlot * 4, otherPage);

                        return otherPage;
                    }
                }
                p = pEnd;
                ranges = Ushr(ranges, 1);
            }
            else
            {
                int rangesToSkip = BitOperations.TrailingZeroCount((uint)ranges);
                ranges = Ushr(ranges, rangesToSkip);
                p += rangesToSkip * 64;
            }
        }
        return 0;
    }

    // TODO: remove
    public void Export(int page, string path)
    {
        NioBuffer buf = BufferOfPage(page);
        int p = OffsetOfPage(page);
        int len = buf.GetInt(p) & 0x3fff_ffff;
        const int BUF_SIZE = 64 * 1024;
        byte[] b = new byte[BUF_SIZE];
        int bytesRemaining = len;
        using FileStream fout = new FileStream(path, FileMode.Create, FileAccess.Write);
        using GZipStream @out = new GZipStream(fout, CompressionMode.Compress);
        byte flagMask = 0x3f;
        while (bytesRemaining > 0)
        {
            int chunkSize = System.Math.Min(bytesRemaining, BUF_SIZE);
            buf.Get(b, 0, chunkSize);
            b[3] &= flagMask;
            flagMask = 0xff;
            @out.Write(b, 0, chunkSize);
            bytesRemaining -= chunkSize;
            p += chunkSize;
        }
    }

    protected internal int GetIndexEntry(int id)
    {
        return baseMapping!.GetInt(IndexPointer() + id * 4);
    }

    protected internal void SetIndexEntry(int id, int page)
    {
        int pIndexEntry = IndexPointer() + id * 4;
        NioBuffer indexBlock = GetBlock(pIndexEntry & unchecked((int)0xffff_f000)); // TODO: assumes block length 4096
        indexBlock.PutInt(pIndexEntry % BLOCK_LEN, page);
    }

    public int FetchBlob(int id)
    {
        int page = GetIndexEntry(id);
        if (page != 0) return page;
        if (downloader == null)
        {
            throw new StoreException(string.Format(CultureInfo.InvariantCulture,
                "Cannot download {0:X6}; repository URL must be specified", id), Path);
        }
        Downloader.Ticket ticket = downloader.Request(id, null);
        ticket.AwaitCompletion();
        ticket.ThrowError();
        return ticket.Page();
    }

    public new void Close()
    {
        if (downloader != null) downloader.Shutdown();
        base.Close();
    }

    public void RemoveBlobs(IEnumerator<int> iter)
    {
        BeginTransaction(LOCK_EXCLUSIVE);
        while (iter.MoveNext())
        {
            int id = iter.Current;
            int page = GetIndexEntry(id);
            FreeBlob(page);
            SetIndexEntry(id, 0);
        }
        Commit();
        EndTransaction();
    }

    /// <summary>
    /// Resets the metadata section to a blank state (so it can be copied or exported).
    /// </summary>
    protected virtual void ResetMetadata(NioBuffer buf)
    {
        buf.PutInt(TRUNK_FT_RANGE_BITS_OFS, 0);
        for (int i = 0; i < 512; i++) buf.PutInt(TRUNK_FREE_TABLE_OFS + i, 0);
        buf.PutInt(TOTAL_PAGES_OFS, 0);
    }

    public void CreateCopy(string newPath)
    {
        int metadataSize = baseMapping!.GetInt(METADATA_SIZE_OFS);
        NioBuffer buf = NioBuffer.Allocate(metadataSize);
        buf.Order(ByteOrder.LittleEndian);
        buf.Put(0, baseMapping!, 0, metadataSize);
        ResetMetadata(buf);
        buf.PutInt(TOTAL_PAGES_OFS, BytesToPages(metadataSize));

        using FileStream channel = new FileStream(newPath, FileMode.Create, FileAccess.Write);
        channel.Write(buf.Array()!, 0, metadataSize);
    }
}
