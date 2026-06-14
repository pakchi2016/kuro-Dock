using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using Kuro_DockThrone.Core.Models;
using Kuro_DockThrone.Core.Storage;

namespace Kuro_DockGrimoire
{
    public partial class MainWindow : Window
    {
        // ★ Windows APIの召喚：現在のマウス座標を正確に取得します
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
        private bool _isClosing = false;

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
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is BookmarkModel bookmark)
            {
                NavigateExplorer(bookmark.Path);
            }
        }

        private void LoadBookmarks()
        {
            try
            {
                // ★ JSONの存在確認からパスの解決、解読まですべてを玉座の書記官に委ねます
                var multiLevelData = ThroneStorage.LoadBookmarks();

                // 解読された知識を魔法陣（UI）へと注ぎ込みます
                ItemsControl_Indexes.ItemsSource = multiLevelData;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Kuro-Dock Grimoire - 法典エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ★ コンテキストメニューの基本：別の場所がクリックされたら即座に消滅します
        private void Window_Deactivated(object sender, EventArgs e)
        {
            SafeClose(); // 修正：this.Close() から置き換えます
        }

        private void NavigateExplorer(string targetPath)
        {
            if (App.TargetExplorerHwnd == IntPtr.Zero) return;

            // ★ 新設：転移先が現実世界に存在するか確認する防壁ですわ
            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                System.Windows.MessageBox.Show($"指定された座標は既にこの世界から消失していますわ。\n{targetPath}",
                                "Kuro-Dock Grimoire - 転移失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                SafeClose();
                return;
            }

            try
            {
                // ★ COM参照を捨て、OSから直接「Shell.Application」を動的に召喚します
                Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellAppType);
                    dynamic windows = shell.Windows();

                    foreach (dynamic ie in windows)
                    {
                        if (ie != null && new IntPtr((long)ie.HWND) == App.TargetExplorerHwnd)
                        {
                            ie.Navigate(targetPath);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // COMオブジェクトとの通信で予期せぬエラーが起きた場合の最終防壁です
                System.Windows.MessageBox.Show($"転移魔術の詠唱中に未知の妨害を受けましたわ:\n{ex.Message}",
                                "Kuro-Dock Grimoire - 致命的例外", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            SafeClose();
        }

        // ★ 新設：テストボタンが押された時のトリガーです
        private void TestNavigateButton_Click(object sender, RoutedEventArgs e)
        {
            // 仮の転移先として、問答無用で「C:\」を指定してみますわ
            NavigateExplorer(@"C:\");
        }

        // ★ 新設：App（タスクトレイ）から呼び出される再読み込みの窓口ですわ
        public void Reload法典()
        {
            // すでに構築済みの LoadBookmarks を再実行するだけですわね。実にかんたんです
            LoadBookmarks();
        }

        private void SafeClose()
        {
            if (!_isClosing)
            {
                _isClosing = true;
                this.Close();
            }
        }


    }
}