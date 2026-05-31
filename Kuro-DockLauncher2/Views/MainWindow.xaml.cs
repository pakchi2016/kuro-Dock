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
                if (this.DataContext is MainViewModel vm)
                {
                    // ★ 新しいインデックスにマウスが乗ったので、アニメーションIDを更新します
                    // これにより、もし前のすだれ展開が途中でも、古いループは破棄されますわ
                    int animationId = ++_currentAnimationId;

                    vm.CurrentBookmarks.Clear();

                    _currentSubAnimationId++;
                    vm.CurrentSubBookmarks.Clear();

                    // データを1つずつ、ディレイをかけながら追加していきますわ
                    foreach (var bookmark in indexItem.Bookmarks)
                    {
                        // 展開中にマウスが別の場所へ移動していたら、即座に追加を中止する安全装置です
                        if (animationId != _currentAnimationId) break;

                        vm.CurrentBookmarks.Add(bookmark);

                        // ★ ここが「天津すだれ」の極意！ 30ミリ秒だけ待ってから次を追加します
                        // この時間が短いほど速く、長いほどゆっくりと展開されますわ
                        await System.Threading.Tasks.Task.Delay(30);
                    }
                }
            }
        }

        // ★ ブックマークにマウスが乗った時の処理です
        private async void Bookmark_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && element.DataContext is BookmarkItem bookmark)
            {
                if (this.DataContext is MainViewModel vm)
                {
                    int animationId = ++_currentSubAnimationId;

                    // 別のブックマークに乗った瞬間、まずは第3階層を綺麗に掃除します
                    vm.CurrentSubBookmarks.Clear();

                    // もし対象がフォルダだった場合のみ、中身を読み取って展開しますわ
                    if (bookmark.IsFolder)
                    {
                        try
                        {
                            // フォルダ内のファイルとフォルダのパスをすべて取得します
                            var entries = System.IO.Directory.EnumerateFileSystemEntries(bookmark.Path);

                            foreach (string path in entries)
                            {
                                // アニメーション中にマウスが外れたら即座に中止する安全装置です
                                if (animationId != _currentSubAnimationId) break;

                                vm.CurrentSubBookmarks.Add(new BookmarkItem { Path = path });
                                await System.Threading.Tasks.Task.Delay(30);
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
        private void DockPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // マウスが離れたので、展開中のアニメーションを強制停止させるためにIDを更新します
            _currentAnimationId++;
            _currentSubAnimationId++;

            if (this.DataContext is MainViewModel vm) 
            {
                vm.CurrentBookmarks.Clear();
                vm.CurrentSubBookmarks.Clear();
            }
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
    }
}