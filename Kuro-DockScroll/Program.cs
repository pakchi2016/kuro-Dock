using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Kuro_DockScroll
{
    class Program
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        static void Main(string[] args)
        {
            // 起動した瞬間のエクスプローラーの座標（HWND）を捕捉します
            IntPtr hwnd = GetForegroundWindow();
            string path = args.Length > 0 ? args[0] : "";

            // 本体と繋がる亜空間への道ですわ
            string pipeName = "KuroDock_SummoningPipe";

            try
            {
                // 1. メモリ上に展開された本体（Grimoire）を探して思念を送ります
                using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                {
                    // 使役魔は待つことをしません。100ミリ秒で見つからなければ見切りをつけます
                    client.Connect(100);
                    using (var writer = new StreamWriter(client))
                    {
                        writer.AutoFlush = true;
                        writer.Write($"{hwnd.ToInt64()}|{path}");
                    }
                }
            }
            catch (TimeoutException)
            {
                // 2. 本体がメモリ上に存在しない（常駐していない）場合、自らエンジンを着火します
                // ※ ScrollとGrimoireが同じフォルダにいる前提の魔術ですわ
                string grimoireExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Kuro-DockGrimoire.exe");

                if (File.Exists(grimoireExe))
                {
                    // 引数（パス）を渡して重厚な本体を叩き起こします
                    Process.Start(grimoireExe, $"\"{path}\"");
                }
            }
            catch
            {
                // 万が一の未知のエラー時は、OSにエラーを吐かせず静かに散ります
            }
        }
    }
}