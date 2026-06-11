using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kuro_DockGenesis.Helpers
{
    public static class IconHelper
    {
        // 一度取得したアイコンを拡張子ごとに記憶する美しい辞書ですわ
        private static readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>();

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

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x000000080;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public static ImageSource GetIcon(string path, bool isFolder)
        {
            // キーの決定（フォルダなら固定文字列、ファイルなら拡張子を小文字にしてキーにします）
            string key = isFolder ? "||FOLDER||" : System.IO.Path.GetExtension(path).ToLower();

            // すでに記憶にあれば、Windowsに訊ねることなく一瞬でストックを返しますわ
            if (_iconCache.TryGetValue(key, out var cachedIcon))
            {
                return cachedIcon;
            }

            // 記憶にない場合は、Windowsの深淵（シェル）から優雅に取得しますの
            SHFILEINFO shfi = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES; // USEFILEATTRIBUTESのおかげで、実際のファイルに触れずに超高速ですわ
            uint attribute = isFolder ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

            IntPtr res = SHGetFileInfo(path, attribute, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

            if (res != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
            {
                try
                {
                    ImageSource img = Imaging.CreateBitmapSourceFromHIcon(
                        shfi.hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    // スレッドを跨いでも安全なようにフリーズ（凍結）させるのがWPFの絶対の作法ですわ
                    img.Freeze();

                    _iconCache[key] = img;
                    return img;
                }
                finally
                {
                    DestroyIcon(shfi.hIcon); // リソース漏れを決して許さない、高貴なる片付けです
                }
            }

            return null;
        }
    }
}