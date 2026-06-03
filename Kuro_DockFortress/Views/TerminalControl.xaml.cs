using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kuro_DockFortress.Views
{
    public partial class TerminalControl : System.Windows.Controls.UserControl
    {
        private Process _process;
        private StreamWriter _streamWriter;

        public TerminalControl()
        {
            InitializeComponent();
            StartPowerShell();
        }

        private void StartPowerShell()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                // ★ 最重要：起動直後にコンソールの文字コードをUTF-8に強制し、SSHの文字化けを粉砕しますわ！
                Arguments = "-NoExit -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8, // C#側の受け取りもUTF8に設定します
                StandardErrorEncoding = Encoding.UTF8
            };

            _process = new Process { StartInfo = startInfo };
            _process.OutputDataReceived += (s, e) => AppendOutput(e.Data);
            _process.ErrorDataReceived += (s, e) => AppendOutput(e.Data);

            _process.Start();
            _streamWriter = _process.StandardInput;
            _streamWriter.AutoFlush = true;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        private void AppendOutput(string text)
        {
            if (text == null) return;
            // 別スレッドからUIを操作するためのお作法ですわ
            Dispatcher.InvokeAsync(() =>
            {
                OutputBox.AppendText(text + Environment.NewLine);
                OutputBox.ScrollToEnd();
            });
        }

        // 卿がEnterを押した時、コマンドを裏のPowerShellに流し込みます
        private void InputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string cmd = InputBox.Text;
                if (string.IsNullOrWhiteSpace(cmd)) return;

                AppendOutput($"\nPS > {cmd}");
                _streamWriter.WriteLine(cmd);
                InputBox.Clear();
            }
        }

        // 外部（ファイラー側）からコマンドを流し込むための公開メソッドですわ
        public void ExecuteCommand(string cmd)
        {
            if (_streamWriter != null)
            {
                _streamWriter.WriteLine(cmd);
            }
        }

        // 要塞を閉じる際、裏のPowerShellも道連れにして綺麗に終了させます
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try { if (_process != null && !_process.HasExited) _process.Kill(); }
            catch { /* 握り潰しますわ */ }
        }
    }
}