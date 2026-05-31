using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace Kuro_DockLauncher2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private NotifyIcon _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // タスクトレイアイコンの生成
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Text = "KuroDock";

            // アプリケーション自体のアイコン（.exeのアイコン）を自動的に抽出して設定しますわ
            _notifyIcon.Icon = Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
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
