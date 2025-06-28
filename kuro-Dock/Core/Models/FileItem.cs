using System;

namespace Kuro_Dock.Core.Models
{
    /// <summary>
    /// ファイルに関する純粋なデータを保持します。
    /// </summary>
    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string ItemType { get; set; } = string.Empty;
    }
}
