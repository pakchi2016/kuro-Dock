using System;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Windows;
using Application = System.Windows.Application;
using Kuro_DockThrone.Core.Communication; // ★玉座の通信法典を召喚しますわ

namespace Kuro_DockGrimoire
{
    public partial class App : Application
    {
        public static IntPtr TargetExplorerHwnd { get; set; }
        public static string TargetExplorerPath { get; set; }

        private const string SummoningPipeName = "KuroDockGrimoire_Pipe";
        private Window _phantomWindow;

        // ★玉座からの思念を受け取るための新しい耳ですわ
        private ThronePipeServer _hubPipeServer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // _phantomWindow が閉じられた時にプロセスを終了する理法です
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

            // 1. 卿のオリジナルの召喚パイプ（エクスプローラーからの呼び出し用）を起動します
            StartSummoningPipeServer();

            // 2. 玉座（Hub）からの命令を受け取るパイプを起動します
            _hubPipeServer = new ThronePipeServer("Grimoire", HandleThroneCommand);
            _hubPipeServer.StartListening();

            // 初回起動時、もし引数（パス）が渡されていればメニューを展開します
            if (!string.IsNullOrEmpty(TargetExplorerPath))
            {
                ShowGrimoire();
            }
        }

        // ====================================================
        // 卿のオリジナル：エクスプローラーからの召喚を受け付ける魔法
        // ====================================================
        private async void StartSummoningPipeServer()
        {
            while (true)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(SummoningPipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
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

        // ====================================================
        // 新規追加：玉座（Hub）からの思念を解釈する魔法
        // ====================================================
        private void HandleThroneCommand(string command)
        {
            // ★ 可視化の光：玉座からの命令が届いたことを証明しますわ

            Dispatcher.Invoke(() =>
            {
                if (command == "Reload")
                {
                    // TODO: 法典（JSON）の再読み込みロジックがあればここに記述しなさいな
                }
                else if (command == "Exit")
                {
                    // 玉座からの自決命令です。
                    // 命綱である _phantomWindow を閉じることで、WPFの理法に従い美しくプロセスを終了させますわ。
                    _hubPipeServer?.StopListening();
                    _phantomWindow?.Close();
                }
            });
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hubPipeServer?.StopListening();
            base.OnExit(e);
        }
    }
}