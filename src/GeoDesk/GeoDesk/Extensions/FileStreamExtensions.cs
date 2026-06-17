/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace GeoDesk.Extensions;

/// <summary>
/// <see cref="FileStream"/> extensions providing cross-platform advisory byte-range locking, with
/// both <em>shared</em> (reader) and <em>exclusive</em> (writer) locks. This is the .NET stand-in
/// for Java's <c>FileChannel.lock(position, size, shared)</c>: the BCL's <see cref="FileStream.Lock"/>
/// can only take exclusive locks, but the underlying OS primitives support shared byte-range locks.
/// </summary>
/// <remarks>
/// Backends: <c>LockFileEx</c>/<c>UnlockFileEx</c> on Windows; <c>fcntl</c> with <c>struct flock</c>
/// on POSIX.
///
/// <para><b>Lock ownership.</b> Windows <c>LockFileEx</c> locks belong to the file <i>handle</i>:
/// separate opens conflict even within one process, and a lock is released when its handle closes.
/// Classic POSIX <c>fcntl(F_SETLK)</c> locks instead belong to the <i>(process, inode)</i> pair —
/// opens within the same process never conflict, and closing <i>any</i> descriptor to the file drops
/// <i>all</i> of the process's locks on it, so using them per-stream is unsafe unless taken once per
/// process. To get handle-scoped semantics that match Windows, Linux uses <b>OFD locks</b>
/// (<c>F_OFD_SETLK</c>), which are owned by the open file description (the descriptor) rather than the
/// process. macOS has no OFD locks, so it falls back to classic per-process <c>fcntl</c> locks.</para>
/// </remarks>
public static class FileStreamExtensions
{

    /// <summary>
    /// Attempts to acquire a byte-range lock on the stream's file without blocking. Returns
    /// <c>true</c> if granted, <c>false</c> if the range conflicts with a lock held elsewhere.
    /// </summary>
    public static bool TryLockRange(this FileStream stream, long position, long length, bool exclusive)
    {
        var handle = stream.SafeFileHandle;
        if (OperatingSystem.IsWindows()) return WindowsLock(handle, position, length, exclusive);
        return UnixSetLock(handle, position, length, exclusive ? LockOp.Exclusive : LockOp.Shared);
    }

    /// <summary>Releases a previously acquired lock on the given range (best-effort).</summary>
    public static void UnlockRange(this FileStream stream, long position, long length)
    {
        var handle = stream.SafeFileHandle;
        if (OperatingSystem.IsWindows())
        {
            WindowsUnlock(handle, position, length);
            return;
        }
        UnixSetLock(handle, position, length, LockOp.Unlock);
    }

    private enum LockOp { Shared, Exclusive, Unlock }

    // === Windows: LockFileEx / UnlockFileEx (per-handle) ===

    private const uint LOCKFILE_FAIL_IMMEDIATELY = 0x1;
    private const uint LOCKFILE_EXCLUSIVE_LOCK = 0x2;
    private const int ERROR_LOCK_VIOLATION = 33;

    [SupportedOSPlatform("windows")]
    private static bool WindowsLock(SafeFileHandle handle, long position, long length, bool exclusive)
    {
        var overlapped = MakeOverlapped(position);
        uint flags = LOCKFILE_FAIL_IMMEDIATELY | (exclusive ? LOCKFILE_EXCLUSIVE_LOCK : 0u);
        if (LockFileEx(handle, flags, 0, Low(length), High(length), ref overlapped)) return true;
        int err = Marshal.GetLastPInvokeError();
        if (err == ERROR_LOCK_VIOLATION) return false;
        throw new IOException($"LockFileEx failed (error {err})");
    }

    [SupportedOSPlatform("windows")]
    private static void WindowsUnlock(SafeFileHandle handle, long position, long length)
    {
        var overlapped = MakeOverlapped(position);
        UnlockFileEx(handle, 0, Low(length), High(length), ref overlapped); // best-effort
    }

    private static NativeOverlapped MakeOverlapped(long position) => new NativeOverlapped
    {
        OffsetLow = unchecked((int)(position & 0xffff_ffff)),
        OffsetHigh = unchecked((int)(position >> 32)),
    };

