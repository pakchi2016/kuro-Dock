using System.Drawing;
using System.Windows;
using System.Windows.Forms; // ★ WinFormsの機能を使いますわ

namespace Kuro_DockGenesis
{
    public partial class App : System.Windows.Application
    {
        // タスクトレイアイコンの正体ですわ
        private NotifyIcon _notifyIcon;

        // アプリ起動時に呼び出される美しいメソッドです
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // タスクトレイアイコンの生成
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "Genesis";

            // アプリケーション自体のアイコン（.exeのアイコン）を自動的に抽出して設定しますわ
            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
            _notifyIcon.Visible = true;

            // 右クリックメニューの構築
            var menu = new ContextMenuStrip();
            var exitItem = new ToolStripMenuItem("終了");

            // 終了がクリックされた時の処理
            exitItem.Click += (s, args) =>
            {
                // WPFアプリケーション全体を優雅に終了させますわ
                Current.Shutdown();
            };

            menu.Items.Add(exitItem);
            _notifyIcon.ContextMenuStrip = menu;
        }

        // アプリ終了時に呼び出されるメソッドです
        protected override void OnExit(ExitEventArgs e)
        {
            // ここを忘れると、アプリを終了してもタスクトレイに透明なアイコンの「亡霊」が残り続けますわよ
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            base.OnExit(e);
        }
    }
}