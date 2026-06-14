using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Kuro_DockGenesis.Models;
using Kuro_DockThrone.Core.Models;
using Kuro_DockThrone.Core.Storage;

namespace Kuro_DockGenesis.ViewModels
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
            try
            {
                // 1. 玉座（Throne.Core）から、すべての魔導具で共通の純粋なデータを読み込みます
                var coreData = ThroneStorage.LoadBookmarks();

                // 2. それを Genesis の画面表示用モデル（アニメーション用プロパティを持つモデル）へ美しく変換します
                IndexItems = new ObservableCollection<Kuro_DockGenesis.Models.IndexItem>(
                    coreData.Select(coreIndex => new Kuro_DockGenesis.Models.IndexItem
                    {
                        Name = coreIndex.Name,
                        Bookmarks = new ObservableCollection<Kuro_DockGenesis.Models.BookmarkItem>(
                            coreIndex.Bookmarks.Select(coreBookmark => new Kuro_DockGenesis.Models.BookmarkItem
                            {
                                Alias = coreBookmark.Alias,
                                Path = coreBookmark.Path
                            })
                        )
                    })
                );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"玉座からの読み込みエラー: {ex.Message}");
                IndexItems = new ObservableCollection<Kuro_DockGenesis.Models.IndexItem>();
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                // 1. Genesis の画面用モデルから、玉座へ献上するための「純粋なデータモデル」へ逆変換します
                var coreData = IndexItems.Select(uiIndex => new Kuro_DockThrone.Core.Models.IndexModel
                {
                    Name = uiIndex.Name,
                    Bookmarks = uiIndex.Bookmarks.Select(uiBookmark => new Kuro_DockThrone.Core.Models.BookmarkModel
                    {
                        Alias = uiBookmark.Alias,
                        Path = uiBookmark.Path
                    }).ToList()
                }).ToList();

                // 2. 玉座の書記官に、共通法典（shared_bookmarks.json）への書き込みを委ねます
                ThroneStorage.SaveBookmarks(coreData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"玉座への保存エラー: {ex.Message}");
            }
        }
        public void Save() => SaveConfiguration();
    }
}