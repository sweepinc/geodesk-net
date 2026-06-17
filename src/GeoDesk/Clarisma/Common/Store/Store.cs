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
using Clarisma.Common.Nio;
using Clarisma.Common.Util;
using ByteOrder = Clarisma.Common.Nio.ByteOrder;
using NioBuffer = Clarisma.Common.Nio.ByteBuffer;

namespace Clarisma.Common.Store;

/// <summary>
/// Base class for persistent data stores that supports transactions and journaling.
/// A Store is backed by a sparse file that is memory-mapped in segments, 1 GB each.
///
/// PORT NOTES vs. the Java original:
/// - <c>FileChannel</c> + <c>MappedByteBuffer[]</c> → a <see cref="FileStream"/> plus a
///   <see cref="MemoryMappedFile"/> from which one view accessor per 1 GB segment is taken.
/// - PORT-LIMITATION (locking): Java uses *shared* byte-range file locks (multiple readers).
///   .NET's <c>FileStream.Lock</c> is exclusive-only and non-blocking, so shared (read) locks
///   are treated as best-effort no-ops here. This is sufficient for single-process access but
///   does not enforce the full multi-process locking protocol.
/// - PORT-LIMITATION (sparse files): on Windows the backing file is marked sparse via P/Invoke
///   so that mapping a 1 GB segment does not allocate 1 GB physically; on Unix files are sparse
///   by default. If marking fails, the store still works but may consume more disk.
/// - <c>Unsafe.invokeCleaner</c> unmapping → deterministic <see cref="IDisposable.Dispose"/>.
/// </summary>
public abstract class Store
{
    private static readonly HashSet<string> openStores = new HashSet<string>();
    private string? path;
    private FileStream? channel;
    private MemoryMappedFile? mmf;
    private long mmfCapacity;
    private FileStream? journal;
    private int lockLevel;
    private bool lockReadHeld;
    private bool lockWriteHeld;
    private MappedByteBuffer?[] mappings = new MappedByteBuffer?[0];
    protected MappedByteBuffer? baseMapping;
    private readonly object mappingsLock = new object();
    private readonly object transactionLock = new object();
    private Dictionary<long, TransactionBlock>? transactionBlocks;
    private long preTransactionFileSize;

    protected const int MAPPING_SIZE = 1 << 30;

    protected const int LOCK_NONE = 0;
    protected const int LOCK_READ = 1;
    protected const int LOCK_APPEND = 2;
    protected const int LOCK_EXCLUSIVE = 3;

    // NOTE (port): unlike the Java original, this does NOT cache the mapped "original"
    // buffer. Because GrowMapping recreates view accessors when the file crosses a 1 GB
    // segment boundary, a cached mapped buffer could go stale mid-transaction. Instead the
    // mapping is re-fetched via GetMapping(SegmentOfPos(pos)) at point of use.
    private sealed class TransactionBlock
    {
        public long pos;
        public NioBuffer current = null!;
    }

    public string Path => path!;

    public void SetPath(string path)
    {
        if (channel != null) throw new StoreException("Store is already open", path);
        this.path = path;
    }

    // === Abstract Methods / Common Overrides ===

    protected abstract void CreateStore();

    protected abstract void VerifyHeader();

    protected virtual void Initialize()
    {
        // by default, do nothing
    }

    protected abstract long GetTrueSize();

    protected abstract long GetTimestamp();

    // === File Mapping ===

    // maps segments lazily
    // (protected internal so BlobStoreChecker, in the same assembly, can read segments —
    // Java relied on package-private access here.)
    protected internal MappedByteBuffer GetMapping(int n)
    {
        lock (mappingsLock)
        {
            if (n < mappings.Length && mappings[n] != null) return mappings[n]!;

            long required = (long)(n + 1) * MAPPING_SIZE;
            if (mmf == null || mmfCapacity < required)
            {
                GrowMapping(required);
            }
            if (n >= mappings.Length)
            {
                System.Array.Resize(ref mappings, n + 1);
            }
            MappedByteBuffer buf = CreateView(n);
            mappings[n] = buf;
            if (n == 0) baseMapping = buf;
            return buf;
        }
    }

