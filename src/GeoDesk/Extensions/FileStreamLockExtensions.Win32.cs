/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

using Microsoft.Win32.SafeHandles;

namespace GeoDesk.Extensions;

// Windows back-end: LockFileEx / UnlockFileEx. Locks are owned by the file handle, which matches the
// per-descriptor semantics the Unix back-ends emulate with OFD / handle-once locks.
/// <summary>
/// Windows back-end of the byte-range file locking extensions, implemented via
/// <c>LockFileEx</c> / <c>UnlockFileEx</c> on the file handle.
/// </summary>
internal static partial class FileStreamLockExtensions
{

    const uint LOCKFILE_FAIL_IMMEDIATELY = 0x1;
    const uint LOCKFILE_EXCLUSIVE_LOCK = 0x2;
    const int ERROR_LOCK_VIOLATION = 33;

    /// <summary>
    /// Applies, blocks for, or releases a byte-range lock on the given file handle via
    /// <c>LockFileEx</c>/<c>UnlockFileEx</c>. Returns true when granted or released, false when a
    /// non-blocking request conflicts, and throws <see cref="IOException"/> on any other failure.
    /// </summary>
    [SupportedOSPlatform("windows")]
    static bool WindowsSetLock(SafeFileHandle handle, long position, long length, LockOp op, bool wait)
    {
        var overlapped = MakeOverlapped(position);
        if (op == LockOp.Unlock)
        {
            UnlockFileEx(handle, 0, Low(length), High(length), ref overlapped); // best-effort
            return true;
        }

        // Omitting LOCKFILE_FAIL_IMMEDIATELY makes LockFileEx block until the lock can be acquired.
        uint flags = (op == LockOp.Exclusive ? LOCKFILE_EXCLUSIVE_LOCK : 0u) | (wait ? 0u : LOCKFILE_FAIL_IMMEDIATELY);
        if (LockFileEx(handle, flags, 0, Low(length), High(length), ref overlapped))
            return true;

        int err = Marshal.GetLastPInvokeError();
        if (!wait && err == ERROR_LOCK_VIOLATION)
            return false; // conflict (only in the fail-immediately form)

        throw new IOException($"LockFileEx failed (error {err})");
    }

    /// <summary>
    /// Builds a <see cref="NativeOverlapped"/> carrying the 64-bit lock offset split into its low and
    /// high 32-bit halves, as <c>LockFileEx</c> expects.
    /// </summary>
    static NativeOverlapped MakeOverlapped(long position) => new NativeOverlapped
    {
        OffsetLow = unchecked((int)(position & 0xffff_ffff)),
        OffsetHigh = unchecked((int)(position >> 32)),
    };

    /// <summary>
    /// Returns the low 32 bits of a 64-bit value.
    /// </summary>
    static uint Low(long v) => unchecked((uint)(v & 0xffff_ffff));

    /// <summary>
    /// Returns the high 32 bits of a 64-bit value.
    /// </summary>
    static uint High(long v) => unchecked((uint)(v >> 32));

    /// <summary>
    /// P/Invoke into the Win32 <c>LockFileEx</c> to acquire a byte-range lock on a file handle.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool LockFileEx(SafeFileHandle hFile, uint dwFlags, uint dwReserved, uint nNumberOfBytesToLockLow, uint nNumberOfBytesToLockHigh, ref NativeOverlapped lpOverlapped);

    /// <summary>
    /// P/Invoke into the Win32 <c>UnlockFileEx</c> to release a byte-range lock on a file handle.
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool UnlockFileEx(SafeFileHandle hFile, uint dwReserved, uint nNumberOfBytesToUnlockLow, uint nNumberOfBytesToUnlockHigh, ref NativeOverlapped lpOverlapped);

}
