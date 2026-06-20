/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Hashing;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

using GeoDesk.Buffers;
using GeoDesk.Common.Util;
using GeoDesk.Extensions;

using ByteOrder = Java.Nio.ByteOrder;
using NioBuffer = Java.Nio.ByteBuffer;

namespace GeoDesk.Common.Store;

/// <summary>
/// Base class for persistent data stores that supports transactions and
/// journaling. A Store is backed by a sparse file that is memory-mapped
/// in segments, 1 GB each.
///
/// All updates to a Store are journaled, to prevent corruption of the Store's
/// contents in the event updates are only partially written due to abnormal
/// process termination or power loss.
///
/// Stores are designed to be shared among processes. Multiple processes can
/// read existing data, while one process may add new data. Modifying existing
/// data requires exclusive access by one single process. Access is mediated
/// via file locks. File locks are treated as advisory. The distinction between
/// "new data" and "existing data" is left to the subclass.
///
/// Adding and updating data must be done within transactions. Multiple threads
/// may read data, but only one thread is allowed to write. Note that the Store
/// base class does not enforce access rules, it only provides the building
/// blocks for transactional semantics and journaling. It is up to the concrete
/// subclasses to ensure proper concurrency.
///
/// The Store base class imposes no format for the contents of the store,
/// except for the following:
///
/// - Write access that requires journaling must be performed via 4-KB blocks
///
/// - Contiguous chunks of data must not cross 1-GB segment boundaries
///
/// - The first 8 bytes of the store file must be metadata, or which the first
///   4 bytes must be the type indicator ("magic number") which cannot be zero
///   (zero indicates an uninitialized file). The other 4 bytes typically
///   store the version number of the file format.
///
/// - The store file must record its creation time (as an 8-byte timestamp).
///   (It is recommended not to rely on the filesystem-provided metadata,
///   but to record this timestamp in the store file itself)
///
/// - Store data assumes little-endian byte order
///
/// Modifications to a Store follow this pattern:
///
/// - beginTransaction()  -- either APPEND or EXCLUSIVE
///
/// - One or more calls to getBlock() to obtain a 4-KB block at a specific
///   address. This block is represented as a ByteBuffer, which can then
///   be modified. When changes are committed, the original contents of these
///   blocks are first written to the journal, so the store file can be restored
///   to its original state if the transaction fails to complete. Once the
///   journal has been safely stored on disk, the changes are written to the
///   store file itself.
///
/// - commit() or rollback() to apply or discard all changes since the last
///   call to beginTransaction(), commit() or rollback()
///
/// - endTransaction()
///
/// For performance reasons, changes may be written directly into the data
/// store's buffers (without obtaining staging buffers via getBlock() ) but
/// only for sections of the store file that are impervious to undefined data
/// due to partially-performed writes (for example, the inner blocks of
/// a previously freed blob in a BlobStore, since these blocks by definition
/// contain garbage). However, at least one block in the same segment must
/// have been obtained via getBlock(), and its contents must be actually
/// modified, in order for the segment to be properly synched by a subsequent
/// call to commit().
///
/// Data that is appended to the end of a file does not require journaling;
/// it can be written directly into the buffers (the above caveat regarding
/// synching does not apply)
///
/// Journal File
/// ============
///
/// The journal file has the following format:
///
/// Action (4 bytes): Indicates what should happen when the store file is
/// opened and the journal file is present: 0 = do nothing, 1 = apply changes
///
/// Timestamp (8 bytes): The creation timestamp of the data store. If there
/// is a mismatch between the timestamps of the store and the journal, the
/// journal is discarded (for example, the store was deleted and re-created,
/// but the journal of the old store file was left behind).
///
/// Journaling Instruction (zero or more):
///
///     Offset and length (8 bytes): The top 54 bits contain the offset of the
///     word (*not* the byte) where the first change should be written.
///     The lower 10 bits contain the number of words to write (-1):
///      e.g. 0x8ffc04 means "write 5 words starting at offset 0x23ff0"
///      (0x8ffc * 4)
///
///     Value (4 bytes; one or more): The 4-byte values to write
///
/// End Marker (8 bytes):   0xffff_ffff_ffff_ffff
/// Checksum (4 bytes):     A CRC32 calculated over the Journaling Instructions
///
/// - Journaling instructions never cross block boundaries
/// - Each journaling instruction can encode up to 1024 words (one 4-KB block)
///
/// File Recovery
/// =============
///
/// If a process terminates abnormally while a Store Transaction is open,
/// it may leave behind a journal file. The next time the Store is opened,
/// the open() method checks for presence of a "hot" journal (a journal
/// that contains valid instructions) and resets the store file to the
/// state after the last successful call to commit(), or its the state
/// prior to the start of the transaction if commit() was not called
/// (or did not complete successfully).
///
/// This ensures that users of the Store class will never find the store file
/// in an inconsistent state due to partially-executed writes.
///
/// (A partially-written journal is discarded; commit() will never begin
/// writing journaled changes to the store file until the journal is
/// completely written to disk).
///
/// TODO: byte order of journal
/// </summary>
/// <remarks>
/// Ported from Java <c>com.clarisma.common.store.Store</c>.
///
/// PORT NOTES vs. the Java original:
/// - <c>FileChannel</c> + <c>MappedByteBuffer[]</c> → a <see cref="FileStream"/> plus one
///   <see cref="MemoryMappedFile"/> + view per 1 GB segment, each exposed as a
///   <see cref="System.Memory{Byte}"/>-backed <see cref="NioBuffer"/> via a
///   <c>Segment</c>. Mapping a new segment never disturbs existing ones
///   (mirrors Java's independent per-segment <c>map()</c>), so there is no shared mapping to grow.
/// - Locking: Java uses byte-range file locks (shared for readers, exclusive for writers/append).
///   The BCL's <c>FileStream.Lock</c> is exclusive-only, so locking goes through the
///   <c>FileStream.LockRange</c>/<c>TryLockRange</c>/<c>UnlockRange</c> extensions, which provide
///   shared and exclusive byte-range locks cross-platform (<c>LockFileEx</c> on Windows; <c>fcntl</c>
///   — OFD locks on Linux — on POSIX). <c>Lock()</c> uses the <b>blocking</b> form (like Java's
///   <c>channel.lock</c>); <c>TryExclusiveLock()</c> uses the non-blocking form (like
///   <c>channel.tryLock</c>). See PORT.md for the per-platform details.
/// - PORT-LIMITATION (sparse files): on Windows the backing file is marked sparse via P/Invoke
///   so that mapping a 1 GB segment does not allocate 1 GB physically; on Unix files are sparse
///   by default. If marking fails, the store still works but may consume more disk.
/// - <c>Unsafe.invokeCleaner</c> unmapping → deterministic <see cref="IDisposable.Dispose"/>.
/// </remarks>
internal abstract class Store
{

