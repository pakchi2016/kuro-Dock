using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kuro_Dock
{
    public static class IconManager
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        public static ImageSource? GetIcon(string path, bool isDirectory)
        {
            SHFILEINFO sfi = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES;

            uint fileAttributes = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : 0;

            IntPtr hIcon = SHGetFileInfo(path, fileAttributes, ref sfi, (uint)Marshal.SizeOf(sfi), flags | SHGFI_SMALLICON);
            if (hIcon == IntPtr.Zero)
            {
                return null;
            }

            Icon icon = (Icon)Icon.FromHandle(sfi.hIcon).Clone();
            DestroyIcon(sfi.hIcon);

            ImageSource imageSource = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            imageSource.Freeze(); // UIスレッド以外からのアクセスのために必須
            return imageSource;
        }
    }
}