/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading;

using GeoDesk.Buffers;
using GeoDesk.Common.Util;

using static GeoDesk.Common.Store.BlobStoreConstants;

namespace GeoDesk.Common.Store;

// TODO: We cannot mark a ticket as "completed" until the transaction has been comitted!

/// <summary>
/// Fetches missing blobs (tiles and the store metadata) from a remote repository over HTTP on a single
/// background thread, inflating each one and writing it into the backing <see cref="BlobStore"/> within a
/// transaction. Callers enqueue work via <see cref="Request"/>, which returns a <see cref="Ticket"/> they
/// can await or attach a completion callback to; identical concurrent requests share one ticket.
/// </summary>
/// <remarks>
/// Ported from Java <c>com.clarisma.common.store.Downloader</c>. The Java original fetches blobs
/// over HTTP via <c>java.net.URLConnection</c> and inflates them with <c>InflaterInputStream</c>;
/// this port uses <see cref="System.Net.Http.HttpClient"/> and <see cref="ZLibStream"/>. The Java
/// <c>DownloadThread</c> (a subclass of <c>Thread</c>) becomes a plain background
/// <see cref="System.Threading.Thread"/>, and Java's <c>synchronized</c> / <c>wait</c> /
/// <c>notifyAll</c> become <see cref="Monitor"/> on a private lock object.
/// </remarks>
internal class Downloader
{

    static readonly HttpClient HttpClient = new HttpClient();

    public const int METADATA_ID = -1;

    const int DORMANT = 0;
    const int READY = 1;
    const int SHUTDOWN = 2;

    const int BUFFER_SIZE = 4096;

    readonly BlobStore store;
    readonly string _baseUrl;
    readonly Queue<Ticket> _ticketQueue;
    readonly Dictionary<int, Ticket> _ticketMap;
    readonly object _mutex = new object();
    int _status = READY;     // TODO: start as dormant
    Exception? _repositoryError;
    Thread? _thread;
    readonly int _maxPendingTickets = 16;
    readonly int _maxKeepAlive = 60_000;
    readonly int _retryAttempts = 3;
    readonly int _retryDelay = 500;
    readonly bool _progressiveDelay = true;

    /// <summary>
    /// Creates a downloader that writes into <paramref name="store"/> and fetches blobs relative to
    /// <paramref name="baseUrl"/> (a trailing slash is appended if missing).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader(BlobStore, String)</c>.</remarks>
    public Downloader(BlobStore store, string baseUrl)
    {
        this.store = store;
        _baseUrl = baseUrl.EndsWith("/", StringComparison.Ordinal) ? baseUrl : (baseUrl + '/');
        _ticketQueue = new Queue<Ticket>();
        _ticketMap = new Dictionary<int, Ticket>();
    }

    /// <summary>
    /// A Ticket represents an order to download a specific blob (or the meta-blob, if id == 0).
    /// Consumers of the blob may either explicitly wait for the Ticket to be completed (via
    /// <see cref="AwaitCompletion"/>) or request a callback via an <see cref="Action{Ticket}"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.Ticket</c>.</remarks>
    public sealed class Ticket
    {

        internal readonly int Id;
        bool _completed;
        int _page;
        Exception? _error;
        internal readonly List<Action<Ticket>> Consumers = new List<Action<Ticket>>();
        readonly object _mutex = new object();

        /// <summary>
        /// Creates a ticket tracking the download of the blob with the given id.
        /// </summary>
        internal Ticket(int id)
        {
            Id = id;
        }

        /// <summary>
        /// Returns the store page at which the downloaded blob was written, valid once the ticket has
        /// completed successfully.
        /// </summary>
        /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.Ticket.page()</c>.</remarks>
        public int Page()
        {
            return _page;
        }

        /// <summary>
        /// Marks the ticket complete with its resulting page and any error, notifies all registered
        /// consumers, and wakes any threads blocked in <see cref="AwaitCompletion"/>. Must not be called
        /// directly; it is invoked by <see cref="Downloader.TicketCompleted"/>.
        /// </summary>
        /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.Ticket.complete(int, Throwable)</c>.</remarks>
        internal void Complete(int page, Exception? error)
        {
            lock (_mutex)
            {
                _page = page;
                _error = error;
                _completed = true;
                foreach (var c in Consumers)
                    c(this);
                Monitor.PulseAll(_mutex);
            }
        }