    private MappedByteBuffer CreateView(int n)
    {
        try
        {
            var accessor = mmf!.CreateViewAccessor((long)n * MAPPING_SIZE, MAPPING_SIZE,
                MemoryMappedFileAccess.ReadWrite);
            var buf = new MappedByteBuffer(accessor, MAPPING_SIZE);
            buf.Order(ByteOrder.LittleEndian); // TODO: check!
            return buf;
        }
        catch (IOException ex)
        {
            throw new StoreException(
                string.Format(CultureInfo.InvariantCulture, "{0}: Failed to map segment at {1:X} ({2})",
                    path, (long)n * MAPPING_SIZE, ex.Message), ex);
        }
    }

    // Grows (or creates) the memory mapping to at least the requested capacity.
    // NOTE: this disposes and recreates the MemoryMappedFile and therefore all existing
    // view accessors. Buffers obtained from GetMapping before a growth must be re-fetched.
    // (For sub-1 GB stores the mapping is created once and never grows, so this is moot.)
    private void GrowMapping(long required)
    {
        MappedByteBuffer?[] old = mappings;
        foreach (MappedByteBuffer? b in old) b?.Dispose();
        mmf?.Dispose();
        mmf = null;

        if (channel!.Length < required)
        {
            channel.SetLength(required);
        }
        mmf = MemoryMappedFile.CreateFromFile(channel, null, required,
            MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, leaveOpen: true);
        mmfCapacity = required;

        // recreate views for previously mapped segments
        mappings = new MappedByteBuffer?[old.Length];
        for (int i = 0; i < old.Length; i++)
        {
            if (old[i] != null)
            {
                mappings[i] = CreateView(i);
                if (i == 0) baseMapping = mappings[i];
            }
        }
    }

    private bool UnmapSegments()
    {
        lock (mappingsLock)
        {
            foreach (MappedByteBuffer? b in mappings) b?.Dispose();
            mappings = new MappedByteBuffer?[0];
            baseMapping = null;
            mmf?.Dispose();
            mmf = null;
            mmfCapacity = 0;
            return true;
        }
    }

    /// <summary>
    /// Locks (or unlocks) the store file. See the class remarks for the shared-lock limitation.
    /// </summary>
    /// <returns>the previous lock level</returns>
    protected int Lock(int newLevel)
    {
        int oldLevel = lockLevel;
        if (newLevel != oldLevel)
        {
            if (lockLevel == LOCK_EXCLUSIVE || newLevel == LOCK_NONE)
            {
                ReleaseRange(ref lockReadHeld, 0, 4);
                lockLevel = LOCK_NONE;
            }
            if (lockLevel == LOCK_NONE && newLevel != LOCK_NONE)
            {
                // shared lock (newLevel != EXCLUSIVE) is a best-effort no-op; only the
                // exclusive read lock is enforced via an OS byte-range lock.
                if (newLevel == LOCK_EXCLUSIVE) AcquireRange(ref lockReadHeld, 0, 4);
            }
            if (oldLevel == LOCK_APPEND)
            {
                ReleaseRange(ref lockWriteHeld, 4, 4);
            }
            if (newLevel == LOCK_APPEND)
            {
                AcquireRange(ref lockWriteHeld, 4, 4);
            }
            lockLevel = newLevel;
        }
        return oldLevel;
    }

    private void AcquireRange(ref bool held, long position, long length)
    {
        if (held) return;
        try
        {
            channel!.Lock(position, length);
            held = true;
        }
        catch (IOException)
        {
            // Region already locked by another process; matches a failed blocking lock
            // only loosely (see class remarks).
        }
    }

    private void ReleaseRange(ref bool held, long position, long length)
    {
        if (!held) return;
        try
        {
            channel!.Unlock(position, length);
        }
        catch (IOException)
        {
        }
        held = false;
    }

    protected bool TryExclusiveLock()
    {
        System.Diagnostics.Debug.Assert(lockLevel == LOCK_NONE);
        try
        {
            channel!.Lock(0, 4);
        }
        catch (IOException)
        {
            return false;
        }
        lockReadHeld = true;
        lockLevel = LOCK_EXCLUSIVE;
        return true;
    }

    public void Open()
    {
        Open(LOCK_READ);
    }

    public void OpenExclusive()
    {
        Open(LOCK_EXCLUSIVE);
    }

