using System;

namespace Kuro_Dock.Core.Models
{

    public class FileItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string ItemType { get; set; } = string.Empty;
    }
}
