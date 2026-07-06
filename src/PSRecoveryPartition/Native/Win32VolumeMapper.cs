using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Enumerates Windows volumes (FindFirstVolumeW / FindNextVolumeW) and
    /// resolves each one to its backing disk extents, mount points, label,
    /// and file system. Replaces the inbox Get-Volume / Get-Partition pipe
    /// for read paths.
    /// </summary>
    internal static class Win32VolumeMapper
    {
        private const int MaxVolumePathBuffer = 1024;
        private const int InitialMountPointBuffer = 1024;

        /// <summary>
        /// Enumerates every fixed/removable volume on the host. Returns a
        /// fresh list each call; callers can cache per cmdlet invocation.
        /// </summary>
        public static IList<Win32VolumeInfo> EnumerateAll()
        {
            var list = new List<Win32VolumeInfo>();
            var sb = new StringBuilder(MaxVolumePathBuffer);
            var find = NativeMethods.FindFirstVolumeW(sb, sb.Capacity);
            if (find == NativeConstants.INVALID_HANDLE_VALUE)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(), "FindFirstVolumeW failed.");
            }
            try
            {
                do
                {
                    var info = TryDescribe(sb.ToString());
                    if (info != null) { list.Add(info); }
                    sb.Length = 0;
                    sb.EnsureCapacity(MaxVolumePathBuffer);
                }
                while (NativeMethods.FindNextVolumeW(find, sb, sb.Capacity));

                var last = Marshal.GetLastWin32Error();
                if (last != NativeConstants.ERROR_NO_MORE_FILES && last != NativeConstants.ERROR_SUCCESS)
                {
                    throw new Win32Exception(last, "FindNextVolumeW failed.");
                }
            }
            finally { NativeMethods.FindVolumeClose(find); }
            return list;
        }

        /// <summary>
        /// Returns the volume that maps to (<paramref name="diskNumber"/>,
        /// starting at <paramref name="startingOffset"/>) by examining each
        /// volume's first disk extent. Returns null when no match exists,
        /// which is normal for partitions without a file system (MSR, BIOS
        /// boot, unformatted recovery slots, etc.).
        /// </summary>
        public static Win32VolumeInfo FindForPartition(
            IList<Win32VolumeInfo> volumes, int diskNumber, long startingOffset)
        {
            if (volumes == null) { return null; }
            foreach (var v in volumes)
            {
                if (v.Extents == null || v.Extents.Count == 0) { continue; }
                var ex = v.Extents[0];
                if (ex.DiskNumber == diskNumber && ex.StartingOffset == startingOffset)
                {
                    return v;
                }
            }
            return null;
        }

        /// <summary>
        /// Reads the first backing disk extent (disk number + starting offset)
        /// for an arbitrary kernel device path such as
        /// <c>\\?\GLOBALROOT\Device\HarddiskVolumeN</c> or
        /// <c>\\?\Volume{guid}\</c>. Returns false when the device cannot be
        /// opened or reports no extents (unformatted / offline).
        /// </summary>
        public static bool TryGetDiskAndOffset(string deviceName, out int diskNumber, out long startingOffset)
        {
            diskNumber = -1;
            startingOffset = -1;
            if (string.IsNullOrEmpty(deviceName)) { return false; }
            try
            {
                var extents = ReadExtents(deviceName);
                if (extents != null && extents.Count > 0)
                {
                    diskNumber = extents[0].DiskNumber;
                    startingOffset = extents[0].StartingOffset;
                    return true;
                }
            }
            catch (Win32Exception) { /* device not ready / no extents */ }
            return false;
        }

        private static Win32VolumeInfo TryDescribe(string volumeName)
        {
            if (string.IsNullOrEmpty(volumeName)) { return null; }
            var info = new Win32VolumeInfo { VolumeName = volumeName };
            try { info.Extents = ReadExtents(volumeName); }
            catch (Win32Exception) { /* volume not yet mounted / no media */ }
            ReadVolumeInformation(volumeName, info);
            info.MountPoints = ReadMountPoints(volumeName);
            return info;
        }

        private static IList<Win32VolumeExtent> ReadExtents(string volumeName)
        {
            var result = new List<Win32VolumeExtent>();
            using (SafeFileHandle handle = DeviceHandleFactory.OpenVolume(volumeName, readOnly: true))
            {
                var bufferSize = Marshal.SizeOf<VOLUME_DISK_EXTENTS_HEADER>()
                                 + 16 * Marshal.SizeOf<DISK_EXTENT>();
                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    int returned;
                    var ok = NativeMethods.DeviceIoControl(
                        handle, NativeConstants.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS,
                        IntPtr.Zero, 0, buffer, bufferSize, out returned, IntPtr.Zero);
                    if (!ok)
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error(),
                            "IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS failed for " + volumeName);
                    }
                    var header = Marshal.PtrToStructure<VOLUME_DISK_EXTENTS_HEADER>(buffer);
                    var headerSize = Marshal.SizeOf<VOLUME_DISK_EXTENTS_HEADER>();
                    var extentSize = Marshal.SizeOf<DISK_EXTENT>();
                    for (int i = 0; i < header.NumberOfDiskExtents; i++)
                    {
                        var entryPtr = IntPtr.Add(buffer, headerSize + i * extentSize);
                        var ex = Marshal.PtrToStructure<DISK_EXTENT>(entryPtr);
                        result.Add(new Win32VolumeExtent
                        {
                            DiskNumber     = ex.DiskNumber,
                            StartingOffset = ex.StartingOffset,
                            Length         = ex.ExtentLength,
                        });
                    }
                }
                finally { Marshal.FreeHGlobal(buffer); }
            }
            return result;
        }

        private static void ReadVolumeInformation(string volumeName, Win32VolumeInfo info)
        {
            // GetVolumeInformationW requires a trailing backslash on the root.
            var root = volumeName.EndsWith("\\", StringComparison.Ordinal) ? volumeName : volumeName + "\\";
            var label = new StringBuilder(261);
            var fs    = new StringBuilder(261);
            uint serial, maxComponent, flags;
            if (NativeMethods.GetVolumeInformationW(
                    root, label, label.Capacity,
                    out serial, out maxComponent, out flags,
                    fs, fs.Capacity))
            {
                info.Label        = label.ToString();
                info.FileSystem   = fs.ToString();
                info.SerialNumber = serial;
            }
        }

        private static IList<string> ReadMountPoints(string volumeName)
        {
            var root = volumeName.EndsWith("\\", StringComparison.Ordinal) ? volumeName : volumeName + "\\";
            var buffer = new char[InitialMountPointBuffer];
            int needed;
            if (!NativeMethods.GetVolumePathNamesForVolumeNameW(root, buffer, buffer.Length, out needed))
            {
                var err = Marshal.GetLastWin32Error();
                if (err == NativeConstants.ERROR_MORE_DATA)
                {
                    buffer = new char[needed];
                    if (!NativeMethods.GetVolumePathNamesForVolumeNameW(root, buffer, buffer.Length, out needed))
                    {
                        return new List<string>();
                    }
                }
                else { return new List<string>(); }
            }
            // The buffer holds back-to-back null-terminated paths followed by a
            // second null. Split on \0, drop empties.
            return new string(buffer, 0, Math.Max(0, needed))
                .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
    }
}
