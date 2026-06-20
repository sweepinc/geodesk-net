/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;

using GeoDesk.Buffers;
using GeoDesk.Common.Store.Format;
using GeoDesk.Common.Util;

using static GeoDesk.Common.Store.BlobStoreConstants;

namespace GeoDesk.Common.Store;

/// <summary>
/// A class that verifies the integrity of a BlobStore.
/// </summary>
/// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker</c>.</remarks>
internal class BlobStoreChecker
{

    /// <summary>
    /// The bits tracked in <see cref="Blob.flags"/> during a check: the checker's own progress flags
    /// (low bits) plus the on-disk free markers carried over from <see cref="BlobHeader.Flags"/>
    /// (bits 30-31). The two groups occupy disjoint bit ranges, so they coexist in one value.
    /// </summary>
    [Flags]
    internal enum BlobFlags
    {
        None = 0,

        /// <summary>A live blob was seen referencing this one (so it must not be free).</summary>
        Referenced = 1,

        /// <summary>This free blob has already been visited in a free-list chain (cycle detection).</summary>
        FreeReferenced = 2,

        /// <summary>The free blob's payload size is page-aligned, as required.</summary>
        ValidFreeSize = 4,

        /// <summary>The free blob's trailer (its last word) holds the correct page count.</summary>
        ValidFreeTrailer = 8,

        /// <summary>On-disk marker: the preceding blob in the file is free (bit 30).</summary>
        PrecedingFree = PRECEDING_BLOB_FREE_FLAG,

        /// <summary>On-disk marker: this blob is free (bit 31).</summary>
        Free = FREE_BLOB_FLAG,
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Blob</c>.</remarks>
    public class Blob : IComparable<Blob>
    {

        internal int firstPage;
        internal int pages;
        internal BlobFlags flags;

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Blob(int, int, int)</c>.</remarks>
        internal Blob(int firstPage, int pages, BlobFlags flags)
        {
            this.firstPage = firstPage;
            this.pages = pages;
            this.flags = flags;
        }

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Blob.isReferenced()</c>.</remarks>
        internal bool IsReferenced => (flags & BlobFlags.Referenced) != 0;

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Blob.isFree()</c>.</remarks>
        internal bool IsFree => (flags & BlobFlags.Free) != 0;

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Blob.hasFlags(int)</c>.</remarks>
        internal bool HasFlags(BlobFlags flags)
        {
            return (this.flags & flags) == flags;
        }

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Blob.compareTo(Blob)</c>.</remarks>
        public int CompareTo(Blob? other)
        {
            return firstPage.CompareTo(other!.firstPage);
        }

    }

    protected readonly BlobStore store;