    protected void Open(int lockMode)
    {
        if (channel != null)
        {
            throw new StoreException("Store is already open", path!);
        }
        string fileName = path!;
        lock (openStores)
        {
            if (openStores.Contains(fileName))
            {
                throw new StoreException(
                    "Only one instance may be open within the same process", path!);
            }
            openStores.Add(fileName);
        }
        try
        {
            bool existed = File.Exists(path!);
            channel = new FileStream(path!, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.ReadWrite);
            if (!existed) MarkSparse(channel);
            Lock(lockMode);

            // Always do this first, even if journal is present
            baseMapping = GetMapping(0);
            int headerWord = baseMapping.GetInt(0);
            if (headerWord == 0)
            {
                CreateStore();
            }

            string journalFile = GetJournalFile();
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
            throw new StoreException("Failed to open store", path!, ex);
        }
    }

    // TODO: use Bytes.putInt
    private static void IntToBytes(byte[] ba, int v)
    {
        ba[0] = (byte)v;
        ba[1] = (byte)(v >> 8);
        ba[2] = (byte)(v >> 16);
        ba[3] = (byte)(v >> 24);
    }

    // === Journaling ===

    protected string GetJournalFile()
    {
        return path + "-journal";
    }

    private void OpenJournal(string journalFile)
    {
        System.Diagnostics.Debug.Assert(journal == null);
        journal = new FileStream(journalFile, FileMode.OpenOrCreate, FileAccess.ReadWrite,
            FileShare.ReadWrite);
    }

    protected bool ProcessJournal(string journalFile)
    {
        if (journal == null) OpenJournal(journalFile);
        journal!.Seek(0, SeekOrigin.Begin);
        int instruction = JournalReadInt();
        if (instruction == 0) return false;

        int prevLockLevel = Lock(LOCK_APPEND); // TODO: need exclusive lock!

        // Check header again, because another process may have already
        // processed the journal while we were waiting for the lock
        journal.Seek(0, SeekOrigin.Begin);
        instruction = JournalReadInt();
        if (instruction == 0) return false;

        bool appliedJournal = false;
        if (VerifyJournal())
        {
            ApplyJournal();
            appliedJournal = true;
        }
        ClearJournal();
        Lock(prevLockLevel);
        return appliedJournal;
    }

