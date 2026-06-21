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

using GeoDesk.Buffers;
using GeoDesk.Extensions;

namespace GeoDesk.Common.Store;

/// <summary>
/// Read-only base for a memory-mapped, snapshot-based store file. Opens the file, takes a shared
/// byte-range lock on the active snapshot to coordinate with writers, and maps the file in fixed-size
/// segments on demand, exposing helpers to resolve the segment and offset of a given page.
/// </summary>
/// <remarks>
/// Ported from Java <c>com.clarisma.common.store.FreeStore</c>.
///
/// PORT NOTE (locking): Java acquires a *shared* byte-range lock on the active snapshot. The BCL's
/// <c>FileStream.Lock</c> is exclusive-only, so this uses the <c>FileStream.TryLockRange</c> extension,
/// which provides a real shared byte-range lock cross-platform (<c>LockFileEx</c> / <c>fcntl</c>).
/// See <see cref="Store"/> for details.
/// </remarks>
internal class FreeStore
{

    readonly string _path;
    FileStream? _channel;
    Segment?[] _mappings = new Segment?[0];
    protected Segment? baseMapping;
    long _fileSize;
    readonly object _mappingsLock = new object();
    int _pageSizeShift = 12; // 4KB default page

    protected const int SEGMENT_SIZE = 1 << 30;
    const int ACTIVE_SNAPSHOT_OFS = 16;
    const int LOCK_OFS = 512;

    /// <summary>
    /// Opens the store file at the given path read-only, validates and reads its header, takes a
    /// shared lock on the active snapshot (throwing <see cref="StoreException"/> if a writer holds it),
    /// maps the base segment, and runs subclass <see cref="Initialize"/>.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore(Path)</c>.</remarks>
    public FreeStore(string path)
    {
        try
        {
            _path = path;
            _channel = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var header = new byte[24];
            var buf = new NioBufferReader(header);
            for (; ; )
            {
                var pos = 0;
                while (pos < header.Length)
                {
                    _channel.Seek(pos, SeekOrigin.Begin);
                    var n = _channel.Read(header, pos, header.Length - pos);
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
                if (!_channel.TryLockRange(LOCK_OFS + activeSnapshot * 2, 1, exclusive: false))
                    throw new StoreException("Store locked", path);

                _fileSize = _channel.Length;
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

    /// <summary>
    /// Closes the store: unmaps all segments and disposes the underlying file channel. Safe to call
    /// when already closed.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore.close()</c>.</remarks>
    public void Close()
    {
        if (_channel == null)
            return;
        UnmapSegments();
        try
        {
            _channel.Dispose();
        }
        catch (IOException)
        {
            // ignore
        }
        _channel = null;
    }

    // === File Mapping ===

    // Non-null accessors that assert the store is open rather than suppressing nullability with `!`.
    /// <summary>
    /// The underlying file channel, throwing if the store has been closed.
    /// </summary>
    FileStream Channel => _channel ?? throw new InvalidOperationException("Store is not open");

    /// <summary>The base segment carrying the active-snapshot header. Valid once the store is open.</summary>
    protected Segment BaseMapping => baseMapping ?? throw new InvalidOperationException("Store is not open");

    // Maps one segment (read-only) as its own MemoryMappedFile + view, wrapped in a
    // Memory&lt;byte&gt;-backed NioBuffer wrapping a Segment. Segments are independent.
    /// <summary>
    /// Returns the read-only memory-mapped <see cref="Segment"/> with the given index, mapping it (and
    /// growing the mapping table) on first access. Thread-safe.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore.getMapping(int)</c>.</remarks>
    protected Segment GetMapping(int n)
    {
        lock (_mappingsLock)
        {
            if (n < _mappings.Length && _mappings[n] is { } existing)
                return existing;

            if (n >= _mappings.Length)
            {
                System.Array.Resize(ref _mappings, n + 1);
            }
            try
            {
                var mappingOfs = (long)n * SEGMENT_SIZE;
                var mappingSize = checked((int)System.Math.Min(_fileSize - mappingOfs, SEGMENT_SIZE));
                var mmf = MemoryMappedFile.CreateFromFile(Channel, null, 0, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true);
                var view = mmf.CreateViewAccessor(mappingOfs, mappingSize, MemoryMappedFileAccess.Read);
                var seg = new Segment(mmf, view, mappingSize);
                _mappings[n] = seg;
                return seg;
            }
            catch (IOException ex)
            {
                throw new StoreException(string.Format(CultureInfo.InvariantCulture, "{0}: Failed to map segment at {1:X} ({2})", _path, (long)n * SEGMENT_SIZE, ex.Message), ex);
            }
        }
    }

    /// <summary>
    /// Disposes and clears all mapped segments. Thread-safe.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore.unmapSegments()</c>.</remarks>
    bool UnmapSegments()
    {
        lock (_mappingsLock)
        {
            foreach (var b in _mappings)
                ((IDisposable?)b)?.Dispose();
            _mappings = new Segment?[0];
            return true;
        }
    }

    /// <summary>
    /// Subclass extension point invoked at the end of construction; the base implementation does nothing.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore.initialize()</c>.</remarks>
    protected virtual void Initialize()
    {
        // do nothing
    }

    /// <summary>
    /// Returns the mapped segment that contains the given page, derived from the page number and the
    /// page-size shift.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore.bufferOfPage(int)</c>.</remarks>
    internal Segment SegmentOfPage(int page)
    {
        return GetMapping(page >> (30 - _pageSizeShift));
    }

    /// <summary>
    /// Returns the byte offset of the given page within its containing segment.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore.offsetOfPage(int)</c>.</remarks>
    public int OffsetOfPage(int page)
    {
        return (page << _pageSizeShift) & 0x3fff_ffff;
    }

    /// <summary>
    /// Returns the index of the currently active snapshot, read from the base segment's header.
    /// </summary>
    /// <remarks>Ported from Java <c>com.clarisma.common.store.FreeStore.activeSnapshot()</c>.</remarks>
    public int ActiveSnapshot()
    {
        // signed byte, matching Java's ByteBuffer.get(int) (see the constructor's lock computation)
        return (sbyte)BaseMapping.Memory.Span[ACTIVE_SNAPSHOT_OFS];
    }

}
