using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Kuro_DockLauncher2.Models
{
    public class BookmarkItem
    {
        public string Path { get; set; }

        [JsonIgnore]
        public string Name => string.IsNullOrEmpty(Path) ? string.Empty : System.IO.Path.GetFileName(Path);

        [JsonIgnore]
        public bool IsFolder => Directory.Exists(Path);
    }
}
