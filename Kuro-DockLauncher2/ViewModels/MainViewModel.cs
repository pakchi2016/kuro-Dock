using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Text.Json;
using Kuro_DockLauncher2.Models;

namespace Kuro_DockLauncher2.ViewModels
{
    public class MainViewModel
    {
        private const string ConfigFileName = "config.json";


        // UIと完全に連動する、たった一つの魔法のリストですわ
        public ObservableCollection<IndexItem> IndexItems { get; set; }
        // ★現在選択中のブックマーク（ファイル/フォルダ）を画面に展開するためのリストですわ
        public ObservableCollection<BookmarkItem> CurrentBookmarks { get; set; } = new ObservableCollection<BookmarkItem>();

        // ★ フォルダの中身（第3階層）を展開するためのリストですわ
        public ObservableCollection<BookmarkItem> CurrentSubBookmarks { get; set; } = new ObservableCollection<BookmarkItem>();

        public MainViewModel()
        {
            IndexItems = new ObservableCollection<IndexItem>();
            // 起動時にただちに読み込みます
            LoadConfiguration();

            // 2. 復元した既存のインデックスの中身（Bookmarks）に監視を付けますわ
            foreach (var item in IndexItems)
            {
                item.Bookmarks.CollectionChanged += (s, e) => SaveConfiguration();
            }

            // 3. 親リスト（IndexItems）自体の変化を監視します
            IndexItems.CollectionChanged += (s, e) =>
            {
                // 新しくインデックスが追加された場合、その中身（Bookmarks）にも監視を付けますわ！
                if (e.NewItems != null)
                {
                    foreach (IndexItem newItem in e.NewItems)
                    {
                        newItem.Bookmarks.CollectionChanged += (bs, be) => SaveConfiguration();
                    }
                }
                // インデックス自体の増減があったので保存します
                SaveConfiguration();
            };
        }

        private void LoadConfiguration()
        {
            if (!File.Exists(ConfigFileName)) return;

            try
            {
                string json = File.ReadAllText(ConfigFileName);
                var items = JsonSerializer.Deserialize<ObservableCollection<IndexItem>>(json);
                if (items != null)
                {
                    IndexItems = items;
                }
            }
            catch (Exception ex)
            {
                // 読み込み失敗時は空のリストから始めますわ
                System.Diagnostics.Debug.WriteLine($"読み込みエラー: {ex.Message}");
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(IndexItems, options);

                // 卿の指定通り、BOM付きのUTF-8で美しく保存して差し上げますわ
                File.WriteAllText(ConfigFileName, json, new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存エラー: {ex.Message}");
            }
        }
    }
}