        /// <summary>
        /// Blocks the calling thread until this ticket has been completed.
        /// </summary>
        /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.Ticket.awaitCompletion()</c>.</remarks>
        public void AwaitCompletion()
        {
            lock (_mutex)
            {
                for (; ; )
                {
                    if (_completed)
                        return;
                    Monitor.Wait(_mutex);
                }
            }
        }

        /// <summary>
        /// Rethrows the error that caused the download to fail, if any; a <see cref="StoreException"/> is
        /// rethrown as-is and any other exception is wrapped in one. The ticket must already be completed.
        /// </summary>
        /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.Ticket.throwError()</c>.</remarks>
        public void ThrowError()
        {
            lock (_mutex)
            {
                Debug.Assert(_completed);
                if (_error != null)
                {
                    // Java rethrows RuntimeExceptions directly and wraps everything else in a
                    // StoreException; here a StoreException (the unchecked-equivalent we raise) is
                    // rethrown directly, and other exceptions (e.g. IOException) are wrapped.
                    if (_error is StoreException)
                        throw _error;
                    throw new StoreException("Download failed: " + _error.Message, _error);
                }
            }
        }
    }

    /// <summary>
    /// Requests the blob with the given <paramref name="id"/> (or the metadata blob for
    /// <see cref="METADATA_ID"/>), returning a <see cref="Ticket"/> tracking the download. An existing
    /// pending ticket for the same id is reused. The optional <paramref name="consumer"/> is invoked when
    /// the ticket completes. Starts the background download thread on first use and blocks while the pending
    /// queue is full.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.request(int, Consumer)</c>.</remarks>
    public Ticket Request(int id, Action<Ticket>? consumer)
    {
        lock (_mutex)
        {
            if (!_ticketMap.TryGetValue(id, out Ticket? ticket))
            {
                while (_ticketMap.Count == _maxPendingTickets)
                    Monitor.Wait(_mutex);

                ticket = new Ticket(id);
                if (_status == SHUTDOWN)
                {
                    _repositoryError ??= new StoreException(
                        "Ticket refused because Downloader has been shut down", (Exception?)null);
                    ticket.Complete(0, _repositoryError);
                    return ticket;
                }
                _ticketMap[id] = ticket;
                _ticketQueue.Enqueue(ticket);
                Monitor.PulseAll(_mutex);
            }
            if (consumer != null)
                ticket.Consumers.Add(consumer);
            if (_thread == null)
            {
                _thread = new Thread(DownloadThreadRun) { IsBackground = true, Name = "Downloader" };
                _thread.Start();
            }
            return ticket;
        }
    }

    /// <summary>
    /// Marks the downloader as shut down (so further requests are refused) and blocks until the background
    /// thread has stopped, interrupting it as needed.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.shutdown()</c>.</remarks>
    public void Shutdown()
    {
        lock (_mutex)
        {
            _status = SHUTDOWN;

            while (_thread != null)
            {
                _thread.Interrupt();
                try
                {
                    Monitor.Wait(_mutex);
                }
                catch (ThreadInterruptedException)
                {
                    // TODO: do nothing?
                }
            }
        }
    }

    /// <summary>
    /// Removes the ticket from the pending map, completes it with its page and error, and wakes any waiters
    /// (including requesters blocked on a full queue).
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.ticketCompleted(Ticket, int, Throwable)</c>.</remarks>
    void TicketCompleted(Ticket ticket, int page, Exception? error)
    {
        lock (_mutex)
        {
            _ticketMap.Remove(ticket.Id);
            ticket.Complete(page, error);
            Monitor.PulseAll(_mutex);
        }
    }

    /// <summary>
    /// Clears the reference to the background thread and wakes any waiters once the thread has finished.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.threadEnded()</c>.</remarks>
    void ThreadEnded()
    {
        lock (_mutex)
        {
            _thread = null;
            Monitor.PulseAll(_mutex);
        }
    }

    /// <summary>
    /// Fails every outstanding ticket with the given exception and clears the work queue, typically after
    /// the download thread has aborted.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.cancelTickets(Throwable)</c>.</remarks>
    void CancelTickets(Exception ex)
    {
        lock (_mutex)
        {
            // make a copy because Complete() modifies ticketMap
            var remainingTickets = new List<Ticket>(_ticketMap.Values);
            foreach (var ticket in remainingTickets)
                TicketCompleted(ticket, 0, ex);
            Debug.Assert(_ticketMap.Count == 0);
            _ticketQueue.Clear();
            _thread = null;      // TODO: this may be problematic!
                                 // but if commented out, we deadlock
        }
    }

