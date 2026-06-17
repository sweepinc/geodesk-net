/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Clarisma.Common.Util;
using NioBuffer = Java.Nio.ByteBuffer;
using static Clarisma.Common.Store.BlobStoreConstants;

namespace Clarisma.Common.Store;

/// <summary>
/// A class that verifies the integrity of a BlobStore.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker</c>.</remarks>
public class BlobStoreChecker
{
    protected readonly BlobStore store;

    private long fileSize;
    private int totalPages;
    private int pageSizeShift;
    private int pageSize;
    private int pagesPerSegment;
    private int metadataSize;
    private readonly Dictionary<int, Blob> blobs = new Dictionary<int, Blob>();
    private readonly List<ErrorEntry> errors = new List<ErrorEntry>();

    protected const int BLOB_REFERENCED_FLAG = 1;
    protected const int FREE_BLOB_REFERENCED_FLAG = 2;
    protected const int VALID_FREE_BLOB_SIZE = 4;
    protected const int VALID_FREE_BLOB_TRAILER = 8;

    public BlobStoreChecker(BlobStore store)
    {
        this.store = store;
        try
        {
            fileSize = store.CurrentFileSize();
        }
        catch (IOException ex)
        {
            Error(0, ex.Message);
        }
        NioBuffer buf = store.GetMapping(0);
        totalPages = buf.GetInt(TOTAL_PAGES_OFS);
        metadataSize = buf.GetInt(METADATA_SIZE_OFS);
        pageSizeShift = 12;
        pageSize = 1 << pageSizeShift;
        pagesPerSegment = (1 << 30) / pageSize;
    }

    public void Check()
    {
        CheckMetadata();
        CheckFreeTables();
        CheckIndex();
        CheckBlobs();
    }

    protected void CheckMetadata()
    {
        if (metadataSize < DEFAULT_METADATA_SIZE || metadataSize > (1 << 30))
        {
            Error(METADATA_SIZE_OFS, "Invalid metadata size: %d", metadataSize);
            return;
        }
    }

    protected virtual void CheckIndex()
    {
        // TODO: abstract?
    }

    protected void CheckBlobs()
    {
        List<Blob> blobList = new List<Blob>(blobs.Values);
        blobList.Sort();
        int metadataPages = (metadataSize + pageSize - 1) / pageSize;
        int nextPage = metadataPages;
        Blob? prevBlob = null;
        foreach (Blob blob in blobList)
        {
            bool gapsOrOverlaps = false;
            if (blob.firstPage != nextPage)
            {
                if (blob.firstPage < metadataPages)
                {
                    Error(blob, " overlaps metadata", blob.firstPage);
                    gapsOrOverlaps = true;
                }
                else if (blob.firstPage < nextPage)
                {
                    Error(blob, " overlaps Blob %d", prevBlob!.firstPage);
                    gapsOrOverlaps = true;
                }
                else
                {
                    CheckUnreferenced(nextPage, blob.firstPage);
                    gapsOrOverlaps = true;
                }
            }
            if (blob.IsFree())
            {
                if (blob.HasFlags(BLOB_REFERENCED_FLAG))
                {
                    Error(blob, " is marked free but is still in use");
                }
                else
                {
                    if (!blob.HasFlags(VALID_FREE_BLOB_SIZE))
                    {
                        Error(blob, ": Invalid size for free blob");
                    }
                    if (!blob.HasFlags(VALID_FREE_BLOB_TRAILER))
                    {
                        Error(blob, ": Invalid free-blob trailer");
                    }
                }
            }
            bool blobStartsAtSegment = AbsPosOfPage(blob.firstPage) % (1 << 30) == 0;
            if (prevBlob != null && !gapsOrOverlaps)
            {
                if (prevBlob.IsFree())
                {
                    if (blob.IsFree() && !blobStartsAtSegment)
                    {
                        Error(blob, " should have been consolidated with " +
                            "previous free blob");
                    }
                    if (!blob.HasFlags(PRECEDING_BLOB_FREE_FLAG))
                    {
                        Error(blob, ": Preceding blob is free, " +
                            "but prev_blob_free flag not set");
                    }
                }
                else
                {
                    if (blob.HasFlags(PRECEDING_BLOB_FREE_FLAG))
                    {
                        Error(blob, ": Preceding blob in use, " +
                            "but prev_blob_free flag set");
                    }
                }
            }
            nextPage = blob.firstPage + blob.pages;
            prevBlob = blob;
        }

        if (nextPage == totalPages) return;
        if (nextPage > totalPages)
        {
            Error(TOTAL_PAGES_OFS, "total_pages should be %d instead of %d",
                nextPage, totalPages);
            return;
        }
        CheckUnreferenced(nextPage, totalPages);
    }