    static readonly HashSet<string> _openStores = new HashSet<string>();

    string? _path;
    FileStream? _channel;
    FileStream? _journal;
    int _lockLevel;
    bool _lockReadHeld;
    bool _lockWriteHeld;
    Segment?[] _mappings = new Segment?[0];
    protected Segment? baseMapping;
    readonly object _mappingsLock = new object();
    readonly object _transactionLock = new object();
    Dictionary<long, TransactionBlock>? _transactionBlocks;
    long _preTransactionFileSize;

    protected const int MAPPING_SIZE = 1 << 30;

    protected const int LOCK_NONE = 0;
    protected const int LOCK_READ = 1;
    protected internal const int LOCK_APPEND = 2;
    protected const int LOCK_EXCLUSIVE = 3;

    // NOTE (port): unlike the Java original, this does NOT cache the mapped "original" buffer;
    // the mapping is re-fetched via GetMapping(SegmentOfPos(pos)) at point of use. (Harmless now
    // that segments are mapped independently and never invalidated; kept as a defensive habit.)
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.TransactionBlock</c>.</remarks>
    sealed class TransactionBlock
    {

        public long pos;
        public NioBuffer current = null!;

    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.path()</c>.</remarks>
    public string Path => _path!;

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.setPath(Path)</c>.</remarks>
    public void SetPath(string path)
    {
        if (_channel != null)
            throw new StoreException("Store is already open", path);
        _path = path;
    }

