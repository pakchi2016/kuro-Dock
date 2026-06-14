using Kuro_DockFortress.Models;
using Kuro_DockFortress.ViewModels;
using Kuro_DockThrone.Core.Models;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Kuro_DockFortress.Views
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon _notifyIcon;
        private bool _isExplicitClose = false;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(); // 紐付けの儀式ですわ

            SetupNotifyIcon();
        }

        // ★ 1. リストの項目をダブルクリックした時の処理ですわ
        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is FileItemModel file)
            {
                // クリックされたのが「フォルダ」だった場合のみ、パスを更新して中に入ります
                if (file.IsDirectory)
                {
                    // DataContext（タブのデータ）を取得して、現在のパスを上書きしますわ
                    if (listView.DataContext is TabItemModel tab)
                    {
                        tab.CurrentPath = file.Path;
                        SyncTerminalPath(tab.CurrentPath);
                    }
                }
                else
                {
                    // ファイルだった場合は、とりあえずシステムの標準アプリで開くようにしてあげますわ
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true });
                    }
                    catch { /* 実行できないファイルは優雅に無視します */ }
                }
            }
        }

        // ★ 2. 「↑」ボタンを押した時の処理ですわ
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel tab)
            {
                // 現在のパスの「親ディレクトリ」を取得します
                var parent = Directory.GetParent(tab.CurrentPath);
                if (parent != null)
                {
                    tab.CurrentPath = parent.FullName;
                }
                else
                {
                    tab.CurrentPath = "PC";
                }
            }
        }

        // ★ 3. パスバーで直接文字を打ち込み、Enterキーを押した時の処理ですわ
        private void PathTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (sender is System.Windows.Controls.TextBox tb && tb.Tag is TabItemModel tab)
                {
                    // 打ち込まれたパスが実在するか確認してから移動します
                    if (tb.Text == "PC" || Directory.Exists(tb.Text))
                    {
                        tab.CurrentPath = tb.Text;
                    }
                    else
                    {
                        // 存在しないデタラメなパスだった場合は、元の正しいパスに強制的に戻しますわ
                        tb.Text = tab.CurrentPath;
                    }
                }
            }
        }

        // ★ 4. 「＋」ボタンを押した時の処理ですわ
        private void AddTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel currentTab)
            {
                if (this.DataContext is MainViewModel vm)
                {
                    // 押されたボタンがどちらの陣営に属しているか判定し、現在のパスを引き継いだ新しいタブを生み出します
                    if (vm.LeftTabs.Contains(currentTab))
                    {
                        vm.LeftTabs.Add(new TabItemModel { CurrentPath = currentTab.CurrentPath });
                    }
                    else if (vm.RightTabs.Contains(currentTab))
                    {
                        vm.RightTabs.Add(new TabItemModel { CurrentPath = currentTab.CurrentPath });
                    }
                }
            }
        }

        // ★ 5. タブの「✕」ボタンを押した時の処理ですわ
        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel targetTab)
            {
                if (this.DataContext is MainViewModel vm)
                {
                    // 最後の1つのタブを閉じられてしまうとUIが崩壊するため、1つより多い場合のみ削除を許可しますわ
                    if (vm.LeftTabs.Contains(targetTab) && vm.LeftTabs.Count > 1)
                    {
                        vm.LeftTabs.Remove(targetTab);
                    }
                    else if (vm.RightTabs.Contains(targetTab) && vm.RightTabs.Count > 1)
                    {
                        vm.RightTabs.Remove(targetTab);
                    }
                }
            }
        }

        // ★ 6. 「コピー」を押した時の処理ですわ
        private void CopyItem_Click(object sender, RoutedEventArgs e)
        {
            // メニューの大元であるListViewと、選択されている項目を取得します
            if (sender is System.Windows.Controls.MenuItem menuItem &&
                menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu &&
                contextMenu.PlacementTarget is System.Windows.Controls.ListView listView)
            {
                if (listView.SelectedItem is FileItemModel file)
                {
                    // Windowsのクリップボードが理解できる「ファイルのリスト」形式に変換して登録しますわ
                    var strCollection = new System.Collections.Specialized.StringCollection();
                    strCollection.Add(file.Path);
                    System.Windows.Clipboard.SetFileDropList(strCollection);
                }
            }
        }

        // ★ 7. 「貼り付け」を押した時の処理ですわ
        private void PasteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem &&
                menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu &&
                contextMenu.PlacementTarget is System.Windows.Controls.ListView listView &&
                System.Windows.Clipboard.ContainsFileDropList() && listView.DataContext is TabItemModel tab)
            {
                string targetDir = tab.CurrentPath;
                if (targetDir == "PC" || !Directory.Exists(targetDir)) return;

                // クリップボードのデータが「切り取り」かどうかを魔術から判定しますわ
                bool isCut = false;
                var dataObj = System.Windows.Clipboard.GetDataObject();
                if (dataObj != null && dataObj.GetDataPresent("Preferred DropEffect"))
                {
                    if (dataObj.GetData("Preferred DropEffect") is MemoryStream ms)
                    {
                        byte[] bytes = ms.ToArray();
                        if (bytes.Length > 0 && bytes[0] == 2) isCut = true;
                    }
                }

                var files = System.Windows.Clipboard.GetFileDropList();
                foreach (string sourcePath in files)
                {
                    try
                    {
                        string destPath = Path.Combine(targetDir, Path.GetFileName(sourcePath));
                        if (sourcePath == destPath) continue; // 同一場所への貼り付けは一旦無視します

                        if (isCut)
                        {
                            // ドライブまたぎの移動でエラーが出た場合はコピー＆削除にフォールバックします
                            try
                            {
                                if (Directory.Exists(sourcePath)) Directory.Move(sourcePath, destPath);
                                else if (File.Exists(sourcePath)) File.Move(sourcePath, destPath);
                            }
                            catch (IOException)
                            {
                                if (Directory.Exists(sourcePath)) { CopyDirectory(sourcePath, destPath); Directory.Delete(sourcePath, true); }
                                else if (File.Exists(sourcePath)) { File.Copy(sourcePath, destPath, false); File.Delete(sourcePath); }
                            }
                        }
                        else
                        {
                            if (Directory.Exists(sourcePath)) CopyDirectory(sourcePath, destPath);
                            else if (File.Exists(sourcePath)) File.Copy(sourcePath, destPath, false);
                        }
                    }
                    catch { /* 握り潰しますわ */ }
                }

                // 切り取りだった場合は、二重貼り付けを防ぐためクリップボードを浄化します
                if (isCut) System.Windows.Clipboard.Clear();

                tab.CurrentPath = targetDir;
            }
        }

        // ★ 8. フォルダを中身ごとすべて複製する絶対命令（再帰メソッド）ですわ
        private void CopyDirectory(string sourceDir, string destinationDir)
        {
            // まずは器となるフォルダを作ります
            Directory.CreateDirectory(destinationDir);

            // 直下のファイルをすべてコピーします
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                File.Copy(file, Path.Combine(destinationDir, Path.GetFileName(file)), false);
            }

            // 直下のサブフォルダ群に対しても、自身を呼び出して再帰的にコピーさせますわ
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                CopyDirectory(dir, Path.Combine(destinationDir, Path.GetFileName(dir)));
            }
        }

        // ★ 9. 右クリックメニューが開かれた瞬間の処理ですわ
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.ContextMenu menu)
            {
                // クリップボードにファイルが存在するかどうかを判定します
                bool hasFiles = System.Windows.Clipboard.ContainsFileDropList();

                foreach (object item in menu.Items)
                {
                    // 「貼り付け」という名前のメニューを探し出し、状態を上書きしますわ
                    if (item is System.Windows.Controls.MenuItem menuItem && menuItem.Header.ToString() == "貼り付け")
                    {
                        menuItem.IsEnabled = hasFiles;
                    }
                }
            }
        }

        // ★ 10. 「切り取り」を押した時の処理ですわ
        private void CutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem &&
                menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu &&
                contextMenu.PlacementTarget is System.Windows.Controls.ListView listView &&
                listView.SelectedItem is FileItemModel file)
            {
                var strCollection = new System.Collections.Specialized.StringCollection { file.Path };

                // ただのコピーではなく「切り取り（移動）」であることを示すWindowsの黒魔術です
                var data = new System.Windows.DataObject();
                data.SetFileDropList(strCollection);

                byte[] moveEffect = new byte[] { 2, 0, 0, 0 }; // 2 = DROPEFFECT_MOVE
                MemoryStream dropEffect = new MemoryStream(moveEffect);
                data.SetData("Preferred DropEffect", dropEffect);

                System.Windows.Clipboard.SetDataObject(data, true);
            }
        }

        // ★ 11. 「削除」を押した時の処理ですわ
        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem &&
                menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu &&
                contextMenu.PlacementTarget is System.Windows.Controls.ListView listView &&
                listView.SelectedItem is FileItemModel file &&
                listView.DataContext is TabItemModel tab)
            {
                var result = System.Windows.MessageBox.Show($"本当に '{file.Name}' を削除してもよろしいですか？\n※ごみ箱には入らず完全に消去されますわよ。",
                                             "削除の確認", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    try
                    {
                        if (file.IsDirectory) Directory.Delete(file.Path, true);
                        else File.Delete(file.Path);

                        // 現在のパスを再代入してリストをリロードしますわ
                        tab.CurrentPath = tab.CurrentPath;
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"削除に失敗しましたわ: {ex.Message}", "エラー", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        // ★ 12. 新規フォルダ（一括・階層構築）の処理ですわ
        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem &&
                menuItem.Parent is System.Windows.Controls.MenuItem parentMenu &&
                parentMenu.Parent is System.Windows.Controls.ContextMenu contextMenu &&
                contextMenu.PlacementTarget is System.Windows.Controls.ListView listView &&
                listView.DataContext is TabItemModel tab)
            {
                if (tab.CurrentPath == "PC") return; // PC画面での創造は禁忌ですわ

                // 複数行モード(true)でダイアログを呼び出します
                var dialog = new InputDialog("作成するフォルダ名を入力してください。\n（改行で複数作成、\\ で階層作成が可能ですわ）", "新規フォルダ作成", true)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    // 改行コードで区切って配列化します
                    string[] lines = dialog.InputText.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        try
                        {
                            string targetPath = Path.Combine(tab.CurrentPath, line.Trim());
                            // ★ 魔法の一撃。test1\test2 もこれで完璧に構築されますわ
                            Directory.CreateDirectory(targetPath);
                        }
                        catch { /* 記号エラー等は美しく握り潰しますわ */ }
                    }

                    tab.CurrentPath = tab.CurrentPath; // リストを最新化
                }
            }
        }

        // ★ 13. 新規テキストファイル作成の処理ですわ
        private void CreateText_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem &&
                menuItem.Parent is System.Windows.Controls.MenuItem parentMenu &&
                parentMenu.Parent is System.Windows.Controls.ContextMenu contextMenu &&
                contextMenu.PlacementTarget is System.Windows.Controls.ListView listView &&
                listView.DataContext is TabItemModel tab)
            {
                if (tab.CurrentPath == "PC") return;

                // 単一行モード(false)でダイアログを呼び出します
                var dialog = new InputDialog("作成するテキストファイル名を入力してください。\n（拡張子がなければ自動で .txt を付与しますわ）", "新規テキスト作成", false)
                {
                    Owner = this
                };

                if (dialog.ShowDialog() == true)
                {
                    string fileName = dialog.InputText.Trim();
                    if (string.IsNullOrEmpty(fileName)) return;

                    // 拡張子の補完機能ですわ
                    if (!fileName.EndsWith(".txt", System.StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".txt";
                    }

                    try
                    {
                        string targetPath = Path.Combine(tab.CurrentPath, fileName);

                        // ファイルを生成し、直後に掴んでいるプロセス（Lock）を解放しますわ
                        using (File.Create(targetPath)) { }
                    }
                    catch { /* 握り潰しますわ */ }

                    tab.CurrentPath = tab.CurrentPath; // リストを最新化
                }
            }
        }

        // ★ 14. 現在地をブックマークに登録する処理ですわ
        private void AddBookmark_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel tab && this.DataContext is MainViewModel vm)
            {
                if (tab.CurrentPath == "PC") return; // PC画面は登録不可です

                vm.ReloadBookmarks(); // 登録直前に最新の法典を取得し、競合を防ぎます
                if (vm.CoreBookmarks == null || vm.CoreBookmarks.Count == 0) return;

                // 階層構造のため、とりあえず「一番上のインデックス（システム領域等）」に登録しますわ
                var targetIndex = vm.CoreBookmarks[0];

                // 既にエコシステム全体で同じパスが登録されていないか確認します
                bool exists = false;
                foreach (var index in vm.CoreBookmarks)
                {
                    if (index.Bookmarks.Any(b => b.Path == tab.CurrentPath)) exists = true;
                }

                if (!exists)
                {
                    // AliasではなくDisplayName側で補完されるよう、スマートに登録しますわ
                    targetIndex.Bookmarks.Add(new BookmarkModel { Alias = tab.Header, Path = tab.CurrentPath });
                    vm.SaveBookmarks();
                }
            }
        }

        // ★ 15. ブックマーク一覧を展開する処理ですわ
        private void ShowBookmarks_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel tab && this.DataContext is MainViewModel vm)
            {
                // ★ 展開する「その瞬間」に玉座から法典を読み込みます！
                // これにより、Genesis等で編集した結果が「再起動なし」で即座にメニューに反映されますわ！
                vm.ReloadBookmarks();
                var menu = new System.Windows.Controls.ContextMenu();

                if (vm.CoreBookmarks == null || vm.CoreBookmarks.Count == 0)
                {
                    menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "ブックマークは空ですわ", IsEnabled = false });
                }
                else
                {
                    // インデックス（親フォルダ）ごとにメニューを生成します
                    foreach (var index in vm.CoreBookmarks)
                    {
                        var parentItem = new System.Windows.Controls.MenuItem { Header = index.Name };

                        foreach (var bm in index.Bookmarks.ToList())
                        {
                            // Aliasが空でも美しく表示される玉座の DisplayName プロパティを使いますわ
                            var childItem = new System.Windows.Controls.MenuItem { Header = bm.DisplayName };

                            // ① 左クリック：そのパスへ移動します
                            childItem.Click += (s, args) =>
                            {
                                if (Directory.Exists(bm.Path)) tab.CurrentPath = bm.Path;
                            };

                            // ② 右クリック：そのブックマークを削除します
                            childItem.MouseRightButtonUp += (s, args) =>
                            {
                                index.Bookmarks.Remove(bm);
                                vm.SaveBookmarks();
                                menu.IsOpen = false;
                                args.Handled = true;
                            };

                            parentItem.Items.Add(childItem);
                        }

                        // インデックスの中身が空だった場合の美しい配慮ですわ
                        if (parentItem.Items.Count == 0)
                        {
                            parentItem.Items.Add(new System.Windows.Controls.MenuItem { Header = "空ですわ", IsEnabled = false });
                        }

                        menu.Items.Add(parentItem);
                    }
                }

                menu.PlacementTarget = btn;
                menu.IsOpen = true;
            }
        }

        // ★ 16. ターミナルの現在地をUIと強制同期させるメソッドですわ
        private void SyncTerminalPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path == "PC") return;
            BottomTerminal.ExecuteCommand($"cd \"{path}\"");
        }

        private void SetupNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();

            // 実行ファイル自身のアイコンを抽出してタスクトレイに表示させますわ
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _notifyIcon.Text = "Fortress";
            _notifyIcon.Visible = true;

            // アイコンをダブルクリックした際も表示させます
            _notifyIcon.DoubleClick += (s, e) => ShowWindow();

            // 右クリックメニューの構築です
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var showItem = new System.Windows.Forms.ToolStripMenuItem("表示");
            showItem.Click += (s, e) => ShowWindow();

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("終了");
            exitItem.Click += (s, e) =>
            {
                // 真の終了フラグを立て、アイコンを片付けてから要塞を完全に沈めます
                _isExplicitClose = true;
                _notifyIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            };

            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        // ★ 要塞を最前面に浮上させる処理ですわ
        private void ShowWindow()
        {
            this.Show();
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }
            this.Activate(); // 最前面に持ってくる絶対命令です
        }

        // ★ 「×」ボタンが押された時の処理ですわ
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // タスクトレイからの「終了」以外は、すべて「隠す」処理にすり替えます
            if (!_isExplicitClose)
            {
                e.Cancel = true; // 終了処理をキャンセル（無効化）します
                this.Hide();     // 姿を消しますわ
            }
        }
    }
}