    /// <summary>
    /// Dequeues the next pending ticket. When <paramref name="wait"/> is true and the queue is empty, waits
    /// up to the keep-alive timeout for more work, clearing the thread reference if none arrives. Returns
    /// null when there is nothing to do.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.takeTicket(boolean)</c>.</remarks>
    Ticket? TakeTicket(bool wait)
    {
        lock (_mutex)
        {
            Ticket? ticket = _ticketQueue.Count > 0 ? _ticketQueue.Dequeue() : null;
            if (ticket != null)
                return ticket;
            if (!wait)
                return null;
            Monitor.Wait(_mutex, _maxKeepAlive);
            ticket = _ticketQueue.Count > 0 ? _ticketQueue.Dequeue() : null;
            if (ticket == null)
                _thread = null;
            return ticket;
        }
    }

    /// <summary>
    /// Builds the repository URL for the blob with the given id: <c>meta.tile</c> for the metadata blob, or
    /// a hierarchical <c>XXX/YYY.tile</c> path derived from the tile number otherwise.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.urlOf(int)</c>.</remarks>
    protected Uri UrlOf(int id)
    {
        if (id == METADATA_ID)
            return new Uri(_baseUrl + "meta.tile");
        return new Uri(string.Format(CultureInfo.InvariantCulture, "{0}{1:X3}/{2:X3}.tile",
            _baseUrl, (int)((uint)id >> 12), id & 0xfff));
    }

    // TODO: don't complete tickets here
    /// <summary>
    /// Resolves a ticket to a store page: returns the existing page if the blob is already present (or, for
    /// metadata, if the store is non-empty), otherwise downloads it from its URL, records the new index
    /// entry for tile blobs, and returns the page it was written to.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.download(Ticket)</c>.</remarks>
    protected int Download(Ticket ticket)
    {
        int id = ticket.Id;
        if (id == METADATA_ID)
        {
            if (!store.IsEmpty())
                return 0;
        }
        else
        {
            int existingPage = store.GetIndexEntry(id);
            if (existingPage != 0)
                return existingPage;
        }

        Uri url = UrlOf(id);

        // TODO: retry

        int page = Download(id, url);
        if (id != METADATA_ID)
            store.SetIndexEntry(id, page);
        return page;
    }

    /// <summary>
    /// Throws a <see cref="StoreException"/> with the given reason, used to abort a download when the tile
    /// data is malformed or incompatible.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.invalidTileFile(String)</c>.</remarks>
    [DoesNotReturn]
    void InvalidTileFile(string reason)
    {
        throw new StoreException(reason, (Exception?)null);
    }

    /// <summary>
    /// Downloads and inflates the blob at the given URL, validating its exported-tile header (magic number
    /// and, for tile blobs, the store GUID), allocating store space, and writing the payload into the store.
    /// First and last blocks are journaled (and, for metadata, only the first block) while interior blocks
    /// are written directly. Returns the first store page of the written blob.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.download(int, URL)</c>.</remarks>
    protected int Download(int id, Uri url)
    {
        // TODO: checksums

        byte[] buf = new byte[BUFFER_SIZE];
        int firstPage;

        using Stream input = HttpClient.GetStreamAsync(url).GetAwaiter().GetResult();

        if (ReadFully(input, buf, 0, EXPORTED_HEADER_LEN) != EXPORTED_HEADER_LEN)
        {
            InvalidTileFile("Invalid tile: Truncated header");
        }
        int magic = Bytes.GetInt(buf, 0);
        if (magic != EXPORTED_MAGIC)
        {
            InvalidTileFile(string.Format(CultureInfo.InvariantCulture,
                "Invalid tile: Wrong file type ({0:X8})", magic));
        }
        if (id != METADATA_ID)
        {
            // We don't check GUID for meta-tile because the store will be empty at that point.
            // The store GUID and the tile header GUID are both stored in Java UUID (big-endian)
            // layout, so both parse to the same Guid value the exporter assigned.
            var tileGuid = new Guid(buf.AsSpan(EXPORTED_HEADER_GUID, GUID_LEN), bigEndian: true);
            if (store.GetGuid() != tileGuid)
            {
                InvalidTileFile("Incompatible tile: " +
                    Convert.ToHexString(buf, EXPORTED_HEADER_GUID, GUID_LEN));
            }
        }

        int uncompressedSize = Bytes.GetInt(buf, EXPORTED_ORIGINAL_LEN_OFS);
        int payloadSize = uncompressedSize + 4;     // one word is the checksum
        if (payloadSize <= 0 || payloadSize > (1 << 30) - 4)
        {
            InvalidTileFile("Invalid tile: Uncompressed size invalid");
        }

        using Stream zipIn = new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true);
        var checksum = new Crc32();