    private bool VerifyJournal()
    {
        byte[] ba = new byte[4];
        Crc32 crc = new Crc32();
        try
        {
            journal!.Seek(4, SeekOrigin.Begin);
            long timestamp = JournalReadLong();
            if (timestamp != GetTimestamp()) return false;
            for (; ; )
            {
                int patchLow = JournalReadInt();
                int patchHigh = JournalReadInt();
                if (patchHigh == unchecked((int)0xffff_ffff) && patchLow == unchecked((int)0xffff_ffff)) break;
                int len = (patchLow & 0x3ff) + 1;
                IntToBytes(ba, patchLow);
                crc.Append(ba);
                IntToBytes(ba, patchHigh);
                crc.Append(ba);
                for (int i = 0; i < len; i++)
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

    private void ApplyJournal()
    {
        HashSet<int> affectedSegments = new HashSet<int>();

        Log.Debug("Applying journal...");

        int patchCount = 0;
        journal!.Seek(12, SeekOrigin.Begin);
        for (; ; )
        {
            int patchLow = JournalReadInt();
            int patchHigh = JournalReadInt();
            if (patchHigh == unchecked((int)0xffff_ffff) && patchLow == unchecked((int)0xffff_ffff)) break;
            long pos = ((long)patchHigh << 32) | ((long)patchLow & 0xffff_ffffL);
            pos = (pos >> 10) << 2; // TODO: careful of sign
            int len = (patchLow & 0x3ff) + 1;
            int segmentNumber = (int)(pos >> 30);
            int ofs = (int)pos & 0x3fff_ffff;
            MappedByteBuffer buf = GetMapping(segmentNumber);
            affectedSegments.Add(segmentNumber);
            for (int i = 0; i < len; i++)
            {
                int v = JournalReadInt();
                buf.PutInt(ofs, v);
                ofs += 4;
                patchCount++;
            }
        }
        Log.Debug("Syncing patches...");
        SyncSegments(affectedSegments);
        Log.Debug("Patched %d words in %d segments.", patchCount, affectedSegments.Count);
    }

    private void ClearJournal()
    {
        journal!.Seek(0, SeekOrigin.Begin);
        JournalWriteInt(0);
        journal.SetLength(4); // TODO: just trim to 0 instead?
        journal.Flush(true);
    }

    private void SaveJournal()
    {
        if (journal == null) OpenJournal(GetJournalFile());
        journal!.Seek(0, SeekOrigin.Begin);
        JournalWriteInt(1); // TODO
        JournalWriteLong(GetTimestamp());
        byte[] ba = new byte[4];
        Crc32 crc = new Crc32();
        foreach (TransactionBlock block in transactionBlocks!.Values)
        {
            int pCurrent = 0;
            NioBuffer original = GetMapping(SegmentOfPos(block.pos));
            NioBuffer current = block.current;
            int originalOfs = (int)(block.pos & 0x3fff_ffff);
            int pOriginal = originalOfs;
            for (; ; )
            {
                int oldValue = original.GetInt(pOriginal);
                int newValue = current.GetInt(pCurrent);
                if (oldValue != newValue)
                {
                    int pCurrentStart = pCurrent;
                    for (; ; )
                    {
                        pCurrent += 4;
                        pOriginal += 4;
                        if (pCurrent == 4096) break;
                        oldValue = original.GetInt(pOriginal);
                        newValue = current.GetInt(pCurrent);
                        if (oldValue == newValue) break;
                    }
                    long pos = (block.pos + pCurrentStart) << 8;
                    System.Diagnostics.Debug.Assert((pos & 0x3ff) == 0); // lower 10 bits must be clear
                    int len = (pCurrent - pCurrentStart) / 4;
                    System.Diagnostics.Debug.Assert(len > 0 && len <= 1024);
                    int patchLow = (int)pos | (len - 1);
                    int patchHigh = (int)((ulong)pos >> 32);
                    JournalWriteInt(patchLow);
                    JournalWriteInt(patchHigh);
                    IntToBytes(ba, patchLow);
                    crc.Append(ba);
                    IntToBytes(ba, patchHigh);
                    crc.Append(ba);

                    int pEnd = pCurrent;
                    int p = pCurrentStart;
                    p += originalOfs;
                    pEnd += originalOfs;
                    for (; p < pEnd; p += 4)
                    {
                        int v = original.GetInt(p);
                        JournalWriteInt(v);
                        IntToBytes(ba, v);
                        crc.Append(ba);
                    }
                }
                pCurrent += 4;
                if (pCurrent >= 4096) break;
                pOriginal += 4;
            }
        }
        JournalWriteInt(unchecked((int)0xffff_ffff));
        JournalWriteInt(unchecked((int)0xffff_ffff));
        JournalWriteInt((int)crc.GetCurrentHashAsUInt32());
        journal.Flush(true);
    }

    // === Transactions ===

    private void SyncSegments(HashSet<int> affectedSegments)
    {
        foreach (int segment in affectedSegments)
        {
            GetMapping(segment).Force();
        }
    }

    public void Close()
    {
        if (channel == null) return;

        try
        {
            long trueSize = GetTrueSize();
            bool journalPresent = false;
            string journalFile = GetJournalFile();
            if (journal != null)
            {
                journalPresent = true;
            }
            if (!journalPresent)
            {
                journalPresent = File.Exists(journalFile);
            }

            Lock(LOCK_NONE);
            bool segmentUnmapAttempted = false;

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
                        if (journal != null)
                        {
                            journal.Dispose();
                            journal = null;
                        }
                        File.Delete(journalFile);
                    }
                    if (trueSize > 0)
                    {
                        segmentUnmapAttempted = true;
                        if (UnmapSegments())
                        {
                            channel.SetLength(trueSize);
                        }
                    }
                    Lock(LOCK_NONE);
                }
            }
            if (!segmentUnmapAttempted) UnmapSegments();
            channel.Dispose();
        }
        catch (IOException ex)
        {
            throw new StoreException(
                string.Format(CultureInfo.InvariantCulture, "Error while closing file ({0})", ex.Message),
                path!, ex);
        }
        finally
        {
            channel = null;
            lockLevel = LOCK_NONE;
            lockReadHeld = false;
            lockWriteHeld = false;
            mappings = new MappedByteBuffer?[0];
            lock (openStores)
            {
                openStores.Remove(path!);
            }
        }
    }

    protected void BeginTransaction(int transactionLockLevel)
    {
        System.Diagnostics.Debug.Assert(transactionLockLevel == LOCK_APPEND || transactionLockLevel == LOCK_EXCLUSIVE);
        System.Threading.Monitor.Enter(transactionLock);
        try
        {
            Lock(transactionLockLevel);
            try
            {
                string journalFile = GetJournalFile();
                if (File.Exists(journalFile)) ProcessJournal(journalFile);
                preTransactionFileSize = GetTrueSize();
            }
            catch (Exception)
            {
                Lock(LOCK_READ);
                throw;
            }
        }
        catch (Exception)
        {
            System.Threading.Monitor.Exit(transactionLock);
            throw;
        }
        transactionBlocks = new Dictionary<long, TransactionBlock>();
    }

