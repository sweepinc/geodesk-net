/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace GeoDesk.Common.Store;

/// <summary>
/// One mapped 1 GB file view — the unit the store maps the file in. A <see cref="MemoryManager{Byte}"/>
/// that owns the <see cref="MemoryMappedFile"/> and its view and exposes them as a single, writable
/// <see cref="System.Memory{Byte}"/> window via the inherited <see cref="MemoryManager{T}.Memory"/>;
/// <see cref="Dispose()"/> unmaps. Typed access is the job of the cursor structs / <c>NioBuffer</c> over
/// that <c>Memory</c> (via <c>BufferExtensions</c>) — there is deliberately no ByteBuffer-shaped get/put
/// API here. (A read-only consumer such as <c>NioBuffer</c> simply takes the window as
/// <see cref="System.ReadOnlyMemory{Byte}"/>.)
/// </summary>
/// <remarks>Merges the former <c>MappedFileMemoryManager</c> with the store's window type — it both owns
/// the mapped view and is the unit the store keeps. Store data is little-endian; the cursors decode it.
/// (The crash-recovery journal is big-endian but never goes through a Segment — see
/// <c>Store.JournalReadInt/JournalWriteInt</c>.)</remarks>
sealed unsafe class Segment : MemoryManager<byte>
{

    readonly MemoryMappedFile _file;
    readonly MemoryMappedViewAccessor _view;
    readonly byte* _addr;
    readonly int _length; // the logical window length (the view's ByteLength may be rounded up larger)

    /// <summary>
    /// Initializes a new instance
    /// </summary>
    /// <param name="file"></param>
    /// <param name="view"></param>
    /// <param name="length"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public Segment(MemoryMappedFile file, MemoryMappedViewAccessor view, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        _file = file ?? throw new ArgumentNullException(nameof(file));
        _view = view ?? throw new ArgumentNullException(nameof(view));

        if (_file.SafeMemoryMappedFileHandle.IsClosed || _file.SafeMemoryMappedFileHandle.IsInvalid)
            throw new InvalidOperationException();
        if (_view.SafeMemoryMappedViewHandle.IsClosed || _view.SafeMemoryMappedViewHandle.IsInvalid)
            throw new InvalidOperationException();

        _length = length;
        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref _addr);
    }

    /// <inheritdoc />
    public override Span<byte> GetSpan() => new Span<byte>(_addr, _length);

    /// <inheritdoc />
    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if (elementIndex < 0 || elementIndex >= _length)
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        return new MemoryHandle(Unsafe.Add<byte>(_addr, elementIndex));
    }

    /// <inheritdoc />
    public override void Unpin()
    {

    }

    /// <summary>Flushes pending writes through to the underlying file (Java's <c>MappedByteBuffer.force()</c>).</summary>
    public void Force() => _view.Flush();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _view.SafeMemoryMappedViewHandle.ReleasePointer();
            _view.Dispose();
            _file.Dispose();
        }
    }

}
