using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Kuro_DockGrimoire
{
    public partial class App : Application
    {
        public static IntPtr TargetExplorerHwnd { get; set; }
        public static string TargetExplorerPath { get; set; }

        private const string PipeName = "KuroDock_SummoningPipe";
        private NotifyIcon _notifyIcon;
        private Window _phantomWindow;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            this.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // 不可視のアンカー（ブートプロセスの命綱）を錬成します
            _phantomWindow = new Window
            {
                Title = "Kuro-Dock Grimoire Phantom Host",
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent,
                Opacity = 0,
                Top = -10000,
                Left = -10000
            };
            this.MainWindow = _phantomWindow;
            _phantomWindow.Show();

            SetupTaskTray();
            StartPipeServer();

            // 初回起動時、もし引数（パス）が渡されていればメニューを展開します
            if (!string.IsNullOrEmpty(TargetExplorerPath))
            {
                ShowGrimoire();
            }
        }

        private async void StartPipeServer()
        {
            while (true)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        await server.WaitForConnectionAsync();
                        using (var reader = new StreamReader(server))
                        {
                            string message = await reader.ReadToEndAsync();

                            if (!string.IsNullOrEmpty(message))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    var parts = message.Split('|');
                                    if (parts.Length >= 2 && long.TryParse(parts[0], out long hwndVal))
                                    {
                                        TargetExplorerHwnd = new IntPtr(hwndVal);
                                        TargetExplorerPath = parts[1];
                                        ShowGrimoire();
                                    }
                                });
                            }
                        }
                    }
                }
                catch
                {
                    await Task.Delay(100);
                }
            }
        }

        private void ShowGrimoire()
        {
            try
            {
                var window = new MainWindow();
                window.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"魔法陣の展開中に未知の妨害を受けましたわ:\n{ex.Message}", "Grimoire", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupTaskTray()
        {
            var contextMenu = new ContextMenuStrip();

            var reloadMenu = new ToolStripMenuItem("法典（JSON）再読み込み");
            reloadMenu.Click += (s, e) => {
                _notifyIcon.ShowBalloonTip(2000, "Kuro-Dock Grimoire", "次回展開時に最新の法典が適用されますわ。", ToolTipIcon.Info);
            };
            contextMenu.Items.Add(reloadMenu);

            contextMenu.Items.Add(new ToolStripSeparator());

            var exitMenu = new ToolStripMenuItem("終了");
            exitMenu.Click += (s, e) => {
                _notifyIcon.Dispose();
                _phantomWindow.Close();
            };
            contextMenu.Items.Add(exitMenu);

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Shield,
                ContextMenuStrip = contextMenu,
                Text = "Grimoire",
                Visible = true
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            base.OnExit(e);
        }
    }
}