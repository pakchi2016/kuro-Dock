using Kuro_Dock.Core.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kuro_Dock.Core.Services
{

    public class DirectoryService
    {
        public IEnumerable<DirectoryItem> GetRootDirectories()
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    yield return new DirectoryItem
                    {
                        Name = drive.Name,
                        FullPath = drive.RootDirectory.FullName
                    };
                }
            }
        }

        public IEnumerable<DirectoryItem> GetSubDirectories(string parentPath)
        {
            try
            {
                return Directory.EnumerateDirectories(parentPath)
                                .Select(path => new DirectoryItem
                                {
                                    Name = Path.GetFileName(path),
                                    FullPath = path
                                });
            }
            catch { return Enumerable.Empty<DirectoryItem>(); }
        }
    }
}
