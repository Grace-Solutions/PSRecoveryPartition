using PSRecoveryPartition.Native;

namespace PSRecoveryPartition
{
    /// <summary>
    /// Platform firmware helpers used when wiring boot entries.
    /// </summary>
    internal static class FirmwareInfo
    {
        /// <summary>
        /// The Windows loader file name for the running firmware:
        /// <c>winload.efi</c> on UEFI (the default) or <c>winload.exe</c> on BIOS.
        /// </summary>
        public static string WinloadLeaf()
        {
            uint firmwareType;
            // FirmwareTypeBios = 1, FirmwareTypeUefi = 2.
            if (NativeMethods.GetFirmwareType(out firmwareType) && firmwareType == 1)
            {
                return "winload.exe";
            }
            return "winload.efi";
        }
    }
}