    // === Abstract Methods / Common Overrides ===

    /// <summary>
    /// Creates the metadata for an empty store. Called by open() if the file-type indicator is zero.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.createStore()</c>.</remarks>
    protected abstract void CreateStore();

    /// <summary>
    /// Checks the file-type indicator and other metadata fields (e.g. file format version).
    /// Called by open().
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.verifyHeader()</c>.</remarks>
    protected abstract void VerifyHeader();

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.initialize()</c>.</remarks>
    protected virtual void Initialize()
    {
        // by default, do nothing
    }

    /// <summary>
    /// Gets the "real" size of the store file. Memory-mapping causes the file to grow, so the file
    /// size returned by the OS is typically larger than the actual space used by the file.
    /// </summary>
    /// <returns>file size in bytes</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.getTrueSize()</c>.</remarks>
    protected abstract long GetTrueSize();

    /// <summary>
    /// Gets the creation timestamp of the Store. Where this is stored is at the discretion of
    /// subclasses; however, it should be recorded in the file itself, as OS-provided metadata is
    /// not always reliable.
    /// </summary>
    /// <returns>the creation timestamp (milliseconds since midnight, January 1, 1970 UTC)</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.getTimestamp()</c>.</remarks>
    protected abstract long GetTimestamp();

    // === File Mapping ===

    // maps segments lazily
    // (protected internal so BlobStoreChecker, in the same assembly, can read segments —
    // Java relied on package-private access here.)
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.getMapping(int)</c>.</remarks>
    protected internal Segment GetMapping(int n)
    {
        lock (_mappingsLock)
        {
            if (n < _mappings.Length && _mappings[n] != null)
                return _mappings[n]!;

            if (n >= _mappings.Length)
                System.Array.Resize(ref _mappings, n + 1);

            var seg = MapSegment(n);
            _mappings[n] = seg;
            if (n == 0)
                baseMapping = seg;
            return seg;
        }
    }

