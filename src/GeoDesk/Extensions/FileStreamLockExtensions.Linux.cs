/*
 * Copyright (c) Clarisma / GeoDesk contributors
 *
 * This source code is licensed under the Apache 2.0 license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using Microsoft.Win32.SafeHandles;

namespace GeoDesk.Extensions;

// Linux back-end: fcntl with OFD locks (F_OFD_SETLK / F_OFD_SETLKW), so a lock is owned by the open
// file description (the descriptor) rather than the process — handle-scoped semantics matching
// Windows LockFileEx.
//
//   struct flock { short l_type; short l_whence; off_t l_start; off_t l_len; pid_t l_pid; }
//
// fcntl is a C variadic function (int fcntl(int, int, ...)), but a fixed-signature P/Invoke works on
// Linux: both x86-64 and AArch64/Linux pass the first integer/pointer arguments in registers whether
// the callee is variadic or not, so the flock pointer lands where fcntl expects it. (This is NOT true
// on Apple-silicon macOS — see the OSX back-end.) CoreCLR does not support vararg (__arglist) P/Invoke,
// so that is not an option.
internal static partial class FileStreamLockExtensions
{

    const int EAGAIN_LINUX = 11; // range already locked (non-blocking request)

    const int F_OFD_SETLK_LINUX = 37;
    const int F_OFD_SETLKW_LINUX = 38;
    const short F_RDLCK_LINUX = 0;
    const short F_WRLCK_LINUX = 1;
    const short F_UNLCK_LINUX = 2;

    [StructLayout(LayoutKind.Sequential)]
    struct FlockLinux
    {

        public short l_type;
        public short l_whence;
        public long l_start;
        public long l_len;
        public int l_pid; // must be 0 for OFD locks

    }

    [SupportedOSPlatform("linux")]
    static bool LinuxSetLock(SafeFileHandle handle, long position, long length, LockOp op, bool wait)
    {
        var refAdded = false;

        try
        {
            handle.DangerousAddRef(ref refAdded);
            int fd = (int)handle.DangerousGetHandle();
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

            int cmd = wait ? F_OFD_SETLKW_LINUX : F_OFD_SETLK_LINUX;
            for (; ; )
            {
                int rc = FcntlLinux(fd, cmd, ref fl);
                if (rc == 0)
                    return true; // granted (or released)

                int errno = Marshal.GetLastPInvokeError();
                if (errno == EINTR)
                    continue; // interrupted by a signal — retry the blocking call

                if (!wait && (errno == EAGAIN_LINUX || errno == EACCES))
                    return false; // conflict

                throw new IOException($"fcntl failed (errno {errno}) [fd={fd} op={op} wait={wait} position={position} length={length}]");
            }
        }
        finally
        {
            if (refAdded)
                handle.DangerousRelease();
        }
    }

    [DllImport("libc", SetLastError = true, EntryPoint = "fcntl")]
    static extern int FcntlLinux(int fd, int cmd, ref FlockLinux lockArg);

}
