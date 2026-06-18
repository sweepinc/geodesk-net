using System;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace GeoDesk.Buffers;

/// <summary>
/// A <see cref="MemoryManager{Byte}"/> over a region of a memory mapped file. This <see cref="MemoryManager{Byte}"/>
/// becomes owner of the file and view, disposing it upon disposal of itself.
/// </summary>
sealed unsafe class MappedFileMemoryManager : MemoryManager<byte>
{

    readonly MemoryMappedFile _file;
    readonly MemoryMappedViewAccessor _view;
    readonly byte* _addr;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="view"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public MappedFileMemoryManager(MemoryMappedFile file, MemoryMappedViewAccessor view)
    {
        _file = file ?? throw new ArgumentNullException(nameof(file));
        _view = view ?? throw new ArgumentNullException(nameof(view));

        if (_file.SafeMemoryMappedFileHandle.IsClosed || _file.SafeMemoryMappedFileHandle.IsInvalid)
            throw new InvalidOperationException();
        if (_view.SafeMemoryMappedViewHandle.IsClosed || _view.SafeMemoryMappedViewHandle.IsInvalid)
            throw new InvalidOperationException();

        _view.SafeMemoryMappedViewHandle.AcquirePointer(ref _addr);
    }

    /// <inheritdoc />
    public override Span<byte> GetSpan()
    {
        var len = _view.SafeMemoryMappedViewHandle.ByteLength;
        return new Span<byte>(_addr, checked((int)len));
    }

    /// <inheritdoc />
    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if (elementIndex < 0 || elementIndex >= checked((int)_view.SafeMemoryMappedViewHandle.ByteLength))
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        return new MemoryHandle(Unsafe.Add<byte>(_addr, elementIndex));
    }

    /// <inheritdoc />
    public override void Unpin()
    {

    }

    /// <summary>
    /// Flushes any changes to the memory mapped view to the underlying file. Note that this is not necessary to call, as the view will
    /// flush itself when it is disposed. However, if you want to ensure that changes are flushed before disposing the view, you can call
    /// this method.
    /// </summary>
    public void Flush()
    {
        _view.Flush();
    }

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