        if (id == METADATA_ID)
        {
            // For the metadata, we only journal the first block. Need to enforce requirement that
            // metadata can only be updated in an empty store.

            firstPage = 0;
            int firstBlockLen = System.Math.Min(uncompressedSize, BLOCK_LEN);
            var rootBlock = store.GetBlockOfPage(0);
            Read(zipIn, buf, rootBlock, 0, firstBlockLen, checksum);
            if (uncompressedSize > BLOCK_LEN)
                Read(zipIn, buf, new NioBufferWriter(store.SegmentOfPage(0).Memory), BLOCK_LEN, uncompressedSize - BLOCK_LEN, checksum);

            // TODO: adjust page size

            // Set total number of pages based on the metadata length (This cannot be included as
            // part of the downloaded metadata itself, since it varies based on the page size)
            int metadataSize = rootBlock.GetInt(METADATA_SIZE_OFS);
            rootBlock.PutInt(TOTAL_PAGES_OFS, store.BytesToPages(metadataSize));
        }
        else
        {
            // For blobs, we have to journal the first block, because we need to journal the header
            // data of a previously freed block. If the blob's payload overwrites the free-blob tail,
            // we need to journal the last block as well. All the other blocks in-between can be
            // written directly into the store, since these areas by definition only contain garbage.

            // TODO: must enforce that we never allocate the space of a blob that is freed within
            //  the same transaction!

            int headerLen = 8;  // blob header word + checksum word
            firstPage = store.AllocateBlob(payloadSize);
            int p = store.OffsetOfPage(firstPage);
            int firstBlockLen = System.Math.Min(uncompressedSize, BLOCK_LEN - headerLen);
            Read(zipIn, buf, store.GetBlockOfPage(firstPage), headerLen, firstBlockLen, checksum);
            // we don't use p here, because the block buffer uses relative addressing
            if (uncompressedSize > firstBlockLen)
            {
                // Blob is longer than one block (can still be single-page)

                int pages = store.PagesForPayloadSize(payloadSize);

                // We can't use store.OffsetOfPage(firstPage + pages) because it returns 0 if blob
                // sits at end of 1-GB segment; that's why we calculate explicitly
                int pTail = p + (pages << store.pageSizeShift) - FREE_BLOB_TRAILER_LEN;
                int pPayloadEnd = p + uncompressedSize + headerLen;
                var blobBuf = new NioBufferWriter(store.SegmentOfPage(firstPage).Memory);
                int pUnprotectedStart = p + BLOCK_LEN;
                if (pPayloadEnd > pTail)
                {
                    int pUnprotectedEnd = pTail & unchecked((int)0xffff_f000); // TODO: assumes 4096 block len
                    int unprotectedLen = pUnprotectedEnd - pUnprotectedStart;
                    if (unprotectedLen > 0)
                    {
                        Read(zipIn, buf, blobBuf, pUnprotectedStart, unprotectedLen, checksum);
                    }
                    long absoluteTailBlockPos = store.AbsoluteOffsetOfPage(firstPage)
                        + pUnprotectedEnd - p;
                    var tailBlock = store.GetBlock(absoluteTailBlockPos);
                    int tailBlockLen = pPayloadEnd - pUnprotectedEnd;
                    Debug.Assert(tailBlockLen > 0);
                    Debug.Assert(tailBlockLen <= 4096);
                    Read(zipIn, buf, tailBlock, 0, tailBlockLen, checksum);
                }
                else
                {
                    Read(zipIn, buf, blobBuf, pUnprotectedStart,
                        pPayloadEnd - pUnprotectedStart, checksum);
                }
            }
        }

