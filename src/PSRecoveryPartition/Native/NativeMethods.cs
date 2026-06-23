using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// P/Invoke surface for the Win32 disk, volume, and file-system APIs the
    /// module relies on. All entry points are <c>internal</c> and consumed
    /// through purpose-built wrappers (DeviceHandleFactory, Win32DiskInfoReader,
    /// Win32PartitionWriter) rather than called directly from cmdlets.
    /// </summary>
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFileW(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            int nInBufferSize,
            IntPtr lpOutBuffer,
            int nOutBufferSize,
            out int lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        // Volume enumeration (FindFirstVolumeW / FindNextVolumeW / FindVolumeClose).
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindFirstVolumeW(
            [Out] System.Text.StringBuilder lpszVolumeName,
            int cchBufferLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindNextVolumeW(
            IntPtr hFindVolume,
            [Out] System.Text.StringBuilder lpszVolumeName,
            int cchBufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindVolumeClose(IntPtr hFindVolume);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVolumeInformationW(
            string lpRootPathName,
            [Out] System.Text.StringBuilder lpVolumeNameBuffer,
            int nVolumeNameSize,
            out uint lpVolumeSerialNumber,
            out uint lpMaximumComponentLength,
            out uint lpFileSystemFlags,
            [Out] System.Text.StringBuilder lpFileSystemNameBuffer,
            int nFileSystemNameSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetVolumePathNamesForVolumeNameW(
            string lpszVolumeName,
            [Out] char[] lpszVolumePathNames,
            int cchBufferLength,
            out int lpcchReturnLength);

        // Mount-point management. Path arguments must be backslash-terminated;
        // SetVolumeMountPointW requires the volume name in \\?\Volume{guid}\ form.
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetVolumeMountPointW(
            string lpszVolumeMountPoint,
            string lpszVolumeName);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteVolumeMountPointW(string lpszVolumeMountPoint);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetVolumeLabelW(string lpRootPathName, string lpVolumeName);

        // Bitmask of in-use DOS drive letters (bit 0 = A:, bit 25 = Z:). Used to
        // pick a free letter when fmifs!FormatEx needs a DOS root because it
        // does not accept \\?\Volume{guid}\ device paths.
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetLogicalDrives();

        // fmifs!FormatEx is the inbox formatting entry point used by
        // format.com. It is undocumented but ABI-stable since NT 4.0. Callers
        // supply a callback that receives progress and status notifications.
        public delegate bool FormatExCallback(int command, int subAction, IntPtr argument);

        [DllImport("fmifs.dll", CharSet = CharSet.Unicode)]
        public static extern void FormatEx(
            string driveRoot,
            int mediaType,
            string fileSystem,
            string label,
            [MarshalAs(UnmanagedType.Bool)] bool quickFormat,
            int clusterSize,
            FormatExCallback callback);
    }
}