    protected void EndTransaction()
    {
        System.Diagnostics.Debug.Assert(IsInTransaction());
        System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(transactionLock));
        transactionBlocks = null;
        try
        {
            Lock(LOCK_READ);
        }
        finally
        {
            System.Threading.Monitor.Exit(transactionLock);
        }
    }

    protected bool IsInTransaction()
    {
        return transactionBlocks != null;
    }

    protected void Rollback()
    {
        System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(transactionLock));
        transactionBlocks!.Clear();
    }

    /// <summary>
    /// Returns the number of the segment in which the given file position is located.
    /// </summary>
    protected static int SegmentOfPos(long pos)
    {
        return (int)(pos >> 30);
    }

    protected void Commit()
    {
        System.Diagnostics.Debug.Assert(IsInTransaction());
        System.Diagnostics.Debug.Assert(System.Threading.Monitor.IsEntered(transactionLock));

        SaveJournal();

        HashSet<int> affectedSegments = new HashSet<int>();
        foreach (TransactionBlock block in transactionBlocks!.Values)
        {
            int segment = SegmentOfPos(block.pos);
            int ofs = (int)block.pos & 0x3fff_ffff;
            System.Diagnostics.Debug.Assert((ofs & 0xfff) == 0);
            System.Diagnostics.Debug.Assert(block.current.Array()!.Length == 4096);
            GetMapping(segment).Put(ofs, block.current.Array()!);
            affectedSegments.Add(segment);
        }

        long currentFileSize = GetTrueSize();
        if (currentFileSize > preTransactionFileSize)
        {
            int firstSegment = SegmentOfPos(preTransactionFileSize);
            int lastSegment = SegmentOfPos(currentFileSize - 1);
            for (int segment = firstSegment; segment <= lastSegment; segment++)
            {
                affectedSegments.Add(segment);
            }
        }

        SyncSegments(affectedSegments);

        ClearJournal();
    }

    protected NioBuffer GetBlock(long pos)
    {
        System.Diagnostics.Debug.Assert((pos & 0xfff) == 0,
            string.Format(CultureInfo.InvariantCulture, "{0}: Block must start at 4KB-aligned position", pos));

        if (pos < preTransactionFileSize)
        {
            transactionBlocks!.TryGetValue(pos, out TransactionBlock? block);
            if (block == null)
            {
                block = new TransactionBlock();
                block.pos = pos;
                NioBuffer original = GetMapping((int)(pos >> 30));
                int ofs = (int)pos & 0x3fff_ffff;
                byte[] copy = new byte[4096];
                original.Get(ofs, copy);
                block.current = NioBuffer.Wrap(copy);
                block.current.Order(original.Order()); // TODO
                transactionBlocks[pos] = block;
            }
            return block.current;
        }
        NioBuffer buf = GetMapping((int)(pos >> 30));
        ByteOrder order = buf.Order();
        buf = buf.Slice((int)pos & 0x3fff_ffff, 4096);
        buf.Order(order);
        return buf;
    }

    public long CurrentFileSize()
    {
        return new FileInfo(path!).Length;
    }

    // === Journal big-endian primitives (Java RandomAccessFile is big-endian) ===

    private void JournalWriteInt(int v)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(b, v);
        journal!.Write(b);
    }

    private void JournalWriteLong(long v)
    {
        Span<byte> b = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(b, v);
        journal!.Write(b);
    }

    private int JournalReadInt()
    {
        Span<byte> b = stackalloc byte[4];
        ReadFully(b);
        return BinaryPrimitives.ReadInt32BigEndian(b);
    }

    private long JournalReadLong()
    {
        Span<byte> b = stackalloc byte[8];
        ReadFully(b);
        return BinaryPrimitives.ReadInt64BigEndian(b);
    }

    private void ReadFully(Span<byte> dst)
    {
        int read = 0;
        while (read < dst.Length)
        {
            int n = journal!.Read(dst.Slice(read));
            if (n <= 0) throw new EndOfStreamException();
            read += n;
        }
    }

    // Marks a file as sparse on Windows so that mapping large segments does not
    // physically allocate them. No-op (and unnecessary) on other platforms.
    private static void MarkSparse(FileStream fs)
    {
        if (!OperatingSystem.IsWindows()) return;
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

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);
}