        // TODO: the InflaterInputStream already read the CRC32 into its own buffer; the checksum
        //  comparison is disabled for now (see the Java original), so the computed checksum is
        //  currently unused.

        return firstPage;
    }

    /// <summary>
    /// Reads compressed data from a stream and writes it into a target buffer.
    /// </summary>
    /// <remarks>
    /// Ported from Java <c>com.clarisma.common.store.Downloader.read(InflaterInputStream, byte[],
    /// ByteBuffer, int, int, Checksum)</c>. Note .NET <see cref="Stream.Read(byte[], int, int)"/>
    /// returns 0 (not -1) at end of stream.
    /// </remarks>
    void Read(Stream zipIn, byte[] buf, NioBufferWriter target, int p, int len, Crc32 checksum)
    {
        while (len > 0)
        {
            int chunkLen = System.Math.Min(len, buf.Length);
            int bytesRead = zipIn.Read(buf, 0, chunkLen);
            if (bytesRead <= 0)
                throw new IOException("Unexpected end of compressed data");
            target.Put(p, buf, 0, bytesRead);
            checksum.Update(buf, 0, bytesRead);
            p += bytesRead;
            len -= bytesRead;
        }
    }

    /// <summary>
    /// Reads exactly <paramref name="len"/> bytes (or until end of stream) from <paramref name="s"/> into
    /// <paramref name="buf"/> at the given offset, mirroring the single <c>InputStream.read(b, off, len)</c>
    /// the Java original performs to load the tile header. Returns the number of bytes actually read.
    /// </summary>
    static int ReadFully(Stream s, byte[] buf, int off, int len)
    {
        int total = 0;
        while (total < len)
        {
            int n = s.Read(buf, off + total, len - total);
            if (n <= 0)
                break;
            total += n;
        }
        return total;
    }

    /// <summary>
    /// The background thread loop: repeatedly takes a ticket, opens an append transaction, downloads and
    /// commits each available ticket within it, then ends the transaction. On any failure it cancels all
    /// outstanding tickets and tears down the transaction before the thread exits.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Downloader.DownloadThread.run()</c>.</remarks>
    void DownloadThreadRun()
    {
        try
        {
            for (; ; )
            {
                Ticket? ticket = TakeTicket(true);
                if (ticket == null)
                    break;
                store.BeginTransaction(Store.LOCK_APPEND);
                for (; ; )
                {
                    Exception? error = null;
                    int page = -1;
                    try
                    {
                        page = Download(ticket);
                        store.Commit();
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }

                    // TODO: should we wait commit in batches instead, and notify all completed
                    //  tickets at end of transaction?

                    TicketCompleted(ticket, page, error);
                    ticket = TakeTicket(false);
                    if (ticket == null)
                        break;
                }
                store.EndTransaction();
            }
        }
        catch (Exception ex)
        {
            CancelTickets(ex);
            try
            {
                store.EndTransaction();
            }
            catch (Exception)
            {
                // doesn't matter at this point, we're shutting down because of an exception
            }
        }
        ThreadEnded();
    }

    /// <summary>
    /// Mirrors <c>java.util.zip.CRC32</c> (used by the Java original's read() helper). The downloaded
    /// payload's checksum verification is currently disabled (see <see cref="Download(int, Uri)"/>), so the
    /// computed value is currently unused; it is retained to keep the port structurally faithful.
    /// </summary>
    sealed class Crc32
    {

        static readonly uint[] Table = BuildTable();

        uint _crc = 0xffff_ffff;

        /// <summary>
        /// Builds the 256-entry CRC-32 lookup table for the IEEE polynomial.
        /// </summary>
        static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xedb8_8320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        /// <summary>
        /// Updates the running checksum with <paramref name="length"/> bytes from <paramref name="buf"/>
        /// starting at <paramref name="offset"/>.
        /// </summary>
        public void Update(byte[] buf, int offset, int length)
        {
            uint c = _crc;
            for (int i = 0; i < length; i++)
                c = Table[(c ^ buf[offset + i]) & 0xff] ^ (c >> 8);
            _crc = c;
        }

        /// <summary>
        /// The finalized CRC-32 checksum value computed so far.
        /// </summary>
        public long Value => _crc ^ 0xffff_ffff;
    }
}
