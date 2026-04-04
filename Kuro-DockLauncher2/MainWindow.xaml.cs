using KuroDockLauncher2.Index;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Diagnostics; // Process.Start用
using System.Windows.Media; // Brushes用

namespace KuroDockLauncher
{
    public partial class MainWindow : Window
    {
        private const string ConfigFileName = "config.json";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 画面の「作業領域（タスクバーを除いた領域）」を取得します
            var workArea = SystemParameters.WorkArea;
            // ウィンドウの高さを画面の高さに合わせます
            this.Height = workArea.Height;
            // ウィンドウの左位置 = 画面右端 - ウィンドウの幅
            this.Left = workArea.Right - this.Width;
            // ウィンドウの上位置 = 作業領域の上端
            this.Top = workArea.Top;

            LoadConfiguration();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveConfiguration();
            Application.Current.Shutdown();
        }

        private void AddFolderIndex_Click(object sender, RoutedEventArgs e)
        {
            var indexdoc = new IndexFolderControl();
            indexdoc.ShowDialog();
        }

        private void AddFileIndex_Click(object sender, RoutedEventArgs e)
        {
            var indexdoc = new IndexFileControl();
            indexdoc.ShowDialog();
        }

        private void ClearPanel(object sender, MouseEventArgs e)
        {
            var middleFolderPanel = (StackPanel)this.FindName("FolderMiddlePanel");
            if (middleFolderPanel != null) middleFolderPanel.Children.Clear();
            var middleFilePanel = (StackPanel)this.FindName("FileMiddlePanel");
            if (middleFilePanel != null) middleFilePanel.Children.Clear();
            var pulldown = (StackPanel)this.FindName("FolderPulldown");
            if (pulldown != null) pulldown.Children.Clear();
        }

        private void SaveConfiguration()
        {
            var config = new AppConfig();

            // 1. フォルダインデックスの保存
            // IndexFolderPanel内のボタンを探してデータを抽出
            var folderPanel = (StackPanel)this.FindName("IndexFolderPanel");
            if (folderPanel != null)
            {
                foreach (var child in folderPanel.Children)
                {
                    // 追加されているのはButtonのみ、末尾のBorder等は除外
                    if (child is Button btn)
                    {
                        var data = new IndexData
                        {
                            Name = btn.Content.ToString(),
                            // Tagには List<string> が入っているはずですわ
                            Paths = btn.Tag as List<string> ?? new List<string>()
                        };
                        config.FolderIndexes.Add(data);
                    }
                }
            }

            // 2. ファイルインデックスの保存
            var filePanel = (StackPanel)this.FindName("IndexFilePanel");
            if (filePanel != null)
            {
                foreach (var child in filePanel.Children)
                {
                    if (child is Button btn)
                    {
                        var data = new IndexData
                        {
                            Name = btn.Content.ToString(),
                            Paths = btn.Tag as List<string> ?? new List<string>()
                        };
                        config.FileIndexes.Add(data);
                    }
                }
            }

            // 3. JSONファイルへの書き出し
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true }; // 読みやすく整形
                string jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(ConfigFileName, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の保存に失敗しましたわ。\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadConfiguration()
        {
            if (!File.Exists(ConfigFileName)) return;

            try
            {
                string jsonString = File.ReadAllText(ConfigFileName);
                var config = JsonSerializer.Deserialize<AppConfig>(jsonString);

                if (config == null) return;

                // 1. フォルダインデックスの復元
                var folderPanel = (StackPanel)this.FindName("IndexFolderPanel");
                if (folderPanel != null && config.FolderIndexes != null)
                {
                    // 既存のボタンがあればクリア（ボーダーなどは残すため、挿入位置を考慮）
                    // ここでは単純に追加していきます（既存のUI設計に合わせて挿入位置を調整）
                    // 元のコードでは panel.Children.Insert(panelIndex, IndexButton); を使用していました
                    // 末尾のContextMenu用Borderより前に追加する必要があります
                    int insertIndex = folderPanel.Children.Count > 0 ? folderPanel.Children.Count - 1 : 0;

                    foreach (var data in config.FolderIndexes)
                    {
                        CreateAndAddIndexButton(folderPanel, data, true, insertIndex);
                        insertIndex++;
                    }
                }

                // 2. ファイルインデックスの復元
                var filePanel = (StackPanel)this.FindName("IndexFilePanel");
                if (filePanel != null && config.FileIndexes != null)
                {
                    int insertIndex = filePanel.Children.Count > 0 ? filePanel.Children.Count - 1 : 0;

                    foreach (var data in config.FileIndexes)
                    {
                        CreateAndAddIndexButton(filePanel, data, false, insertIndex);
                        insertIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の読み込みに失敗しましたわ。\n{ex.Message}");
            }
        }
        private void CreateAndAddIndexButton(StackPanel panel, IndexData data, bool isFolder, int insertIndex)
        {
            Button indexButton = new Button
            {
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 1, 0, 0),
                Content = data.Name,
                Tag = data.Paths, // パスリストを復元
                AllowDrop = true
            };

            // フォルダ用とファイル用でスタイルや挙動が少し違うため分岐
            if (isFolder)
            {
                indexButton.Background = Brushes.AliceBlue;
                indexButton.Drop += FolderIndex_Drop; // フォルダ用のドロップ処理
            }
            else
            {
                indexButton.Drop += FileIndex_Drop;   // ファイル用のドロップ処理
            }

            // 共通のイベント
            indexButton.MouseEnter += IndexButton_MouseEnter; // 中身を表示する処理

            // 右クリックメニュー（削除）の再構築
            ContextMenu menu = new ContextMenu();
            MenuItem removeItem = new MenuItem();
            removeItem.Header = isFolder ? "フォルダインデックスボタン削除" : "ファイルインデックスボタン削除";

            // 削除時のイベントハンドラ
            removeItem.Click += (s, args) =>
            {
                panel.Children.Remove(indexButton);
                // 対応する表示パネルもクリア
                if (isFolder)
                {
                    var middle = (StackPanel)this.FindName("FolderMiddlePanel");
                    middle?.Children.Clear();
                    var side = (StackPanel)this.FindName("FolderPulldown");
                    side?.Children.Clear();
                }
                else
                {
                    var middle = (StackPanel)this.FindName("FileMiddlePanel");
                    middle?.Children.Clear();
                }
            };
            menu.Items.Add(removeItem);
            indexButton.ContextMenu = menu;

            // パネルに追加
            if (insertIndex >= 0 && insertIndex < panel.Children.Count)
                panel.Children.Insert(insertIndex, indexButton);
            else
                panel.Children.Add(indexButton);
        }

        private void FolderIndex_Drop(object sender, DragEventArgs e)
        {
            Button button = (Button)sender;
            List<string> pathList = button.Tag as List<string> ?? new List<string>();

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (Directory.Exists(file)) pathList.Add(file); // フォルダのみ追加
                }
                button.Tag = pathList;

                // 即座に表示を更新
                RefreshMiddlePanel(pathList, "FolderMiddlePanel", "FolderPulldown");
            }
        }