    // Maps one 1 GB segment as its own MemoryMappedFile + view, wrapped in a Memory&lt;byte&gt;-backed
    // NioBuffer. Each segment is independent: mapping a later segment never invalidates earlier ones
    // (Java's per-segment FileChannel.map() semantics), so there is no shared mapping to grow.
    /// <remarks>Port-only helper: maps one 1 GB segment as a <c>Segment</c>.</remarks>
    Segment MapSegment(int n)
    {
        try
        {
            var required = (long)(n + 1) * MAPPING_SIZE;
            if (_channel!.Length < required)
                _channel.SetLength(required);

            var mmf = MemoryMappedFile.CreateFromFile(_channel, null, required, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
            var view = mmf.CreateViewAccessor((long)n * MAPPING_SIZE, MAPPING_SIZE, MemoryMappedFileAccess.ReadWrite);
            return new Segment(mmf, view, MAPPING_SIZE);
        }
        catch (IOException ex)
        {
            throw new StoreException(string.Format(CultureInfo.InvariantCulture, "{0}: Failed to map segment at {1:X} ({2})", _path, (long)n * MAPPING_SIZE, ex.Message), ex);
        }
    }


    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.unmapSegments()</c>.</remarks>
    bool UnmapSegments()
    {
        lock (_mappingsLock)
        {
            foreach (var b in _mappings)
                ((IDisposable?)b)?.Dispose();

            _mappings = new Segment?[0];
            baseMapping = null;
            return true;
        }
    }

    /// <summary>
    /// Locks (or unlocks) the store file. See the class remarks for the shared-lock limitation.
    /// </summary>
    /// <returns>the previous lock level</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.lock(int)</c>.</remarks>
    protected int Lock(int newLevel)
    {
        var oldLevel = _lockLevel;
        if (newLevel != oldLevel)
        {
            if (_lockLevel == LOCK_EXCLUSIVE || newLevel == LOCK_NONE)
            {
                // Java: lockRead.release(); lockRead = null;
                if (_lockReadHeld)
                { _channel!.UnlockRange(0, 4); _lockReadHeld = false; }
                _lockLevel = LOCK_NONE;
            }
            if (_lockLevel == LOCK_NONE && newLevel != LOCK_NONE)
            {
                // Java: lockRead = channel.lock(0, 4, newLevel != LOCK_EXCLUSIVE) — a *blocking* lock,
                // shared for READ/APPEND, exclusive for EXCLUSIVE.
                _channel!.LockRange(0, 4, exclusive: newLevel == LOCK_EXCLUSIVE);
                _lockReadHeld = true;
            }
            if (oldLevel == LOCK_APPEND)
            {
                // Java: lockWrite.release(); lockWrite = null;
                if (_lockWriteHeld)
                { _channel!.UnlockRange(4, 4); _lockWriteHeld = false; }
            }
            if (newLevel == LOCK_APPEND)
            {
                // Java: lockWrite = channel.lock(4, 4, false) — a blocking exclusive write lock on [4, 8).
                _channel!.LockRange(4, 4, exclusive: true);
                _lockWriteHeld = true;
            }
            _lockLevel = newLevel;
        }
        return oldLevel;
    }

    /// <summary>
    /// Attempts to obtain an exclusive lock on the store file. The store file must be unlocked.
    /// If another process holds any lock on this Store, the method returns immediately.
    /// </summary>
    /// <returns>true if the exclusive lock was obtained, or false if another process holds a lock</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.tryExclusiveLock()</c>.</remarks>
    protected bool TryExclusiveLock()
    {
        System.Diagnostics.Debug.Assert(_lockLevel == LOCK_NONE);
        bool acquired;
        try
        {
            acquired = _channel!.TryLockRange(0, 4, exclusive: true);
        }
        catch (IOException)
        {
            return false;
        }
        if (!acquired)
            return false;
        _lockReadHeld = true;
        _lockLevel = LOCK_EXCLUSIVE;
        return true;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.open()</c>.</remarks>
    public void Open()
    {
        Open(LOCK_READ);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.openExclusive()</c>.</remarks>
    public void OpenExclusive()
    {
        Open(LOCK_EXCLUSIVE);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.open(int)</c>.</remarks>
    protected void Open(int lockMode)
    {
        if (_channel != null)
        {
            throw new StoreException("Store is already open", _path!);
        }
        var fileName = _path!;
        lock (_openStores)
        {
            if (_openStores.Contains(fileName))
            {
                throw new StoreException(
                    "Only one instance may be open within the same process", _path!);
            }
            _openStores.Add(fileName);
        }
        try
        {
            var existed = File.Exists(_path!);
            _channel = new FileStream(_path!, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.ReadWrite);
            if (!existed)
                MarkSparse(_channel);
            Lock(lockMode);

            // Always do this first, even if journal is present
            baseMapping = GetMapping(0);
            var headerWord = baseMapping!.Memory.Span.GetIntLE(0);
            if (headerWord == 0)
            {
                CreateStore();
            }

            var journalFile = GetJournalFile();
            if (File.Exists(journalFile))
            {
                ProcessJournal(journalFile);
            }
            VerifyHeader(); // TODO: when to do this?
            Initialize();
        }
        catch (IOException ex)
        {
            Close();
            throw new StoreException("Failed to open store", _path!, ex);
        }
    }

    // TODO: use Bytes.putInt
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.intToBytes(byte[], int)</c>.</remarks>
    static void IntToBytes(byte[] ba, int v)
    {
        ba[0] = (byte)v;
        ba[1] = (byte)(v >> 8);
        ba[2] = (byte)(v >> 16);
        ba[3] = (byte)(v >> 24);
    }

    // === Journaling ===

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.getJournalFile()</c>.</remarks>
    protected string GetJournalFile()
    {
        return _path + "-journal";
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.openJournal(File)</c>.</remarks>
    void OpenJournal(string journalFile)
    {
        System.Diagnostics.Debug.Assert(_journal == null);
        _journal = new FileStream(journalFile, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.ReadWrite);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.processJournal(File)</c>.</remarks>
    protected bool ProcessJournal(string journalFile)
    {
        if (_journal == null)
            OpenJournal(journalFile);
        _journal!.Seek(0, SeekOrigin.Begin);
        var instruction = JournalReadInt();
        if (instruction == 0)
            return false;

        var prevLockLevel = Lock(LOCK_APPEND); // TODO: need exclusive lock!

        // Check header again, because another process may have already
        // processed the journal while we were waiting for the lock
        _journal.Seek(0, SeekOrigin.Begin);
        instruction = JournalReadInt();
        if (instruction == 0)
            return false;

        var appliedJournal = false;
        if (VerifyJournal())
        {
            ApplyJournal();
            appliedJournal = true;
        }
        ClearJournal();
        Lock(prevLockLevel);
        return appliedJournal;
    }

    /// <summary>
    /// Checks whether the Journal File is valid. The Journal File must be open prior to calling
    /// this method.
    /// </summary>
    /// <returns>true if the journal file is complete and valid</returns>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.verifyJournal()</c>.</remarks>
    bool VerifyJournal()
    {
        var ba = new byte[4];
        var crc = new Crc32();
        try
        {
            _journal!.Seek(4, SeekOrigin.Begin);
            var timestamp = JournalReadLong();
            if (timestamp != GetTimestamp())
                return false;
            for (; ; )
            {
                var patchLow = JournalReadInt();
                var patchHigh = JournalReadInt();
                if (patchHigh == unchecked((int)0xffff_ffff) && patchLow == unchecked((int)0xffff_ffff))
                    break;
                var len = (patchLow & 0x3ff) + 1;
                IntToBytes(ba, patchLow);
                crc.Append(ba);
                IntToBytes(ba, patchHigh);
                crc.Append(ba);
                for (var i = 0; i < len; i++)
                {
                    IntToBytes(ba, JournalReadInt());
                    crc.Append(ba);
                }
            }
            return JournalReadInt() == (int)crc.GetCurrentHashAsUInt32();
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }

    /// <summary>
    /// Applies the edit instructions from the journal to the data store, and syncs the resulting
    /// changes to disk. The journal must be opened and verified prior to calling this method.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.applyJournal()</c>.</remarks>
    void ApplyJournal()
    {
        var affectedSegments = new HashSet<int>();

        Log.Debug("Applying journal...");

        var patchCount = 0;
        _journal!.Seek(12, SeekOrigin.Begin);
        for (; ; )
        {
            var patchLow = JournalReadInt();
            var patchHigh = JournalReadInt();
            if (patchHigh == unchecked((int)0xffff_ffff) && patchLow == unchecked((int)0xffff_ffff))
                break;
            var pos = ((long)patchHigh << 32) | ((long)patchLow & 0xffff_ffffL);
            pos = (pos >> 10) << 2; // TODO: careful of sign
            var len = (patchLow & 0x3ff) + 1;
            var segmentNumber = (int)(pos >> 30);
            var ofs = (int)pos & 0x3fff_ffff;
            var buf = new NioBufferWriter(GetMapping(segmentNumber).Memory);
            affectedSegments.Add(segmentNumber);
            for (var i = 0; i < len; i++)
            {
                var v = JournalReadInt();
                buf.PutInt(ofs, v);
                ofs += 4;
                patchCount++;
            }
        }
        Log.Debug("Syncing patches...");
        SyncSegments(affectedSegments);
        Log.Debug("Patched %d words in %d segments.", patchCount, affectedSegments.Count);
    }

    /// <summary>
    /// Resets the journal and flushes it to disk.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.clearJournal()</c>.</remarks>
    void ClearJournal()
    {
        _journal!.Seek(0, SeekOrigin.Begin);
        JournalWriteInt(0);
        _journal.SetLength(4); // TODO: just trim to 0 instead?
        _journal.Flush(true);
    }

    /// <summary>
    /// Writes the rollback instructions for the current transaction into the Journal File.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.saveJournal()</c>.</remarks>
    void SaveJournal()
    {
        if (_journal == null)
            OpenJournal(GetJournalFile());
        _journal!.Seek(0, SeekOrigin.Begin);
        JournalWriteInt(1); // TODO
        JournalWriteLong(GetTimestamp());
        var ba = new byte[4];
        var crc = new Crc32();
        foreach (var block in _transactionBlocks!.Values)
        {
            var pCurrent = 0;
            var original = new NioBufferReader(GetMapping(SegmentOfPos(block.pos)).Memory);
            NioBuffer current = block.current;
            var originalOfs = (int)(block.pos & 0x3fff_ffff);
            var pOriginal = originalOfs;
            for (; ; )
            {
                var oldValue = original.GetInt(pOriginal);
                var newValue = current.GetInt(pCurrent);
                if (oldValue != newValue)
                {
                    var pCurrentStart = pCurrent;
                    for (; ; )
                    {
                        pCurrent += 4;
                        pOriginal += 4;
                        if (pCurrent == 4096)
                            break;
                        oldValue = original.GetInt(pOriginal);
                        newValue = current.GetInt(pCurrent);
                        if (oldValue == newValue)
                            break;
                    }
                    var pos = (block.pos + pCurrentStart) << 8;
                    System.Diagnostics.Debug.Assert((pos & 0x3ff) == 0); // lower 10 bits must be clear
                    var len = (pCurrent - pCurrentStart) / 4;
                    System.Diagnostics.Debug.Assert(len > 0 && len <= 1024);
                    var patchLow = (int)pos | (len - 1);
                    var patchHigh = (int)((ulong)pos >> 32);
                    JournalWriteInt(patchLow);
                    JournalWriteInt(patchHigh);
                    IntToBytes(ba, patchLow);
                    crc.Append(ba);
                    IntToBytes(ba, patchHigh);
                    crc.Append(ba);

                    var pEnd = pCurrent;
                    var p = pCurrentStart;
                    p += originalOfs;
                    pEnd += originalOfs;
                    for (; p < pEnd; p += 4)
                    {
                        var v = original.GetInt(p);
                        JournalWriteInt(v);
                        IntToBytes(ba, v);
                        crc.Append(ba);
                    }
                }
                pCurrent += 4;
                if (pCurrent >= 4096)
                    break;
                pOriginal += 4;
            }
        }
        JournalWriteInt(unchecked((int)0xffff_ffff));
        JournalWriteInt(unchecked((int)0xffff_ffff));
        JournalWriteInt((int)crc.GetCurrentHashAsUInt32());
        _journal.Flush(true);
    }

    // === Transactions ===

    /// <summary>
    /// Ensures that modified segments are written to disk.
    /// </summary>
    /// <param name="affectedSegments">a set of integers which specify the segments to sync to disk</param>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.syncSegments(IntSet)</c>.</remarks>
    void SyncSegments(HashSet<int> affectedSegments)
    {
        foreach (var segment in affectedSegments)
        {
            GetMapping(segment).Force();
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.close()</c>.</remarks>
    public void Close()
    {
        if (_channel == null)
            return;

        try
        {
            var trueSize = GetTrueSize();
            var journalPresent = false;
            var journalFile = GetJournalFile();
            if (_journal != null)
            {
                journalPresent = true;
            }
            if (!journalPresent)
            {
                journalPresent = File.Exists(journalFile);
            }

            Lock(LOCK_NONE);
            var segmentUnmapAttempted = false;

            if (journalPresent || trueSize > 0)
            {
                if (TryExclusiveLock())
                {
                    if (journalPresent)
                    {
                        if (ProcessJournal(journalFile))
                        {
                            trueSize = GetTrueSize();
                        }
                        if (_journal != null)
                        {
                            _journal.Dispose();
                            _journal = null;
                        }
                        File.Delete(journalFile);
                    }
                    if (trueSize > 0)
                    {
                        segmentUnmapAttempted = true;
                        if (UnmapSegments())
                        {
                            _channel.SetLength(trueSize);
                        }
                    }
                    Lock(LOCK_NONE);
                }
            }
            if (!segmentUnmapAttempted)
                UnmapSegments();
            _channel.Dispose();
        }
        catch (IOException ex)
        {
            throw new StoreException(
                string.Format(CultureInfo.InvariantCulture, "Error while closing file ({0})", ex.Message),
                _path!, ex);
        }
        finally
        {
            _channel = null;
            _lockLevel = LOCK_NONE;
            _lockReadHeld = false;
            _lockWriteHeld = false;
            _mappings = new Segment?[0];
            lock (_openStores)
            {
                _openStores.Remove(_path!);
            }
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.beginTransaction(int)</c>.</remarks>
    protected internal void BeginTransaction(int transactionLockLevel)
    {
        System.Diagnostics.Debug.Assert(transactionLockLevel == LOCK_APPEND || transactionLockLevel == LOCK_EXCLUSIVE);
        System.Threading.Monitor.Enter(_transactionLock);
        try
        {
            Lock(transactionLockLevel);
            try
            {
                var journalFile = GetJournalFile();
                if (File.Exists(journalFile))
                    ProcessJournal(journalFile);
                _preTransactionFileSize = GetTrueSize();
            }
            catch (Exception)
            {
                Lock(LOCK_READ);
                throw;
            }
        }
        catch (Exception)
        {
            System.Threading.Monitor.Exit(_transactionLock);
            throw;
        }
        _transactionBlocks = new Dictionary<long, TransactionBlock>();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.endTransaction()</c>.</remarks>
    protected internal void EndTransaction()
    {
        System.Diagnostics.Debug.Assert(IsInTransaction());
        System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_transactionLock));
        _transactionBlocks = null;
        try
        {
            Lock(LOCK_READ);
        }
        finally
        {
            System.Threading.Monitor.Exit(_transactionLock);
        }
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.isInTransaction()</c>.</remarks>
    protected bool IsInTransaction()
    {
        return _transactionBlocks != null;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.rollback()</c>.</remarks>
    protected void Rollback()
    {
        System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_transactionLock));
        _transactionBlocks!.Clear();
    }

    /// <summary>
    /// Returns the number of the segment in which the given file position is located.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.segmentOfPos(long)</c>.</remarks>
    protected static int SegmentOfPos(long pos)
    {
        return (int)(pos >> 30);
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.commit()</c>.</remarks>
    protected internal void Commit()
    {
        System.Diagnostics.Debug.Assert(IsInTransaction());
        System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(_transactionLock));

        SaveJournal();

        var affectedSegments = new HashSet<int>();
        foreach (var block in _transactionBlocks!.Values)
        {
            var segment = SegmentOfPos(block.pos);
            var ofs = (int)block.pos & 0x3fff_ffff;
            System.Diagnostics.Debug.Assert((ofs & 0xfff) == 0);
            System.Diagnostics.Debug.Assert(block.current.Array()!.Length == 4096);
            new NioBufferWriter(GetMapping(segment).Memory).Put(ofs, block.current.Array()!);
            affectedSegments.Add(segment);
        }

        var currentFileSize = GetTrueSize();
        if (currentFileSize > _preTransactionFileSize)
        {
            var firstSegment = SegmentOfPos(_preTransactionFileSize);
            var lastSegment = SegmentOfPos(currentFileSize - 1);
            for (var segment = firstSegment; segment <= lastSegment; segment++)
            {
                affectedSegments.Add(segment);
            }
        }

        SyncSegments(affectedSegments);

        ClearJournal();
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.getBlock(long)</c>.</remarks>
    protected internal NioBuffer GetBlock(long pos)
    {
        System.Diagnostics.Debug.Assert((pos & 0xfff) == 0, string.Format(CultureInfo.InvariantCulture, "{0}: Block must start at 4KB-aligned position", pos));

        if (pos < _preTransactionFileSize)
        {
            _transactionBlocks!.TryGetValue(pos, out var block);
            if (block == null)
            {
                block = new TransactionBlock();
                block.pos = pos;
                var original = new NioBufferReader(GetMapping((int)(pos >> 30)).Memory);
                var ofs = (int)pos & 0x3fff_ffff;
                var copy = new byte[4096];
                original.Get(ofs, copy);
                block.current = NioBuffer.Wrap(copy);
                block.current.Order(ByteOrder.LittleEndian); // TODO
                _transactionBlocks[pos] = block;
            }
            return block.current;
        }

        // Boundary: GetBlock still returns a ByteBuffer block for the (not-yet-migrated) write path.
        var buf = NioBuffer.Of(GetMapping((int)(pos >> 30)).Memory);
        buf = buf.Slice((int)pos & 0x3fff_ffff, 4096);
        buf.Order(ByteOrder.LittleEndian);
        return buf;
    }

    /// <remarks>Ported from Java <c>com.clarisma.common.store.Store.currentFileSize()</c>.</remarks>
    public long CurrentFileSize()
    {
        return new FileInfo(_path!).Length;
    }

    // === Journal big-endian primitives (Java RandomAccessFile is big-endian) ===

    /// <remarks>Port-only helper for Java's <c>RandomAccessFile.writeInt(int)</c> (big-endian).</remarks>
    void JournalWriteInt(int v)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, v);
        _journal!.Write(b);
    }

    /// <remarks>Port-only helper for Java's <c>RandomAccessFile.writeLong(long)</c> (big-endian).</remarks>
    void JournalWriteLong(long v)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(b, v);
        _journal!.Write(b);
    }

    /// <remarks>Port-only helper for Java's <c>RandomAccessFile.readInt()</c> (big-endian).</remarks>
    int JournalReadInt()
    {
        Span<byte> b = stackalloc byte[4];
        ReadFully(b);
        return BinaryPrimitives.ReadInt32BigEndian(b);
    }

    /// <remarks>Port-only helper for Java's <c>RandomAccessFile.readLong()</c> (big-endian).</remarks>
    long JournalReadLong()
    {
        Span<byte> b = stackalloc byte[8];
        ReadFully(b);
        return BinaryPrimitives.ReadInt64BigEndian(b);
    }

    /// <remarks>Port-only helper: reads exactly <c>dst.Length</c> bytes or throws (Java's
    /// <c>RandomAccessFile.readXxx</c> throw <c>EOFException</c> on a short read).</remarks>
    void ReadFully(Span<byte> dst)
    {
        var read = 0;
        while (read < dst.Length)
        {
            var n = _journal!.Read(dst.Slice(read));
            if (n <= 0)
                throw new EndOfStreamException();
            read += n;
        }
    }

    // Marks a file as sparse on Windows so that mapping large segments does not
    // physically allocate them. No-op (and unnecessary) on other platforms.
    /// <remarks>Port-only helper: Windows sparse-file marking (Java requests this via the
    /// <c>SPARSE</c> open option; on Unix files are sparse by default).</remarks>
    static void MarkSparse(FileStream fs)
    {
        if (!OperatingSystem.IsWindows())
            return;
        try
        {
            const uint FSCTL_SET_SPARSE = 0x000900C4;
            DeviceIoControl(fs.SafeFileHandle.DangerousGetHandle(), FSCTL_SET_SPARSE,
                IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero);
        }
        catch
        {
            // best effort; store still works without sparse marking
        }
    }

    /// <remarks>Port-only: P/Invoke for the Windows sparse-file marking in <see cref="MarkSparse"/>.</remarks>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

}
