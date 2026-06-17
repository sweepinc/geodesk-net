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

namespace Clarisma.Common.Store;

/// <summary>
/// Cross-platform advisory byte-range file locking, supporting both <em>shared</em> (reader) and
/// <em>exclusive</em> (writer) locks. This is the .NET stand-in for Java's
/// <c>FileChannel.lock(position, size, shared)</c>: the BCL's <see cref="FileStream.Lock"/> can only
/// take exclusive locks, but the underlying OS primitives support shared byte-range locks —
/// <c>LockFileEx</c> on Windows and <c>fcntl(F_SETLK)</c> on POSIX.
/// </summary>
/// <remarks>
/// All locks are non-blocking (try-lock): <see cref="TryLock"/> returns <c>false</c> when the range
/// conflicts with a lock held elsewhere, rather than waiting. Locks are advisory and tied to the
/// file handle; closing the handle releases them.
///
/// <para>POSIX note: the <c>struct flock</c> field order and the <c>F_*LCK</c> / <c>F_SETLK</c>
/// constants differ between Linux and macOS, so each has its own marshalled layout below. On Apple
/// Silicon the C variadic ABI passes the <c>flock*</c> on the stack; callers that must not silently
/// lose a lock should treat a thrown <see cref="IOException"/> accordingly — the read path here uses
/// the lock best-effort.</para>
/// </remarks>
internal static class FileLock
{

    /// <summary>
    /// Attempts to acquire a byte-range lock without blocking. Returns <c>true</c> if the lock was
    /// granted, <c>false</c> if the range conflicts with a lock held by another handle/process.
    /// </summary>
    public static bool TryLock(SafeFileHandle handle, long position, long length, bool exclusive)
    {
        if (OperatingSystem.IsWindows()) return WindowsLock(handle, position, length, exclusive);
        return UnixLock(handle, position, length, exclusive ? LockOp.Exclusive : LockOp.Shared);
    }

    /// <summary>Releases a previously acquired lock on the given range (best-effort).</summary>
    public static void Release(SafeFileHandle handle, long position, long length)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsUnlock(handle, position, length);
            return;
        }
        UnixLock(handle, position, length, LockOp.Unlock);
    }

    private enum LockOp { Shared, Exclusive, Unlock }

    // === Windows: LockFileEx / UnlockFileEx ===

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

    // === POSIX: fcntl(F_SETLK) with struct flock ===

    // errno values that mean "range is already locked" for a non-blocking F_SETLK.
    private const int EACCES = 13;
    private const int EAGAIN_LINUX = 11;
    private const int EAGAIN_MACOS = 35;

    private const int SEEK_SET = 0;

    private static bool UnixLock(SafeFileHandle handle, long position, long length, LockOp op)
    {
        bool refAdded = false;
        try
        {
            handle.DangerousAddRef(ref refAdded);
            int fd = (int)handle.DangerousGetHandle();
            int rc = OperatingSystem.IsMacOS()
                ? MacSetLock(fd, position, length, op)
                : LinuxSetLock(fd, position, length, op);
            if (rc == 0) return true;
            int errno = Marshal.GetLastPInvokeError();
            if (errno == EACCES || errno == EAGAIN_LINUX || errno == EAGAIN_MACOS) return false;
            throw new IOException($"fcntl(F_SETLK) failed (errno {errno})");
        }
        finally
        {
            if (refAdded) handle.DangerousRelease();
        }
    }

    // --- Linux: struct flock = { short l_type; short l_whence; off_t l_start; off_t l_len; pid_t l_pid; }

    private const int F_SETLK_LINUX = 6;
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
        public int l_pid;
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
        return FcntlLinux(fd, F_SETLK_LINUX, ref fl);
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "fcntl")]
    private static extern int FcntlLinux(int fd, int cmd, ref FlockLinux lockArg);

    // --- macOS: struct flock = { off_t l_start; off_t l_len; pid_t l_pid; short l_type; short l_whence; }

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
