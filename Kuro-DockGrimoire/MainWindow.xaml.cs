using Kuro_DockGrimoire.Models;
using SHDocVw;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace Kuro_DockGrimoire
{
    public partial class MainWindow : Window
    {
        // ★ Windows APIの召喚：現在のマウス座標を正確に取得します
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadBookmarks();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. マウスの現在位置を取得します
            Win32Point mousePos = new Win32Point();
            GetCursorPos(ref mousePos);

            // 2. その位置にウィンドウを強制転移させます
            this.Left = mousePos.X;
            this.Top = mousePos.Y;

            // 3. App.xaml.csで捕捉した証拠（HWNDとパス）を画面に表示して確認しますわ
            //TextBlock_Hwnd.Text = $"HWND: {App.TargetExplorerHwnd}";
            
            //TextBlock_Path.Text = $"Path: {App.TargetExplorerPath}";
        }

        // ★ メニューがクリックされた時の転移発動です
        private void BookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BookmarkModel bookmark)
            {
                NavigateExplorer(bookmark.Path);
            }
        }

        private void LoadBookmarks()
        {
            // 1. 全アプリ共通の聖域（%APPDATA%\Kuro-Dock）のパスを特定します
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string kuroDockDir = Path.Combine(appDataPath, "Kuro-Dock");
            string jsonPath = Path.Combine(kuroDockDir, "shared_bookmarks.json");

            // 2. フォルダが存在しなければ、新たに領地を開拓します
            if (!Directory.Exists(kuroDockDir))
            {
                Directory.CreateDirectory(kuroDockDir);
            }

            // 3. 法典（JSON）が存在しない場合は、2階層の初期データを錬成します
            if (!File.Exists(jsonPath))
            {
                var defaultData = new List<IndexModel>
                {
                    new IndexModel
                    {
                        Name = "📁 システム領域",
                        Bookmarks = new List<BookmarkModel>
                        {
                            new BookmarkModel { Alias = "◆ Cドライブ", Path = @"C:\" },
                            new BookmarkModel { Alias = "◆ ドキュメント", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }
                        }
                    }
                };

                // 卿の確立した、日本語を美しく保つためのエンコード魔法ですわ
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
                };

                string jsonString = JsonSerializer.Serialize(defaultData, options);
                File.WriteAllText(jsonPath, jsonString, new System.Text.UTF8Encoding(true));
            }

            // 4. 法典の解読（デシリアライズ）を実行し、魔法陣（UI）へ注ぎ込みます
            try
            {
                string jsonContent = File.ReadAllText(jsonPath);

                var readOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // ★ 修正：フラットなListから、2階層の List<IndexModel> へと解読の規格を変更しましたわ
                var multiLevelData = JsonSerializer.Deserialize<List<IndexModel>>(jsonContent, readOptions);

                ItemsControl_Indexes.ItemsSource = multiLevelData;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"法典の読み込みに失敗しましたわ: {ex.Message}", "Kuro-Dock Grimoire", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ★ コンテキストメニューの基本：別の場所がクリックされたら即座に消滅します
        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.Close();
        }

        private void NavigateExplorer(string targetPath)
        {
            if (App.TargetExplorerHwnd == IntPtr.Zero) return;

            // ★ 新設：転移先が現実世界に存在するか確認する防壁ですわ
            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                MessageBox.Show($"指定された座標は既にこの世界から消失していますわ。\n{targetPath}",
                                "Kuro-Dock Grimoire - 転移失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close(); // 失敗を告げた後、美しく姿を消します
                return;
            }

            try
            {
                ShellWindows shellWindows = new ShellWindows();
                foreach (InternetExplorer ie in shellWindows)
                {
                    if (new IntPtr((long)ie.HWND) == App.TargetExplorerHwnd)
                    {
                        ie.Navigate(targetPath);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                // COMオブジェクトとの通信で予期せぬ切断が起きた場合の最終防壁です
                MessageBox.Show($"転移魔術の詠唱中に未知の妨害を受けましたわ:\n{ex.Message}",
                                "Kuro-Dock Grimoire - 致命的例外", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.Close();
        }

        // ★ 新設：テストボタンが押された時のトリガーです
        private void TestNavigateButton_Click(object sender, RoutedEventArgs e)
        {
            // 仮の転移先として、問答無用で「C:\」を指定してみますわ
            NavigateExplorer(@"C:\");
        }
    }
}