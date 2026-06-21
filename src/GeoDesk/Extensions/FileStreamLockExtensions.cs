/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;

namespace GeoDesk.Extensions;

/// <summary>
/// <see cref="FileStream"/> extensions providing cross-platform advisory byte-range locking, with
/// both <em>shared</em> (reader) and <em>exclusive</em> (writer) locks, in blocking and non-blocking
/// forms. This is the .NET stand-in for Java's <c>FileChannel.lock</c> / <c>tryLock(position, size,
/// shared)</c>: the BCL's <see cref="FileStream.Lock"/> can only take exclusive locks, but the
/// underlying OS primitives support shared byte-range locks.
/// </summary>
/// <remarks>
/// This file only dispatches by operating system; each platform back-end lives in its own
/// partial-class file and fully owns its return-code / errno interpretation:
/// <list type="bullet">
///   <item><c>FileStreamLockExtensions.Win32.cs</c> — <c>LockFileEx</c>/<c>UnlockFileEx</c>.</item>
///   <item><c>FileStreamLockExtensions.Linux.cs</c> — <c>fcntl</c> OFD locks (<c>F_OFD_SETLK</c>).</item>
///   <item><c>FileStreamLockExtensions.OSX.cs</c> — classic <c>fcntl</c> locks (macOS has no OFD).</item>
/// </list>
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
internal static partial class FileStreamLockExtensions
{

    /// <summary>
    /// The kind of byte-range lock request passed to the platform back-ends: a shared (reader)
    /// lock, an exclusive (writer) lock, or a release of an existing lock.
    /// </summary>
    // Lock request kind, common to all back-ends.
    internal enum LockOp { Shared, Exclusive, Unlock }

    // Shared POSIX errno / whence values used by both the Linux and macOS back-ends. (Windows uses
    // its own Win32 error codes.) These are identical on Linux and macOS; the values that differ
    // between them — EAGAIN, the F_* commands and lock types — live in the per-platform files.
    const int EINTR = 4;    // interrupted system call — retry the blocking fcntl
    const int EACCES = 13;  // range already locked (non-blocking request)
    const int SEEK_SET = 0;

    /// <summary>
    /// Attempts to acquire a byte-range lock on the stream's file without blocking — the analog of
    /// Java's <c>FileChannel.tryLock</c>. Returns <c>true</c> if granted, <c>false</c> if the range
    /// conflicts with a lock held elsewhere.
    /// </summary>
    public static bool TryLockRange(this FileStream stream, long position, long length, bool exclusive)
    {
        var op = exclusive ? LockOp.Exclusive : LockOp.Shared;

        if (OperatingSystem.IsWindows())
            return WindowsSetLock(stream.SafeFileHandle, position, length, op, wait: false);

        if (OperatingSystem.IsLinux())
            return LinuxSetLock(stream.SafeFileHandle, position, length, op, wait: false);

        throw new PlatformNotSupportedException("Byte-range file locking is not supported on this platform.");
    }

    /// <summary>
    /// Acquires a byte-range lock on the stream's file, <b>blocking</b> until it can be granted — the
    /// analog of Java's <c>FileChannel.lock</c>. (Throws on a genuine I/O error, not on contention.)
    /// </summary>
    public static void LockRange(this FileStream stream, long position, long length, bool exclusive)
    {
        var op = exclusive ? LockOp.Exclusive : LockOp.Shared;

        if (OperatingSystem.IsWindows())
        {
            WindowsSetLock(stream.SafeFileHandle, position, length, op, wait: true);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            LinuxSetLock(stream.SafeFileHandle, position, length, op, wait: true);
            return;
        }

        throw new PlatformNotSupportedException("Byte-range file locking is not supported on this platform.");
    }

    /// <summary>
    /// Releases a previously acquired lock on the given byte range. Best-effort: the underlying
    /// unlock is not guaranteed to report failure.
    /// </summary>
    public static void UnlockRange(this FileStream stream, long position, long length)
    {
        if (OperatingSystem.IsWindows())
        {
            WindowsSetLock(stream.SafeFileHandle, position, length, LockOp.Unlock, wait: false);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            LinuxSetLock(stream.SafeFileHandle, position, length, LockOp.Unlock, wait: false);
            return;
        }

        throw new PlatformNotSupportedException("Byte-range file locking is not supported on this platform.");
    }

}