    long _fileSize;
    int _totalPages;
    int _pageSizeShift;
    int _pageSize;
    int _pagesPerSegment;
    int _metadataSize;
    readonly Dictionary<int, Blob> _blobs = new Dictionary<int, Blob>();
    readonly List<ErrorEntry> _errors = new List<ErrorEntry>();

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker(BlobStore)</c>.</remarks>
    public BlobStoreChecker(BlobStore store)
    {
        this.store = store;
        try
        {
            _fileSize = store.CurrentFileSize();
        }
        catch (IOException ex)
        {
            Error(0, ex.Message);
        }

        var seg = store.GetSegment(0);
        _totalPages = seg.Memory.Span.GetIntLE(TOTAL_PAGES_OFS);
        _metadataSize = seg.Memory.Span.GetIntLE(METADATA_SIZE_OFS);
        _pageSizeShift = 12;
        _pageSize = 1 << _pageSizeShift;
        _pagesPerSegment = (1 << 30) / _pageSize;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.check()</c>.</remarks>
    public void Check()
    {
        CheckMetadata();
        CheckFreeTables();
        CheckIndex();
        CheckBlobs();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkMetadata()</c>.</remarks>
    protected void CheckMetadata()
    {
        if (_metadataSize < DEFAULT_METADATA_SIZE || _metadataSize > (1 << 30))
        {
            Error(METADATA_SIZE_OFS, "Invalid metadata size: %d", _metadataSize);
            return;
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkIndex()</c>.</remarks>
    protected virtual void CheckIndex()
    {
        // TODO: abstract?
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkBlobs()</c>.</remarks>
    protected void CheckBlobs()
    {
        var blobList = new List<Blob>(_blobs.Values);
        blobList.Sort();
        var metadataPages = (_metadataSize + _pageSize - 1) / _pageSize;
        var nextPage = metadataPages;
        Blob? prevBlob = null;
        foreach (var blob in blobList)
        {
            var gapsOrOverlaps = false;
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
            if (blob.IsFree)
            {
                if (blob.HasFlags(BlobFlags.Referenced))
                {
                    Error(blob, " is marked free but is still in use");
                }
                else
                {
                    if (!blob.HasFlags(BlobFlags.ValidFreeSize))
                    {
                        Error(blob, ": Invalid size for free blob");
                    }
                    if (!blob.HasFlags(BlobFlags.ValidFreeTrailer))
                    {
                        Error(blob, ": Invalid free-blob trailer");
                    }
                }
            }
            var blobStartsAtSegment = AbsPosOfPage(blob.firstPage) % (1 << 30) == 0;
            if (prevBlob != null && !gapsOrOverlaps)
            {
                if (prevBlob.IsFree)
                {
                    if (blob.IsFree && !blobStartsAtSegment)
                    {
                        Error(blob, " should have been consolidated with " + "previous free blob");
                    }
                    if (!blob.HasFlags(BlobFlags.PrecedingFree))
                    {
                        Error(blob, ": Preceding blob is free, " + "but prev_blob_free flag not set");
                    }
                }
                else
                {
                    if (blob.HasFlags(BlobFlags.PrecedingFree))
                    {
                        Error(blob, ": Preceding blob in use, " + "but prev_blob_free flag set");
                    }
                }
            }
            nextPage = blob.firstPage + blob.pages;
            prevBlob = blob;
        }

        if (nextPage == _totalPages)
            return;

        if (nextPage > _totalPages)
        {
            Error(TOTAL_PAGES_OFS, "total_pages should be %d instead of %d", nextPage, _totalPages);
            return;
        }

        CheckUnreferenced(nextPage, _totalPages);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkUnreferenced(int, int)</c>.</remarks>
    void CheckUnreferenced(int start, int end)
    {
        Error(AbsPosOfPage(start), "%d page%s of unreferenced data", end - start, end - start == 1 ? "" : "s");
    }

    // We compute the segment ourselves (rather than delegating to BlobStore) to give us more
    // flexibility later in addressing corrupt page size settings. The caller wraps its Memory as needed.
    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.bufferOfPage(int)</c>.</remarks>
    protected Segment SegmentOfPage(int page)
    {
        return store.GetSegment(page / _pagesPerSegment);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.offsetOfPage(int)</c>.</remarks>
    protected int OffsetOfPage(int page)
    {
        return (page % _pagesPerSegment) * _pageSize;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.absPosOfPage(int)</c>.</remarks>
    protected long AbsPosOfPage(int page)
    {
        return (long)page * _pageSize;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkFreeTables()</c>.</remarks>
    void CheckFreeTables()
    {
        var header = new StoreHeader(SegmentOfPage(0).Memory);
        var rangesUsed = 0;

        for (var slot = 0; slot < 512; slot++)
        {
            var freePage = header.TrunkFreeTablePage(slot);
            if (freePage.IsNil)
                continue;

            rangesUsed |= 1 << (slot >> 4);

            var freeBlob = GetValidBlob(TRUNK_FREE_TABLE_OFS + slot * 4, freePage.Value);
            if (freeBlob == null)
                continue;

            if (!CheckBlobIsFree(freeBlob))
                continue;

            if (!freeBlob.HasFlags(BlobFlags.ValidFreeSize | BlobFlags.ValidFreeTrailer))
                continue;

            CheckLeafFreeTable(slot, freeBlob);
        }

        CheckExpectedVsActual(TRUNK_FT_RANGE_BITS_OFS, "trunk_free_range_mask", rangesUsed, header.TrunkRangeBits);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkExpectedVsActual(long, String, int, int)</c>.</remarks>
    void CheckExpectedVsActual(long ofs, string what, int expected, int actual)
    {
        if (expected != actual)
            Error(ofs, "%s should be %08X instead of %08X", what, expected, actual);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkBlobIsFree(Blob)</c>.</remarks>
    bool CheckBlobIsFree(Blob blob)
    {
        if (!blob.HasFlags(BlobFlags.Free))
        {
            Error(blob, " should be free");
            return false;
        }
        return true;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkLeafFreeTable(int, Blob)</c>.</remarks>
    void CheckLeafFreeTable(int trunkSlot, Blob blob)
    {
        var minPages = (trunkSlot * 512) + 1;
        var maxPages = minPages + 511;

        var page = blob.firstPage;
        var ofs = AbsPosOfPage(page);
        if (blob.pages < minPages || blob.pages > maxPages)
        {
            Error(ofs, "Free blob with %d pages in wrong size range (%d to %d)",
                blob.pages, minPages, maxPages);
            return;
        }

        var pageOfs = OffsetOfPage(page);
        var freeBlobCursor = new FreeBlob(SegmentOfPage(page).Memory.Slice(pageOfs));
        var rangeMask = freeBlobCursor.LeafRangeBits;
        var rangesUsed = 0;
        for (var leafSlot = 0; leafSlot < 512; leafSlot++)
        {
            var freePage = freeBlobCursor.LeafFreeTablePage(leafSlot);
            if (freePage.IsNil)
                continue;
            rangesUsed |= 1 << (leafSlot >> 4);
            var freeBlob = GetValidBlob(ofs + pageOfs + LEAF_FREE_TABLE_OFS + leafSlot * 4, freePage.Value);
            if (freeBlob != null)
                CheckFreeBlobChain(trunkSlot, leafSlot, freeBlob);
        }

        CheckExpectedVsActual(ofs + LEAF_FREE_TABLE_OFS, "leaf_free_range_mask",
            rangesUsed, rangeMask);

        if (rangesUsed == 0)
            Error(blob, "Leaf free-table must have at least one entry");
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.checkFreeBlobChain(int, int, Blob)</c>.</remarks>
    void CheckFreeBlobChain(int trunkSlot, int leafSlot, Blob blob)
    {
        var len = trunkSlot * 512 + leafSlot + 1;
        var prevFreePage = 0;

        for (; ; )
        {
            blob.flags |= BlobFlags.FreeReferenced;
            if (!CheckBlobIsFree(blob))
                return;

            var ofs = AbsPosOfPage(blob.firstPage);
            if (blob.pages != len)
                Error(ofs, "Blob with %d pages listed in freetable for page size %d", blob.pages, len);

            var pageOfs = OffsetOfPage(blob.firstPage);
            var freeBlobCursor = new FreeBlob(SegmentOfPage(blob.firstPage).Memory.Slice(pageOfs));
            var ofsPrev = ofs + pageOfs + PREV_FREE_BLOB_OFS;
            var ofsNext = ofs + pageOfs + NEXT_FREE_BLOB_OFS;
            var prev = freeBlobCursor.PrevPage;
            var next = freeBlobCursor.NextPage;

            if (prev.Value != prevFreePage)
                Error(ofsPrev, "prev_free_blob should be %d, not %d", prevFreePage, prev.Value);

            if (next.IsNil)
                return;

            prevFreePage = blob.firstPage;

            var nextBlob = GetValidBlob(ofsNext, next.Value);
            if (nextBlob == null)
                return;

            if (!CheckBlobIsFree(nextBlob))
                return;

            if (nextBlob.HasFlags(BlobFlags.FreeReferenced))
            {
                Error(ofsNext, "Circular reference in free-blob list (to %d)", next);
                return;
            }

            blob = nextBlob;
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Error</c>.</remarks>
    sealed class ErrorEntry : IComparable<ErrorEntry>
    {

        internal readonly long location;
        internal readonly string message;

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Error(long, String)</c>.</remarks>
        internal ErrorEntry(long location, string message)
        {
            this.location = location;
            this.message = message;
        }

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Error.compareTo(Error)</c>.</remarks>
        public int CompareTo(ErrorEntry? other)
        {
            return location.CompareTo(other!.location);
        }

        /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.Error.toString()</c>.</remarks>
        public override string ToString()
        {
            return JavaFormat.Format("%08X: %s", location, message);
        }

    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.error(long, String, Object...)</c>.</remarks>
    protected void Error(long ofs, string msg, params object?[] args)
    {
        _errors.Add(new ErrorEntry(ofs, JavaFormat.Format(msg, args)));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.error(Blob, String, Object...)</c>.</remarks>
    protected void Error(Blob blob, string msg, params object?[] args)
    {
        _errors.Add(new ErrorEntry(AbsPosOfPage(blob.firstPage), JavaFormat.Format("Blob " + blob.firstPage + msg, args)));
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.getValidBlob(long, int)</c>.</remarks>
    Blob? GetValidBlob(long ofs, int page)
    {
        var blob = GetBlob(page);
        if (blob == null)
            Error(ofs, "Bad blob reference: %d", page);

        return blob;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.useBlob(long, int)</c>.</remarks>
    public Blob? UseBlob(long ofs, int page)
    {
        var blob = GetValidBlob(ofs, page);
        if (blob != null)
            blob.flags |= BlobFlags.Referenced;

        return blob;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.getBlob(int)</c>.</remarks>
    Blob? GetBlob(int page)
    {
        if (_blobs.TryGetValue(page, out var existing))
            return existing;

        if (page <= 0)
            return null;

        if ((page << _pageSizeShift) >= _fileSize)
            return null;

        var header = new BlobHeader(SegmentOfPage(page).Memory.Slice(OffsetOfPage(page)));
        var len = header.PayloadSize;
        var flags = (BlobFlags)header.Flags;
        if (len == 0 || len > (1 << 30) - 4)
        {
            Error(AbsPosOfPage(page), "Blob at page %d has illegal payload length (%d)", page, len);
            return null;
        }

        var lenPages = (len + _pageSize + 3) / _pageSize;
        if (header.IsFree)
        {
            if ((len + 4) % _pageSize == 0)
                flags |= BlobFlags.ValidFreeSize;

            var lastPage = page + lenPages - 1;
            var pTrailer = OffsetOfPage(lastPage) + _pageSize - 4;
            if (SegmentOfPage(lastPage).Memory.Span.GetIntLE(pTrailer) == lenPages)
                flags |= BlobFlags.ValidFreeTrailer;
        }

        var blob = new Blob(page, lenPages, flags);
        _blobs[page] = blob;
        return blob;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.reportErrors(PrintStream)</c>.</remarks>
    public void ReportErrors(TextWriter @out)
    {
        _errors.Sort();
        foreach (var error in _errors)
            @out.WriteLine(error);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.BlobStoreChecker.hasErrors()</c>.</remarks>
    public bool HasErrors()
    {
        return _errors.Count != 0;
    }

}
