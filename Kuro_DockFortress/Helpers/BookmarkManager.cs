using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using Kuro_DockFortress.Models;

namespace Kuro_DockFortress.Helpers
{
    public static class BookmarkManager
    {
        private static readonly string FilePath = "bookmarks.json";

        public static void Save(ObservableCollection<BookmarkItemModel> bookmarks)
        {
            // ★ 日本語が \uXXXX に変換されるのを防ぐ絶対命令です
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(bookmarks, options);

            // ★ UTF8Encoding(true) を指定することで、卿の望む「BOM付き」として出力しますわ
            using (var sw = new StreamWriter(FilePath, false, new UTF8Encoding(true)))
            {
                sw.Write(json);
            }
        }

        public static ObservableCollection<BookmarkItemModel> Load()
        {
            if (!File.Exists(FilePath)) return new ObservableCollection<BookmarkItemModel>();

            try
            {
                // C#はBOMの有無を自動判定してくれるため、読み込みはこれだけで完璧です
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                var list = JsonSerializer.Deserialize<ObservableCollection<BookmarkItemModel>>(json);
                return list ?? new ObservableCollection<BookmarkItemModel>();
            }
            catch
            {
                return new ObservableCollection<BookmarkItemModel>();
            }
        }
    }
}