    private void CheckUnreferenced(int start, int end)
    {
        Error(AbsPosOfPage(start), "%d page%s of unreferenced data",
            end - start, end - start == 1 ? "" : "s");
    }

    protected NioBuffer BufferOfPage(int page)
    {
        return store.GetMapping(page / pagesPerSegment);
    }

    protected int OffsetOfPage(int page)
    {
        return (page % pagesPerSegment) * pageSize;
    }

    protected long AbsPosOfPage(int page)
    {
        return (long)page * pageSize;
    }

    private void CheckFreeTables()
    {
        NioBuffer buf = BufferOfPage(0);
        int rangesUsed = 0;
        int p = TRUNK_FREE_TABLE_OFS;
        for (int slot = 0; slot < 512; slot++, p += 4)
        {
            int freePage = buf.GetInt(p);
            if (freePage == 0) continue;
            rangesUsed |= 1 << (slot >> 4);
            Blob? freeBlob = GetValidBlob(p, freePage);
            if (freeBlob == null) continue;
            if (!CheckBlobIsFree(freeBlob)) continue;
            if (!freeBlob.HasFlags(VALID_FREE_BLOB_SIZE | VALID_FREE_BLOB_TRAILER)) continue;
            CheckLeafFreeTable(slot, freeBlob);
        }
        CheckExpectedVsActual(TRUNK_FT_RANGE_BITS_OFS, "trunk_free_range_mask",
            rangesUsed, buf.GetInt(TRUNK_FT_RANGE_BITS_OFS));
    }

    private void CheckExpectedVsActual(long ofs, string what, int expected, int actual)
    {
        if (expected != actual)
        {
            Error(ofs, "%s should be %08X instead of %08X", what, expected, actual);
        }
    }

    private bool CheckBlobIsFree(Blob blob)
    {
        if (!blob.HasFlags(FREE_BLOB_FLAG))
        {
            Error(blob, " should be free");
            return false;
        }
        return true;
    }

    private void CheckLeafFreeTable(int trunkSlot, Blob blob)
    {
        int minPages = (trunkSlot * 512) + 1;
        int maxPages = minPages + 511;

        int page = blob.firstPage;
        long ofs = AbsPosOfPage(page);
        if (blob.pages < minPages || blob.pages > maxPages)
        {
            Error(ofs, "Free blob with %d pages in wrong size range (%d to %d)",
                blob.pages, minPages, maxPages);
            return;
        }

        NioBuffer buf = BufferOfPage(page);
        int p = OffsetOfPage(page);
        int rangeMask = buf.GetInt(p + LEAF_FT_RANGE_BITS_OFS);
        int rangesUsed = 0;
        p += LEAF_FREE_TABLE_OFS;
        for (int leafSlot = 0; leafSlot < 512; leafSlot++, p += 4)
        {
            int freePage = buf.GetInt(p);
            if (freePage == 0) continue;
            rangesUsed |= 1 << (leafSlot >> 4);
            Blob? freeBlob = GetValidBlob(ofs + p, freePage);
            if (freeBlob != null) CheckFreeBlobChain(trunkSlot, leafSlot, freeBlob);
        }

        CheckExpectedVsActual(ofs + LEAF_FREE_TABLE_OFS, "leaf_free_range_mask",
            rangesUsed, rangeMask);

        if (rangesUsed == 0) Error(blob, "Leaf free-table must have at least one entry");
    }

