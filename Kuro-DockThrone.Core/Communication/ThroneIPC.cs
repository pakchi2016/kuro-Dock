using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // ★ MessageBoxを召喚するための絶対の記述ですわ

namespace Kuro_DockThrone.Core.Communication
{
    // --- 指令を送る玉座の口（Hubが使役します） ---
    public static class ThronePipeClient
    {
        public static async Task SendCommandAsync(string targetApp, string command)
        {
            string pipeName = $"KuroDock_Pipe_{targetApp}";
            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous);

                // 接続を待ちます
                await client.ConnectAsync(1000);

                using var writer = new StreamWriter(client) { AutoFlush = true };
                await writer.WriteLineAsync(command);
            }
            catch (TimeoutException)
            {
                // ターゲットが眠っている（起動していない）状態ですわ。美しく無視します。
            }
            catch (Exception ex)
            {
                // ★ Client側（送信側）のエラー暴露です。ここは targetApp が存在します。
                MessageBox.Show($"[{targetApp}] への通信失敗: {ex.Message}", "Throne IPC 送信警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    // --- 指令を受信する魔導具の耳（Grimoire等が使役します） ---
    public class ThronePipeServer
    {
        private readonly string _pipeName;
        private readonly string _appName; // ★ エラー表示用に自身の手足を記憶させます
        private readonly Action<string> _onCommandReceived;
        private CancellationTokenSource? _cts;

        public ThronePipeServer(string appName, Action<string> onCommandReceived)
        {
            _appName = appName;
            _pipeName = $"KuroDock_Pipe_{appName}";
            _onCommandReceived = onCommandReceived;
        }

        public void StartListening()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ListenLoop(_cts.Token));
        }

        public void StopListening()
        {
            _cts?.Cancel();
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                    await server.WaitForConnectionAsync(token);

                    using var reader = new StreamReader(server);
                    string? command = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        // 受け取った思念をUIスレッド（各魔導具のメインロジック）へ引き渡しますわ
                        _onCommandReceived?.Invoke(command);
                    }
                }
                catch (OperationCanceledException)
                {
                    break; // 終了命令による美しい切断ですわ
                }
                catch (Exception ex)
                {
                    // ★ Server側（受信側）のエラー暴露です。ここでは targetApp ではなく _appName を使います！
                    //MessageBox.Show($"[{_appName}] の受信エラー: {ex.Message}", "Throne IPC 受信警告", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}