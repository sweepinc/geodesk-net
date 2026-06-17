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
using Java.Nio;
using ByteOrder = Java.Nio.ByteOrder;
using NioBuffer = Java.Nio.ByteBuffer;

namespace Clarisma.Common.Store;

// A standalone, read-only store reader.
//
// PORT-LIMITATION (locking): Java acquires a *shared* byte-range lock. .NET has no shared
// byte-range locks, so locking here is a best-effort no-op (see Store for details).
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
                int activeSnapshot = buf.Get(ACTIVE_SNAPSHOT_OFS);
                // PORT-LIMITATION: shared lock not available; best-effort.
                TrySharedLock(LOCK_OFS + activeSnapshot * 2);
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

    private void TrySharedLock(long position)
    {
        // .NET FileStream.Lock is exclusive-only; a shared lock cannot be represented.
        // Best effort: attempt an exclusive lock but ignore contention (treat as held).
        try
        {
            channel!.Lock(position, 1);
        }
        catch (IOException)
        {
        }
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
        return baseMapping!.Get(ACTIVE_SNAPSHOT_OFS);
    }
}