    private void CheckFreeBlobChain(int trunkSlot, int leafSlot, Blob blob)
    {
        int len = trunkSlot * 512 + leafSlot + 1;
        int prevFreePage = 0;

        for (; ; )
        {
            blob.flags |= FREE_BLOB_REFERENCED_FLAG;
            if (!CheckBlobIsFree(blob)) return;
            long ofs = AbsPosOfPage(blob.firstPage);
            if (blob.pages != len)
            {
                Error(ofs, "Blob with %d pages listed in freetable for page size %d",
                    blob.pages, len);
            }
            NioBuffer buf = BufferOfPage(blob.firstPage);
            int p = OffsetOfPage(blob.firstPage);
            int pPrev = p + PREV_FREE_BLOB_OFS;
            int pNext = p + NEXT_FREE_BLOB_OFS;
            long ofsPrev = ofs + pPrev;
            long ofsNext = ofs + pNext;
            int prev = buf.GetInt(pPrev);
            int next = buf.GetInt(pNext);

            if (prev != prevFreePage)
            {
                Error(ofsPrev, "prev_free_blob should be %d, not %d", prevFreePage, prev);
            }
            if (next == 0) return;
            prevFreePage = blob.firstPage;
            Blob? nextBlob = GetValidBlob(ofsNext, next);
            if (nextBlob == null) return;
            if (!CheckBlobIsFree(nextBlob)) return;
            if (nextBlob.HasFlags(FREE_BLOB_REFERENCED_FLAG))
            {
                Error(ofsNext, "Circular reference in free-blob list (to %d)", next);
                return;
            }
            blob = nextBlob;
        }
    }

    private sealed class ErrorEntry : IComparable<ErrorEntry>
    {
        internal readonly long location;
        internal readonly string message;

        internal ErrorEntry(long location, string message)
        {
            this.location = location;
            this.message = message;
        }

        public int CompareTo(ErrorEntry? other)
        {
            return location.CompareTo(other!.location);
        }

        public override string ToString()
        {
            return JavaFormat.Format("%08X: %s", location, message);
        }
    }

    protected void Error(long ofs, string msg, params object?[] args)
    {
        errors.Add(new ErrorEntry(ofs, JavaFormat.Format(msg, args)));
    }

    protected void Error(Blob blob, string msg, params object?[] args)
    {
        errors.Add(new ErrorEntry(AbsPosOfPage(blob.firstPage),
            JavaFormat.Format("Blob " + blob.firstPage + msg, args)));
    }

    public class Blob : IComparable<Blob>
    {
        internal int firstPage;
        internal int pages;
        internal int flags;

        internal Blob(int firstPage, int pages, int flags)
        {
            this.firstPage = firstPage;
            this.pages = pages;
            this.flags = flags;
        }

        internal bool IsReferenced()
        {
            return (flags & BLOB_REFERENCED_FLAG) != 0;
        }

        internal bool IsFree()
        {
            return (flags & FREE_BLOB_FLAG) != 0;
        }

        internal bool HasFlags(int flags)
        {
            return (this.flags & flags) == flags;
        }

        public int CompareTo(Blob? other)
        {
            return firstPage.CompareTo(other!.firstPage);
        }
    }

    private Blob? GetValidBlob(long ofs, int page)
    {
        Blob? blob = GetBlob(page);
        if (blob == null) Error(ofs, "Bad blob reference: %d", page);
        return blob;
    }

    public Blob? UseBlob(long ofs, int page)
    {
        Blob? blob = GetValidBlob(ofs, page);
        if (blob != null) blob.flags |= BLOB_REFERENCED_FLAG;
        return blob;
    }

    private Blob? GetBlob(int page)
    {
        if (blobs.TryGetValue(page, out Blob? existing)) return existing;
        if (page <= 0) return null;
        if ((page << pageSizeShift) >= fileSize) return null;
        NioBuffer buf = BufferOfPage(page);
        int p = OffsetOfPage(page);
        int header = buf.GetInt(p);
        int len = header & PAYLOAD_SIZE_MASK;
        int flags = header & ~PAYLOAD_SIZE_MASK;
        if (len == 0 || len > (1 << 30) - 4)
        {
            Error(AbsPosOfPage(page),
                "Blob at page %d has illegal payload length (%d)", page, len);
            return null;
        }
        int lenPages = (len + pageSize + 3) / pageSize;
        if ((header & FREE_BLOB_FLAG) != 0)
        {
            if ((len + 4) % pageSize == 0) flags |= VALID_FREE_BLOB_SIZE;
            int lastPage = page + lenPages - 1;
            NioBuffer lastBuf = BufferOfPage(lastPage);
            int pTrailer = OffsetOfPage(lastPage) + pageSize - 4;
            if (lastBuf.GetInt(pTrailer) == lenPages) flags |= VALID_FREE_BLOB_TRAILER;
        }
        Blob blob = new Blob(page, lenPages, flags);
        blobs[page] = blob;
        return blob;
    }

    public void ReportErrors(TextWriter @out)
    {
        errors.Sort();
        foreach (ErrorEntry error in errors) @out.WriteLine(error);
    }

    public bool HasErrors()
    {
        return errors.Count != 0;
    }
}