        // ファイルインデックスへのドロップ処理
        private void FileIndex_Drop(object sender, DragEventArgs e)
        {
            Button button = (Button)sender;
            List<string> pathList = button.Tag as List<string> ?? new List<string>();

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    if (File.Exists(file)) pathList.Add(file); // ファイルのみ追加
                }
                button.Tag = pathList;

                // 即座に表示を更新
                RefreshMiddlePanel(pathList, "FileMiddlePanel", null);
            }
        }

        // マウスオーバーで中身を表示する処理
        private void IndexButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) return;

            if (sender is Button indexButton && indexButton.Tag is List<string> pathList)
            {
                // 親パネルを判定して、対象のMiddlePanelを決める
                var parentPanel = VisualTreeHelper.GetParent(indexButton) as StackPanel;
                if (parentPanel != null)
                {
                    if (parentPanel.Name == "IndexFolderPanel")
                        RefreshMiddlePanel(pathList, "FolderMiddlePanel", "FolderPulldown");
                    else if (parentPanel.Name == "IndexFilePanel")
                        RefreshMiddlePanel(pathList, "FileMiddlePanel", null);
                }
            }
        }

        // MiddlePanelに中身（ブックマークボタン）を展開するヘルパー
        private void RefreshMiddlePanel(List<string> pathList, string panelName, string sidePanelName)
        {
            var panel = (StackPanel)this.FindName(panelName);
            if (panel == null) return;

            panel.Children.Clear();

            // サイドパネル（プルダウン）があればクリア
            if (!string.IsNullOrEmpty(sidePanelName))
            {
                var side = (StackPanel)this.FindName(sidePanelName);
                side?.Children.Clear();
            }

            if (pathList == null || pathList.Count == 0) return;

            foreach (string entry in pathList)
            {
                CreateBookmarkItemButton(entry, panel, sidePanelName);
            }
        }

        // 個別のファイル/フォルダボタンを作成
        private void CreateBookmarkItemButton(string path, StackPanel parentPanel, string sidePanelName)
        {
            Button newbutton = new Button
            {
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 1, 0, 0),
                Tag = path,
                Content = System.IO.Path.GetFileName(path),
            };

            newbutton.Click += BookmarkItem_Click;

            // フォルダかつサイドパネル指定がある場合はマウスオーバー処理を追加
            if (!string.IsNullOrEmpty(sidePanelName) && Directory.Exists(path))
            {
                newbutton.MouseEnter += (s, e) =>
                {
                    ShowFolderContentsInSidePanel(path, sidePanelName);
                };
            }

            parentPanel.Children.Add(newbutton);
        }

        // ブックマークボタンクリック（ファイルを開く）
        private void BookmarkItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = btn.Tag.ToString(),
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"起動できませんでしたわ: {ex.Message}");
                }
            }
        }

        // フォルダの中身をサイドパネルに表示する処理
        private void ShowFolderContentsInSidePanel(string folderPath, string sidePanelName)
        {
            var sidePanel = (StackPanel)this.FindName(sidePanelName);
            if (sidePanel == null) return;

            sidePanel.Children.Clear();

            try
            {
                foreach (string entry in Directory.EnumerateFileSystemEntries(folderPath))
                {
                    Button newButton = new Button
                    {
                        Width = 90,
                        Height = 30,
                        Margin = new Thickness(0, 1, 0, 0),
                        Content = System.IO.Path.GetFileName(entry),
                        Tag = entry
                    };
                    newButton.Click += BookmarkItem_Click;
                    sidePanel.Children.Add(newButton);
                }
            }
            catch { /* アクセス権限などで読めない場合は無視 */ }
        }

    }

    // 保存するデータ構造の定義
    public class AppConfig
    {
        public List<IndexData> FolderIndexes { get; set; } = new List<IndexData>();
        public List<IndexData> FileIndexes { get; set; } = new List<IndexData>();
    }

    public class IndexData
    {
        public string Name { get; set; }
        public List<string> Paths { get; set; }
    }
}