using Kuro_Dock.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kuro_Dock.Core.Services
{
    /// <summary>
    /// ファイルに関する操作を専門に担当します。
    /// </summary>
    public class FileService
    {
        public IEnumerable<FileItem> GetFiles(string parentPath)
        {
            try
            {
                return Directory.EnumerateFiles(parentPath)
                                .Select(path => new FileInfo(path))
                                .Select(info => new FileItem
                                {
                                    Name = info.Name,
                                    FullPath = info.FullName,
                                    Size = info.Length,
                                    LastWriteTime = info.LastWriteTime,
                                    ItemType = info.Extension
                                });
            }
            catch { return Enumerable.Empty<FileItem>(); }
        }
    }
}