    private static uint Low(long v) => unchecked((uint)(v & 0xffff_ffff));
    private static uint High(long v) => unchecked((uint)(v >> 32));

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LockFileEx(SafeFileHandle hFile, uint dwFlags, uint dwReserved,
        uint nNumberOfBytesToLockLow, uint nNumberOfBytesToLockHigh, ref NativeOverlapped lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnlockFileEx(SafeFileHandle hFile, uint dwReserved,
        uint nNumberOfBytesToUnlockLow, uint nNumberOfBytesToUnlockHigh, ref NativeOverlapped lpOverlapped);

    // === POSIX: fcntl with struct flock ===

    // errno values that mean "range is already locked" for a non-blocking lock request.
    private const int EACCES = 13;
    private const int EAGAIN_LINUX = 11;
    private const int EAGAIN_MACOS = 35;

    private const int SEEK_SET = 0;

    private static bool UnixSetLock(SafeFileHandle handle, long position, long length, LockOp op)
    {
        bool refAdded = false;
        try
        {
            handle.DangerousAddRef(ref refAdded);
            int fd = (int)handle.DangerousGetHandle();
            int rc = OperatingSystem.IsMacOS()
                ? MacSetLock(fd, position, length, op)
                : LinuxSetLock(fd, position, length, op);
            if (rc == 0) return true; // granted (or released)
            int errno = Marshal.GetLastPInvokeError();
            if (errno == EACCES || errno == EAGAIN_LINUX || errno == EAGAIN_MACOS) return false; // conflict
            throw new IOException($"fcntl failed (errno {errno})");
        }
        finally
        {
            if (refAdded) handle.DangerousRelease();
        }
    }

    // --- Linux: struct flock = { short l_type; short l_whence; off_t l_start; off_t l_len; pid_t l_pid; }
    //     Uses OFD locks (F_OFD_SETLK) so locks are owned by the descriptor, not the process.

    private const int F_SETLK_LINUX = 6;
    private const int F_OFD_SETLK_LINUX = 37;
    private const short F_RDLCK_LINUX = 0;
    private const short F_WRLCK_LINUX = 1;
    private const short F_UNLCK_LINUX = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct FlockLinux
    {
        public short l_type;
        public short l_whence;
        public long l_start;
        public long l_len;
        public int l_pid; // must be 0 for OFD locks
    }

    private static int LinuxSetLock(int fd, long position, long length, LockOp op)
    {
        var fl = new FlockLinux
        {
            l_type = op switch
            {
                LockOp.Shared => F_RDLCK_LINUX,
                LockOp.Exclusive => F_WRLCK_LINUX,
                _ => F_UNLCK_LINUX,
            },
            l_whence = SEEK_SET,
            l_start = position,
            l_len = length,
        };
        // OFD locks (per descriptor) on Linux; classic per-process F_SETLK on other SysV-like Unix.
        int cmd = OperatingSystem.IsLinux() ? F_OFD_SETLK_LINUX : F_SETLK_LINUX;
        return FcntlLinux(fd, cmd, ref fl);
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "fcntl")]
    private static extern int FcntlLinux(int fd, int cmd, ref FlockLinux lockArg);

    // --- macOS: struct flock = { off_t l_start; off_t l_len; pid_t l_pid; short l_type; short l_whence; }
    //     No OFD locks available; classic per-process F_SETLK.

    private const int F_SETLK_MACOS = 8;
    private const short F_RDLCK_MACOS = 1;
    private const short F_UNLCK_MACOS = 2;
    private const short F_WRLCK_MACOS = 3;

    [StructLayout(LayoutKind.Sequential)]
    private struct FlockMacOs
    {
        public long l_start;
        public long l_len;
        public int l_pid;
        public short l_type;
        public short l_whence;
    }

    private static int MacSetLock(int fd, long position, long length, LockOp op)
    {
        var fl = new FlockMacOs
        {
            l_start = position,
            l_len = length,
            l_type = op switch
            {
                LockOp.Shared => F_RDLCK_MACOS,
                LockOp.Exclusive => F_WRLCK_MACOS,
                _ => F_UNLCK_MACOS,
            },
            l_whence = SEEK_SET,
        };
        return FcntlMacOs(fd, F_SETLK_MACOS, ref fl);
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "fcntl")]
    private static extern int FcntlMacOs(int fd, int cmd, ref FlockMacOs lockArg);

}
