using Kuro_DockLauncher2.Models;
using Kuro_DockLauncher2.ViewModels;
using System.Windows;
using System.Linq;
using Kuro_DockLauncher2.Models;

namespace Kuro_DockLauncher2.Views
{
    public partial class MainWindow : Window
    {
        private int _currentAnimationId = 0;
        private int _currentSubAnimationId = 0;
        private bool _isContextMenuOpen = false;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 画面の作業領域（タスクバーを除いた領域）を取得しますわ
            var workArea = SystemParameters.WorkArea;
            // ウィンドウの左端 = 画面右端 - ウィンドウの幅
            this.Left = workArea.Right - this.Width;
            // ウィンドウの上端 = 作業領域の上端
            this.Top = workArea.Top;
        }

        // 右クリックメニューの「追加」が押された時の処理（中身は後で作りますわ）
        private void AddIndex_Click(object sender, RoutedEventArgs e)
        {
            var window = new IndexWindow();

            // ポップアップがOKで閉じられた場合のみ処理しますわ
            if (window.ShowDialog() == true)
            {
                // ウィンドウの裏側にいる ViewModel を取り出し、データを追加します
                if (this.DataContext is MainViewModel vm)
                {
                    vm.IndexItems.Add(new Models.IndexItem { Name = window.IndexName });
                }
            }
        }

        // インデックスボタンにファイル/フォルダがドロップされた時の処理ですわ
        private void IndexButton_Drop(object sender, System.Windows.DragEventArgs e)
        {
            // イベントの発生源（ボタン）の裏側にいる DataContext（IndexItem）を優雅に取り出します
            if (sender is System.Windows.FrameworkElement element && element.DataContext is IndexItem indexItem)
            {
                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] droppedPaths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);

