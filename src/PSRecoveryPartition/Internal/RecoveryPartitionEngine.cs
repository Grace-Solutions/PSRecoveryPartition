using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace PSRecoveryPartition
{
    /// <summary>
    /// High level operations on recovery partitions implemented on top of the
    /// in-box Storage module. Returns rich <see cref="RecoveryPartitionInfo"/>
    /// objects so cmdlets can stay thin.
    /// </summary>
    internal sealed class RecoveryPartitionEngine
    {
        private readonly StorageInvoker _storage;
        private readonly PSCmdlet _owner;

        public RecoveryPartitionEngine(PSCmdlet owner)
        {
            _owner = owner;
            _storage = new StorageInvoker(owner);
        }

        public IList<RecoveryPartitionInfo> Get(int? diskNumber = null, bool recoveryOnly = true)
        {
            var args = new Hashtable();
            if (diskNumber.HasValue) { args["DiskNumber"] = diskNumber.Value; }
            var partitions = _storage.Invoke("Get-Partition", args);
            var result = new List<RecoveryPartitionInfo>();
            foreach (var p in partitions)
            {
                PSObject volume = null;
                try
                {
                    var vol = _storage.Invoke("Get-Volume", new Hashtable { { "Partition", p } });
                    if (vol.Count > 0) { volume = vol[0]; }
                }
                catch { /* not every partition has a volume */ }
                var info = PartitionMapper.FromPartition(p, volume);
                if (!recoveryOnly || info.IsRecoveryPartition) { result.Add(info); }
            }
            return result;
        }

        public RecoveryPartitionInfo Create(int diskNumber, long sizeBytes, string label, string fileSystem, FileInfo windowsREImagePath)
        {
            var disk = _storage.InvokeSingle("Get-Disk", new Hashtable { { "Number", diskNumber } });
            if (disk == null) { throw new InvalidOperationException("Disk " + diskNumber + " was not found."); }
            var partitionStyle = disk.Properties["PartitionStyle"] != null ? disk.Properties["PartitionStyle"].Value as string : null;

            var newPartitionArgs = new Hashtable
            {
                { "DiskNumber", diskNumber },
                { "Size", sizeBytes },
                { "IsHidden", true }
            };
            if (string.Equals(partitionStyle, "GPT", StringComparison.OrdinalIgnoreCase))
            {
                newPartitionArgs["GptType"] = RecoveryPartitionConstants.GptTypeRecovery;
            }
            else if (string.Equals(partitionStyle, "MBR", StringComparison.OrdinalIgnoreCase))
            {
                newPartitionArgs["MbrType"] = "IFS";
                if (_owner != null)
                {
                    _owner.WriteWarning("MBR disk " + diskNumber + ": the Storage module cannot natively assign the Windows recovery MBR type 0x27. The partition will be created as IFS and tagged with the RECOVERY label; downstream tooling that relies on the 0x27 byte must run diskpart 'SET ID=27' or equivalent.");
                }
            }

            var partition = _storage.InvokeSingle("New-Partition", newPartitionArgs);
            if (partition == null) { throw new InvalidOperationException("Failed to create partition on disk " + diskNumber + "."); }

            var fmtArgs = new Hashtable
            {
                { "Partition", partition },
                { "FileSystem", string.IsNullOrEmpty(fileSystem) ? "NTFS" : fileSystem },
                { "NewFileSystemLabel", string.IsNullOrEmpty(label) ? RecoveryPartitionConstants.DefaultLabel : label },
                { "Confirm", false },
                { "Force", true }
            };
            var volume = _storage.InvokeSingle("Format-Volume", fmtArgs);

            var partitionNumber = Convert.ToInt32(partition.Properties["PartitionNumber"].Value);
            _storage.Invoke("Set-Partition", new Hashtable
            {
                { "DiskNumber", diskNumber },
                { "PartitionNumber", partitionNumber },
                { "NoDefaultDriveLetter", true }
            });

            if (windowsREImagePath != null && windowsREImagePath.Exists)
            {
                CopyImageOnto(partition, windowsREImagePath);
            }

            return PartitionMapper.FromPartition(partition, volume);
        }

        public RecoveryPartitionInfo Resize(int diskNumber, int partitionNumber, long newSizeBytes)
        {
            _storage.Invoke("Resize-Partition", new Hashtable
            {
                { "DiskNumber", diskNumber },
                { "PartitionNumber", partitionNumber },
                { "Size", newSizeBytes }
            });
            var partition = _storage.InvokeSingle("Get-Partition", new Hashtable
            {
                { "DiskNumber", diskNumber }, { "PartitionNumber", partitionNumber }
            });
            return PartitionMapper.FromPartition(partition);
        }

        public void Remove(int diskNumber, int partitionNumber)
        {
            _storage.Invoke("Remove-Partition", new Hashtable
            {
                { "DiskNumber", diskNumber },
                { "PartitionNumber", partitionNumber },
                { "Confirm", false }
            });
        }

        public RecoveryPartitionInfo SetMetadata(int diskNumber, int partitionNumber, string label, bool? noDefaultDriveLetter, bool? isHidden)
        {
            if (!string.IsNullOrEmpty(label))
            {
                _storage.Invoke("Set-Volume", new Hashtable
                {
                    { "Partition", _storage.InvokeSingle("Get-Partition", new Hashtable { { "DiskNumber", diskNumber }, { "PartitionNumber", partitionNumber } }) },
                    { "NewFileSystemLabel", label }
                });
            }
            var setArgs = new Hashtable { { "DiskNumber", diskNumber }, { "PartitionNumber", partitionNumber } };
            if (noDefaultDriveLetter.HasValue) { setArgs["NoDefaultDriveLetter"] = noDefaultDriveLetter.Value; }
            if (isHidden.HasValue) { setArgs["IsHidden"] = isHidden.Value; }
            if (setArgs.Count > 2) { _storage.Invoke("Set-Partition", setArgs); }

            var partition = _storage.InvokeSingle("Get-Partition", new Hashtable
            {
                { "DiskNumber", diskNumber }, { "PartitionNumber", partitionNumber }
            });
            return PartitionMapper.FromPartition(partition);
        }

        public RecoveryPartitionMountResult AddAccessPath(int diskNumber, int partitionNumber, string accessPath)
        {
            _storage.Invoke("Add-PartitionAccessPath", new Hashtable
            {
                { "DiskNumber", diskNumber },
                { "PartitionNumber", partitionNumber },
                { "AccessPath", accessPath }
            });
            return new RecoveryPartitionMountResult
            {
                AccessPath = accessPath,
                Mounted = true,
                Changed = true,
                TimestampUtc = DateTimeOffset.UtcNow,
                ExecutionMethod = RecoveryExecutionMethod.Storage,
                Partition = Get(diskNumber, recoveryOnly: false).FirstOrDefault(p => p.PartitionNumber == partitionNumber)
            };
        }

        public RecoveryPartitionMountResult RemoveAccessPath(int diskNumber, int partitionNumber, string accessPath)
        {
            _storage.Invoke("Remove-PartitionAccessPath", new Hashtable
            {
                { "DiskNumber", diskNumber },
                { "PartitionNumber", partitionNumber },
                { "AccessPath", accessPath }
            });
            return new RecoveryPartitionMountResult
            {
                AccessPath = accessPath,
                Mounted = false,
                Changed = true,
                TimestampUtc = DateTimeOffset.UtcNow,
                ExecutionMethod = RecoveryExecutionMethod.Storage,
                Partition = Get(diskNumber, recoveryOnly: false).FirstOrDefault(p => p.PartitionNumber == partitionNumber)
            };
        }

        private void CopyImageOnto(PSObject partition, FileInfo image)
        {
            // Choose a temporary mount point under TEMP, copy the image, then remove.
            var tempRoot = Path.Combine(Path.GetTempPath(), "PSRecoveryPartition-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            var diskNumber = Convert.ToInt32(partition.Properties["DiskNumber"].Value);
            var partNumber = Convert.ToInt32(partition.Properties["PartitionNumber"].Value);
            try
            {
                _storage.Invoke("Add-PartitionAccessPath", new Hashtable
                {
                    { "DiskNumber", diskNumber }, { "PartitionNumber", partNumber }, { "AccessPath", tempRoot }
                });
                var dest = Path.Combine(tempRoot, "Recovery", "WindowsRE");
                Directory.CreateDirectory(dest);
                File.Copy(image.FullName, Path.Combine(dest, image.Name), true);
            }
            finally
            {
                try
                {
                    _storage.Invoke("Remove-PartitionAccessPath", new Hashtable
                    {
                        { "DiskNumber", diskNumber }, { "PartitionNumber", partNumber }, { "AccessPath", tempRoot }
                    });
                }
                catch { /* best effort */ }
                try { Directory.Delete(tempRoot, true); } catch { /* best effort */ }
            }
        }
    }
}
