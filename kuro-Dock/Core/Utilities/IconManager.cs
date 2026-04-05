using System;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media;

namespace Kuro_Dock.Core.Utilities
{
    public static class IconManager
    {
        // 拡張子ごとのアイコンを記憶するスレッドセーフな辞書ですわ
        private static readonly ConcurrentDictionary<string, ImageSource> _iconCache = new();

        // フォルダ用のアイコンは全フォルダ共通なので、一つだけ記憶しておきます
        private static ImageSource? _folderIcon;

        public static ImageSource? GetIcon(string fullPath, bool isDirectory)
        {
            // フォルダの場合：まだ取得していなければ取得し、以降は記憶したものを使い回します
            if (isDirectory)
            {
                if (_folderIcon == null)
                {
                    _folderIcon = IconUtility.GetIcon(fullPath, true);
                }
                return _folderIcon;
            }

            string ext = Path.GetExtension(fullPath).ToLower();

            // .exe, .ico, .lnk 等はファイルごとに固有のアイコンを持つため、記憶せずに毎回取得します
            if (ext == ".exe" || ext == ".ico" || ext == ".lnk")
            {
                return IconUtility.GetIcon(fullPath, true);
            }

            // それ以外のファイル：キャッシュにあればそれを返し、無ければOSから取得して記憶します
            return _iconCache.GetOrAdd(ext, _ => IconUtility.GetIcon(fullPath, true));
        }
    }
}