                    foreach (string path in droppedPaths)
                    {
                        // 既に同じパスが登録されていないか確認し、重複を防ぐ賢明な処理ですわ
                        if (!indexItem.Bookmarks.Any(b => b.Path == path))
                        {
                            // リストに追加するだけ！
                            // ViewModelで監視させているので、ここで勝手にJSONへの保存が走りますわ！
                            indexItem.Bookmarks.Add(new BookmarkItem { Path = path });
                        }
                    }
                }
            }
        }

        // 右クリックメニューの「終了」が押された時の処理
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        // ★ async を追加し、非同期メソッドに昇華させますわ
        private async void IndexButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && element.DataContext is IndexItem indexItem)
            {
                int animationId = ++_currentAnimationId;

                await System.Threading.Tasks.Task.Delay(150);
                if (animationId != _currentAnimationId) return;

                System.Windows.Point pos = element.TranslatePoint(new System.Windows.Point(0, 0), DockPanel);
                BookmarksPanel.Margin = new Thickness(0, pos.Y - 12, 0, 0);

                if (this.DataContext is MainViewModel vm)
                {
                    // ★ 新しいインデックスにマウスが乗ったので、アニメーションIDを更新します
                    // これにより、もし前のすだれ展開が途中でも、古いループは破棄されますわ
                    //int animationId = ++_currentAnimationId;

                    vm.CurrentBookmarks.Clear();

                    _currentSubAnimationId++;
                    vm.CurrentSubBookmarks.Clear();

                    if (indexItem.Bookmarks.Count > 0)
                    {
                        // ★第一幕：先鋒のボタンだけを右からスライドインさせますわ
                        var firstBookmark = indexItem.Bookmarks[0];
                        firstBookmark.InitialOffsetX = 50; // 右から50pxスライド
                        firstBookmark.InitialOffsetY = 0;  // 縦の動きは無し
                        vm.CurrentBookmarks.Add(firstBookmark);

                        // 先鋒がスライドしきるまで少し長めに待ちますわ（150msのアニメーション＋余韻）
                        await System.Threading.Tasks.Task.Delay(180);

                        // ★第二幕：後続のボタンを上から天津すだれの如く展開しますわ
                        for (int i = 1; i < indexItem.Bookmarks.Count; i++)
                        {
                            if (animationId != _currentAnimationId) break;

                            var bookmark = indexItem.Bookmarks[i];
                            bookmark.InitialOffsetX = 0;   // 横の動きは無し
                            bookmark.InitialOffsetY = -20; // 上から20pxスライド
                            vm.CurrentBookmarks.Add(bookmark);

                            await System.Threading.Tasks.Task.Delay(30);
                        }
                    }
                }
            }
        }

        // ★ ブックマークにマウスが乗った時の処理です
        private async void Bookmark_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && element.DataContext is BookmarkItem bookmark)
            {
                int animationId = ++_currentSubAnimationId;

                await System.Threading.Tasks.Task.Delay(150);
                if (animationId != _currentSubAnimationId) return;

                System.Windows.Point pos = element.TranslatePoint(new System.Windows.Point(0, 0), DockPanel);
                SubBookmarksPanel.Margin = new Thickness(0, pos.Y - 12, 0, 0);

                if (this.DataContext is MainViewModel vm)
                {
                    // 別のブックマークに乗った瞬間、まずは第3階層を綺麗に掃除します
                    vm.CurrentSubBookmarks.Clear();

                    // もし対象がフォルダだった場合のみ、中身を読み取って展開しますわ
                    if (bookmark.IsFolder)
                    {
                        try
                        {
                            // フォルダ内のファイルとフォルダのパスをすべて取得します
                            var entries = System.IO.Directory.EnumerateFileSystemEntries(bookmark.Path).ToList();

                            if (entries.Count > 0)
                            {
                                // ★第一幕：先鋒（1つ目の要素のアイコンを取得して追加しますわ）
                                string firstPath = entries[0];
                                bool isFirstFolder = System.IO.Directory.Exists(firstPath);

                                vm.CurrentSubBookmarks.Add(new BookmarkItem
                                {
                                    Path = firstPath,
                                    InitialOffsetX = 50,
                                    InitialOffsetY = 0,
                                    Icon = Kuro_DockLauncher2.Helpers.IconHelper.GetIcon(firstPath, isFirstFolder) // ★ここに追加しましたわ！
                                });

                                await System.Threading.Tasks.Task.Delay(180);

                                // ★第二幕：後続（2つ目以降の要素も、ループ内で1つずつアイコンを取得しますわ）
                                for (int i = 1; i < entries.Count; i++)
                                {
                                    if (animationId != _currentSubAnimationId) break;

                                    string path = entries[i];
                                    bool isFolder = System.IO.Directory.Exists(path);

                                    vm.CurrentSubBookmarks.Add(new BookmarkItem
                                    {
                                        Path = path,
                                        InitialOffsetX = 0,
                                        InitialOffsetY = -20,
                                        Icon = Kuro_DockLauncher2.Helpers.IconHelper.GetIcon(path, isFolder) // ★ここに追加しましたわ！
                                    });
                                    await System.Threading.Tasks.Task.Delay(30);
                                }
                            }
                        }
                        catch
                        {
                            // アクセス権限がない隠しフォルダ等でエラーが出た場合は、美しく握り潰しますわ
                        }
                    }
                }
            }
        }

        // ★ パネル全体からマウスが完全に離れた時に中身をクリアする美しい処理ですわ
        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // メニューが開いている最中（別名付与など）の「偽のMouseLeave」は完全に無視しますわ！
            if (_isContextMenuOpen) return;

            ClosePanel();
        }

        // ブックマークボタンがクリックされた時の起動処理ですわ
        private void Bookmark_Click(object sender, RoutedEventArgs e)
        {
            // DataContextから、対象のBookmarkItemを取り出します
            if (sender is System.Windows.FrameworkElement element && element.DataContext is BookmarkItem bookmark)
            {
                try
                {
                    // 美しく起動して差し上げますわ
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = bookmark.Path,
                        UseShellExecute = true
                    });
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show($"起動に失敗しましたわ。\n{ex.Message}");
                }
            }
        }

        // ★右クリックメニューから「名前を変更」が選ばれたときの処理です
        private void RenameBookmark_Click(object sender, RoutedEventArgs e)
        {
            // メニューの大元であるボタンの裏側にいる BookmarkItem を取り出します
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is BookmarkItem bookmark)
            {
                // 名前変更ウィンドウを呼び出しますわ
                var window = new RenameWindow(bookmark.Name);
                if (window.ShowDialog() == true)
                {
                    // エイリアスを上書きします！
                    // ここで値を入れた瞬間、第1段階で仕込んだ魔法によって画面の文字が即座に切り替わりますわ
                    bookmark.Alias = window.NewAlias;

                    // 忘れずにJSONへ保存させますわよ
                    if (this.DataContext is MainViewModel vm)
                    {
                        vm.Save();
                    }
                }
            }
        }

        private void DockPanel_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            _isContextMenuOpen = true;
        }

        private void DockPanel_ContextMenuClosing(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            _isContextMenuOpen = false;

            // メニューでの用事が済んだ後、すでにマウスがパネルの外にあれば、ここで優雅に閉じます
            if (!DockPanel.IsMouseOver)
            {
                ClosePanel();
            }
        }

        private void ClosePanel()
        {
            _currentAnimationId++;
            _currentSubAnimationId++;

            if (this.DataContext is MainViewModel vm)
            {
                vm.CurrentBookmarks.Clear();
                vm.CurrentSubBookmarks.Clear();
            }

            // XAMLの代わりに、C#側からスライドアウトのアニメーションを命じます
            var anim = new System.Windows.Media.Animation.DoubleAnimation
            {
                To = 99,
                Duration = new System.Windows.Duration(System.TimeSpan.FromSeconds(0.3)),
                EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseIn }
            };

            var sb = new System.Windows.Media.Animation.Storyboard();
            System.Windows.Media.Animation.Storyboard.SetTarget(anim, DockPanel);
            System.Windows.Media.Animation.Storyboard.SetTargetProperty(anim, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
            sb.Children.Add(anim);
            sb.Begin();
        }
    }
}