using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Whole-disk initialization: clears the existing partition table, writes a
    /// fresh MBR/GPT, and lays down a complete partition set in one
    /// IOCTL_DISK_SET_DRIVE_LAYOUT_EX call. Everything here is direct IOCTL work
    /// -- no diskpart, no format.com.
    /// </summary>
    internal static partial class Win32PartitionWriter
    {
        /// <summary>
        /// Locks and dismounts every volume backed by <paramref name="diskNumber"/>
        /// so the kernel will accept a new drive layout. Best effort: a volume that
        /// cannot be locked is reported through <paramref name="verbose"/> and
        /// skipped, because the layout write itself is the authoritative gate.
        /// </summary>
        public static void DismountVolumesOnDisk(int diskNumber, Action<string> verbose)
        {
            IList<Win32VolumeInfo> volumes;
            try { volumes = Win32VolumeMapper.EnumerateAll(); }
            catch (Exception ex)
            {
                if (verbose != null) { verbose("Volume enumeration failed before dismount: " + ex.Message); }
                return;
            }

            foreach (var vol in volumes)
            {
                if (vol.Extents == null || !vol.Extents.Any(e => e.DiskNumber == diskNumber)) { continue; }
                try
                {
                    using (var handle = DeviceHandleFactory.OpenVolume(vol.VolumeName, readOnly: false))
                    {
                        int returned;
                        NativeMethods.DeviceIoControl(handle, NativeConstants.FSCTL_LOCK_VOLUME,
                            IntPtr.Zero, 0, IntPtr.Zero, 0, out returned, IntPtr.Zero);
                        var dismounted = NativeMethods.DeviceIoControl(handle, NativeConstants.FSCTL_DISMOUNT_VOLUME,
                            IntPtr.Zero, 0, IntPtr.Zero, 0, out returned, IntPtr.Zero);
                        if (verbose != null)
                        {
                            verbose(dismounted
                                ? "Dismounted volume " + vol.VolumeName + " on disk " + diskNumber + "."
                                : "Could not dismount volume " + vol.VolumeName + " (Win32 " + Marshal.GetLastWin32Error() + ").");
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (verbose != null) { verbose("Skipping volume " + vol.VolumeName + ": " + ex.Message); }
                }
            }
        }

        /// <summary>
        /// Erases the partition table on <paramref name="diskNumber"/>. Safe to
        /// call on a RAW disk that has no table yet.
        /// </summary>
        public static void DeleteDriveLayout(int diskNumber)
        {
            using (SafeFileHandle handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: false))
            {
                int returned;
                if (!NativeMethods.DeviceIoControl(
                        handle, NativeConstants.IOCTL_DISK_DELETE_DRIVE_LAYOUT,
                        IntPtr.Zero, 0, IntPtr.Zero, 0, out returned, IntPtr.Zero))
                {
                    var err = Marshal.GetLastWin32Error();
                    // ERROR_NOT_READY / ERROR_INVALID_FUNCTION on an already-raw
                    // disk is benign: there is simply nothing to delete.
                    if (err != 21 && err != 1)
                    {
                        throw new Win32Exception(err,
                            "IOCTL_DISK_DELETE_DRIVE_LAYOUT failed for disk " + diskNumber + ".");
                    }
                }
            }
            UpdateProperties(diskNumber);
        }

        /// <summary>
        /// Writes a fresh partition table of the requested style onto
        /// <paramref name="diskNumber"/>.
        /// </summary>
        public static void CreateDisk(int diskNumber, PartitionStyle style)
        {
            var create = new CREATE_DISK { PartitionStyle = style };
            if (style == PartitionStyle.Gpt)
            {
                create.Gpt = new CREATE_DISK_GPT { DiskId = Guid.NewGuid(), MaxPartitionCount = 128u };
            }
            else
            {
                create.Mbr = new CREATE_DISK_MBR { Signature = GenerateMbrSignature() };
            }

            var size = Marshal.SizeOf<CREATE_DISK>();
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                for (int i = 0; i < size; i++) { Marshal.WriteByte(buffer, i, 0); }
                Marshal.StructureToPtr(create, buffer, false);

                using (SafeFileHandle handle = DeviceHandleFactory.OpenPhysicalDisk(diskNumber, readOnly: false))
                {
                    int returned;
                    if (!NativeMethods.DeviceIoControl(
                            handle, NativeConstants.IOCTL_DISK_CREATE_DISK,
                            buffer, size, IntPtr.Zero, 0, out returned, IntPtr.Zero))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(),
                            "IOCTL_DISK_CREATE_DISK failed for disk " + diskNumber + " (" + style + ").");
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buffer); }

            UpdateProperties(diskNumber);
        }

        /// <summary>
        /// Replaces the entire partition table with <paramref name="entries"/>.
        /// The caller is responsible for having already written a table of the
        /// matching style via <see cref="CreateDisk"/>, since the layout header is
        /// taken from the disk's freshly-read geometry.
        /// </summary>
        public static void ApplyFullLayout(int diskNumber, IList<PARTITION_INFORMATION_EX> entries)
        {
            var disk = Win32DiskInfoReader.Read(diskNumber);
            WriteLayout(diskNumber, disk, entries);
            UpdateProperties(diskNumber);
        }
    }
}
