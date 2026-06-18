using System;
using System.IO;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
struct FlockLinux { public short l_type; public short l_whence; public long l_start; public long l_len; public int l_pid; }

static class P {
    [DllImport("libc", SetLastError = true, EntryPoint = "fcntl")]
    public static extern int FcntlRef(int fd, int cmd, ref FlockLinux lockArg);

    static void Try(string label, int fd, int cmd, short type, long start, long len) {
        var fl = new FlockLinux { l_type = type, l_whence = 0, l_start = start, l_len = len, l_pid = 0 };
        int rc = FcntlRef(fd, cmd, ref fl);
        int e = Marshal.GetLastPInvokeError();
        Console.WriteLine($"{label}: rc={rc} errno={e}");
    }

    static void Main() {
        var path = "/tmp/flock_target.bin";
        File.WriteAllBytes(path, new byte[4096]);

        Console.WriteLine("== fd opened O_RDONLY (FileAccess.Read) ==");
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
            int fd = (int)fs.SafeFileHandle.DangerousGetHandle();
            Try("OFD  F_RDLCK (37)", fd, 37, 0, 512, 1);
            Try("OFD  F_WRLCK (37)", fd, 37, 1, 0, 4);
            Try("OFD  F_WRLCK wait (38)", fd, 38, 1, 4, 4);
        }
        Console.WriteLine("== fd opened O_RDWR (FileAccess.ReadWrite) ==");
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) {
            int fd = (int)fs.SafeFileHandle.DangerousGetHandle();
            Try("OFD  F_WRLCK (37)", fd, 37, 1, 0, 4);
        }
    }
}
