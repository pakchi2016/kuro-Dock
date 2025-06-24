using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Kuro_Dock.Models;

namespace Kuro_Dock.Services
{
    public class BookmarkService
    {
        private readonly string _filePath;

        public BookmarkService()
        {
            // AppDataフォルダ内に、我々のアプリ専用の保存場所を作る
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "Kuro-Dock");
            Directory.CreateDirectory(appFolderPath); // フォルダがなければ作成
            _filePath = Path.Combine(appFolderPath, "bookmarks.json");
        }

        public List<BookmarkItem> LoadBookmarks()
        {
            if (!File.Exists(_filePath))
            {
                return new List<BookmarkItem>();
            }

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<BookmarkItem>>(json) ?? new List<BookmarkItem>();
        }

        public void SaveBookmarks(IEnumerable<BookmarkItem> bookmarks)
        {
            var json = JsonSerializer.Serialize(bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}