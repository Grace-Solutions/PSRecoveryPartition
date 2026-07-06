using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace PSRecoveryPartition.Native
{
    /// <summary>
    /// Minimal managed wrapper over the Windows Imaging (WIMGAPI, <c>wimgapi.dll</c>)
    /// API. Used to apply a boot image flat onto a target directory (non-RAM /
    /// flat boot) and to extract a single file (for example <c>boot.sdi</c>) from
    /// inside a WIM. WIMGAPI is the native, in-box imaging surface -- it lets the
    /// module read and apply WIM content without the DISM PowerShell module, the
    /// DISM API, or shelling out to <c>dism.exe</c>.
    /// </summary>
    internal static class WimgApi
    {
        // dwDesiredAccess
        private const uint WIM_GENERIC_READ  = 0x80000000;

        // dwCreationDisposition
        private const uint WIM_OPEN_EXISTING = 3;

        // dwFlagsAndAttributes / apply flags
        private const uint WIM_FLAG_NONE      = 0x00000000;

        // Compression (ignored for OPEN_EXISTING but must be passed).
        private const uint WIM_COMPRESS_NONE  = 0;

        // Message-callback ids (WIM_MSG = WM_APP(0x8000) + 0x1476). Offsets per
        // wimgapi.h: +1 = WIM_MSG_TEXT, +2 = WIM_MSG_PROGRESS (percent in wParam),
        // +3 = WIM_MSG_PROCESS. Using +1 here previously meant we listened for the
        // text message and never received the apply progress at all.
        private const uint WIM_MSG          = 0x8000 + 0x1476;
        private const uint WIM_MSG_PROGRESS = WIM_MSG + 2;

        // Message-callback return values.
        private const uint WIM_MSG_SUCCESS    = 0;
        private const uint WIM_MSG_ABORT_IMAGE = 0xFFFFFFFF;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint WimMessageCallback(uint msgId, IntPtr wParam, IntPtr lParam, IntPtr userData);

        [DllImport("wimgapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr WIMCreateFile(
            string pszWimPath,
            uint dwDesiredAccess,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            uint dwCompressionType,
            out uint pdwCreationResult);

        [DllImport("wimgapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WIMSetTemporaryPath(IntPtr hWim, string pszPath);

        [DllImport("wimgapi.dll", SetLastError = true)]
        private static extern uint WIMGetImageCount(IntPtr hWim);

        [DllImport("wimgapi.dll", SetLastError = true)]
        private static extern IntPtr WIMLoadImage(IntPtr hWim, uint dwImageIndex);

        [DllImport("wimgapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WIMApplyImage(IntPtr hImage, string pszPath, uint dwApplyFlags);

        [DllImport("wimgapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WIMExtractImagePath(IntPtr hImage, string pszImagePath, string pszDestinationPath, uint dwExtractFlags);

        [DllImport("wimgapi.dll", SetLastError = true)]
        private static extern uint WIMRegisterMessageCallback(IntPtr hWim, WimMessageCallback fpMessageProc, IntPtr pvUserData);

        [DllImport("wimgapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WIMUnregisterMessageCallback(IntPtr hWim, WimMessageCallback fpMessageProc);

        [DllImport("wimgapi.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WIMCloseHandle(IntPtr hObj);

        private const uint INVALID_IMAGE_INDEX = 0xFFFFFFFF;

        /// <summary>
        /// Number of images inside <paramref name="wimPath"/>. Lets callers
        /// validate a requested index before applying.
        /// </summary>
        public static int GetImageCount(string wimPath)
        {
            var hWim = OpenRead(wimPath);
            try { return (int)WIMGetImageCount(hWim); }
            finally { WIMCloseHandle(hWim); }
        }

        /// <summary>
        /// Applies image <paramref name="imageIndex"/> (1-based) from
        /// <paramref name="wimPath"/> flat onto <paramref name="destinationDir"/>.
        /// <paramref name="onProgress"/>, when supplied, receives 0-100 percent
        /// updates. <paramref name="temporaryDir"/> is WIMGAPI's scratch area
        /// (defaults to the destination's parent when null).
        /// </summary>
        public static void ApplyImage(string wimPath, int imageIndex, string destinationDir, string temporaryDir, Action<int> onProgress)
        {
            if (imageIndex < 1) { throw new ArgumentOutOfRangeException("imageIndex", imageIndex, "Image index is 1-based."); }
            Directory.CreateDirectory(destinationDir);

            var hWim = OpenRead(wimPath);
            WimMessageCallback callback = null;
            var callbackRegistered = false;
            try
            {
                if (!WIMSetTemporaryPath(hWim, temporaryDir ?? Path.GetTempPath()))
                {
                    ThrowLastError("WIMSetTemporaryPath");
                }

                var count = (int)WIMGetImageCount(hWim);
                if (imageIndex > count)
                {
                    throw new ArgumentOutOfRangeException(
                        "imageIndex", imageIndex, "The WIM contains " + count + " image(s).");
                }

                if (onProgress != null)
                {
                    callback = (msgId, wParam, lParam, userData) =>
                    {
                        if (msgId == WIM_MSG_PROGRESS)
                        {
                            try { onProgress((int)(wParam.ToInt64() & 0xFFFF)); } catch { /* never abort on reporter error */ }
                        }
                        return WIM_MSG_SUCCESS;
                    };
                    if (WIMRegisterMessageCallback(hWim, callback, IntPtr.Zero) == INVALID_IMAGE_INDEX)
                    {
                        ThrowLastError("WIMRegisterMessageCallback");
                    }
                    callbackRegistered = true;
                }

                var hImage = WIMLoadImage(hWim, (uint)imageIndex);
                if (hImage == IntPtr.Zero) { ThrowLastError("WIMLoadImage"); }
                try
                {
                    if (!WIMApplyImage(hImage, destinationDir, WIM_FLAG_NONE))
                    {
                        ThrowLastError("WIMApplyImage");
                    }
                }
                finally { WIMCloseHandle(hImage); }
            }
            finally
            {
                if (callbackRegistered && callback != null)
                {
                    try { WIMUnregisterMessageCallback(hWim, callback); } catch { /* best effort */ }
                }
                WIMCloseHandle(hWim);
            }
        }

        /// <summary>
        /// Extracts a single file at <paramref name="pathInImage"/> (for example
        /// <c>\Windows\Boot\DVD\PCAT\boot.sdi</c>) from image
        /// <paramref name="imageIndex"/> to <paramref name="destinationFile"/>.
        /// Returns false when the file is absent from the image; throws on any
        /// other failure.
        /// </summary>
        public static bool TryExtractFile(string wimPath, int imageIndex, string pathInImage, string destinationFile)
        {
            if (imageIndex < 1) { throw new ArgumentOutOfRangeException("imageIndex", imageIndex, "Image index is 1-based."); }
            var destDir = Path.GetDirectoryName(destinationFile);
            if (!string.IsNullOrEmpty(destDir)) { Directory.CreateDirectory(destDir); }

            var hWim = OpenRead(wimPath);
            try
            {
                if (!WIMSetTemporaryPath(hWim, Path.GetTempPath())) { ThrowLastError("WIMSetTemporaryPath"); }
                var hImage = WIMLoadImage(hWim, (uint)imageIndex);
                if (hImage == IntPtr.Zero) { ThrowLastError("WIMLoadImage"); }
                try
                {
                    if (WIMExtractImagePath(hImage, pathInImage, destinationFile, WIM_FLAG_NONE)) { return true; }
                    var err = Marshal.GetLastWin32Error();
                    // ERROR_FILE_NOT_FOUND (2) / ERROR_PATH_NOT_FOUND (3): the file
                    // simply is not in this image -- a normal "not found" result.
                    if (err == 2 || err == 3) { return false; }
                    throw new Win32Exception(err, "WIMExtractImagePath failed for '" + pathInImage + "' (Win32 " + err + ").");
                }
                finally { WIMCloseHandle(hImage); }
            }
            finally { WIMCloseHandle(hWim); }
        }

        private static IntPtr OpenRead(string wimPath)
        {
            if (string.IsNullOrEmpty(wimPath)) { throw new ArgumentException("WIM path is required.", "wimPath"); }
            uint creationResult;
            var hWim = WIMCreateFile(wimPath, WIM_GENERIC_READ, WIM_OPEN_EXISTING, WIM_FLAG_NONE, WIM_COMPRESS_NONE, out creationResult);
            if (hWim == IntPtr.Zero) { ThrowLastError("WIMCreateFile('" + wimPath + "')"); }
            return hWim;
        }

        private static void ThrowLastError(string what)
        {
            var err = Marshal.GetLastWin32Error();
            throw new Win32Exception(err, what + " failed with Win32 error " + err + ".");
        }
    }
}
