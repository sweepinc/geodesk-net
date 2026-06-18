/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using GeoDesk.Extensions;
using Java.Nio;
using ByteOrder = Java.Nio.ByteOrder;
using NioBuffer = Java.Nio.ByteBuffer;

namespace Clarisma.Common.Store;

// A standalone, read-only store reader.
//
// Locking: Java acquires a *shared* byte-range lock on the active snapshot. The BCL's
// FileStream.Lock is exclusive-only, so this uses the FileStream.TryLockRange extension, which
// provides a real shared byte-range lock cross-platform (LockFileEx / fcntl). See Store for details.
/// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore</c>.</remarks>
public class FreeStore
{
    private readonly string path;
    private FileStream? channel;
    private MemoryMappedFile? mmf;
    private MappedByteBuffer?[] mappings = new MappedByteBuffer?[0];
    protected MappedByteBuffer? baseMapping;
    private long fileSize;
    private readonly object mappingsLock = new object();
    private int pageSizeShift = 12; // 4KB default page

    protected const int SEGMENT_SIZE = 1 << 30;
    private const int ACTIVE_SNAPSHOT_OFS = 16;
    private const int LOCK_OFS = 512;

    public FreeStore(string path)
    {
        try
        {
            this.path = path;
            channel = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            byte[] header = new byte[24];
            NioBuffer buf = NioBuffer.Wrap(header).Order(ByteOrder.LittleEndian);
            for (; ; )
            {
                int pos = 0;
                while (pos < header.Length)
                {
                    channel.Seek(pos, SeekOrigin.Begin);
                    int n = channel.Read(header, pos, header.Length - pos);
                    if (n < 0)
                    {
                        throw new StoreException("Invalid store", path);
                    }
                    pos += n;
                }
                // Java's ByteBuffer.get(int) returns a *signed* byte; cast to sbyte so the lock
                // position matches Java exactly (the port's ByteBuffer.Get returns an unsigned byte).
                int activeSnapshot = (sbyte)buf.Get(ACTIVE_SNAPSHOT_OFS);
                // Shared (reader) byte-range lock on the active snapshot, mirroring Java's
                // channel.tryLock(LOCK_OFS + activeSnapshot * 2, 1, true). A shared lock is compatible
                // with other readers but excludes a writer/compactor recycling the snapshot; if it
                // cannot be acquired, a writer holds it exclusively, so the store is locked. (A true
                // shared lock is essential here: the FileStream is buffered, so the small header read
                // pulls in a block overlapping this byte — under an *exclusive* lock that read would
                // fail. FileStream.TryLockRange supplies a real shared lock via LockFileEx / fcntl.)
                if (!channel.TryLockRange(LOCK_OFS + activeSnapshot * 2, 1, exclusive: false))
                {
                    throw new StoreException("Store locked", path);
                }
                fileSize = channel.Length;
                baseMapping = GetMapping(0);

                break;
            }
        }
        catch (IOException ex)
        {
            throw new StoreException("Failed to open store", path, ex);
        }
        Initialize();
    }

    public void Close()
    {
        if (channel == null) return;
        UnmapSegments();
        try
        {
            channel.Dispose();
        }
        catch (IOException)
        {
            // ignore
        }
        channel = null;
    }

    // === File Mapping ===

    protected MappedByteBuffer GetMapping(int n)
    {
        lock (mappingsLock)
        {
            if (n < mappings.Length && mappings[n] != null) return mappings[n]!;

            if (mmf == null)
            {
                mmf = MemoryMappedFile.CreateFromFile(channel!, null, 0,
                    MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true);
            }
            if (n >= mappings.Length)
            {
                System.Array.Resize(ref mappings, n + 1);
            }
            try
            {
                long mappingOfs = (long)n * SEGMENT_SIZE;
                long mappingSize = System.Math.Min(fileSize - mappingOfs, SEGMENT_SIZE);
                var accessor = mmf.CreateViewAccessor(mappingOfs, mappingSize, MemoryMappedFileAccess.Read);
                var buf = new MappedByteBuffer(accessor, (int)mappingSize);
                buf.Order(ByteOrder.LittleEndian); // TODO: check!
                mappings[n] = buf;
                return buf;
            }
            catch (IOException ex)
            {
                throw new StoreException(
                    string.Format(CultureInfo.InvariantCulture, "{0}: Failed to map segment at {1:X} ({2})",
                        path, (long)n * SEGMENT_SIZE, ex.Message), ex);
            }
        }
    }

    private bool UnmapSegments()
    {
        lock (mappingsLock)
        {
            foreach (MappedByteBuffer? b in mappings) b?.Dispose();
            mappings = new MappedByteBuffer?[0];
            mmf?.Dispose();
            mmf = null;
            return true;
        }
    }

    protected virtual void Initialize()
    {
        // do nothing
    }

    public NioBuffer BufferOfPage(int page)
    {
        return GetMapping(page >> (30 - pageSizeShift));
    }

    public int OffsetOfPage(int page)
    {
        return (page << pageSizeShift) & 0x3fff_ffff;
    }

    public int ActiveSnapshot()
    {
        // signed byte, matching Java's ByteBuffer.get(int) (see the constructor's lock computation)
        return (sbyte)baseMapping!.Get(ACTIVE_SNAPSHOT_OFS);
    }
}
