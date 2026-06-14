using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Kuro_DockThrone.Core.Models
{
    public class BookmarkModel
    {
        // エコシステムにおけるブックマークの真名と転移先パスですわ
        public string Alias { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;

        // 画面表示用の動的な「仮の姿」です。JSONシリアライズからは隠蔽しますわ
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Alias))
                    return Alias;

                if (string.IsNullOrWhiteSpace(Path))
                    return "未設定の領域";

                try
                {
                    if (Path.Length <= 3 && Path.EndsWith(":\\"))
                        return Path;

                    string name = System.IO.Path.GetFileName(Path.TrimEnd('\\', '/'));
                    return string.IsNullOrEmpty(name) ? Path : name;
                }
                catch
                {
                    return Path;
                }
            }
        }
    }

    public class IndexModel
    {
        public string Name { get; set; } = string.Empty;
        public List<BookmarkModel> Bookmarks { get; set; } = new List<BookmarkModel>();
    }
}