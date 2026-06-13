using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

namespace Kuro_DockGrimoire
{
    public class Program
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private const string MutexName = "KuroDockGrimoire_Mutex";
        private const string PipeName = "KuroDockGrimoire_Pipe";
        private static Mutex _mutex;

        [STAThread]
        public static void Main(string[] args)
        {
            // 1. WPFが立ち上がる前に、純粋なC#の層で常駐判定を行います
            bool isFirstInstance = false;
            try
            {
                _mutex = new Mutex(true, MutexName, out isFirstInstance);
            }
            catch (AbandonedMutexException)
            {
                isFirstInstance = true;
            }

            // 2. 影のプロセス（表示用プログラムとしての呼び出し）
            if (!isFirstInstance)
            {
                // WPFを一切呼び出さず、軽量なコンソールアプリのように通信だけして美しく終了します
                IntPtr hwnd = GetForegroundWindow();
                string path = args.Length > 0 ? args[0] : "";
                SendToMainProcess(hwnd, path);

                return; // ここで終了すれば絶対にクラッシュしませんわ
            }

            // 3. 本体プロセス（ブートプログラムとしての常駐化）
            // ここで初めてWPFの世界を召喚します
            var app = new App();
            App.TargetExplorerHwnd = GetForegroundWindow();
            if (args.Length > 0) App.TargetExplorerPath = args[0];

            app.InitializeComponent();
            app.Run(); // WPFのメッセージループを開始します

            // 本体が終了する際のお片付けですわ
            _mutex?.Dispose();
        }

        private static void SendToMainProcess(IntPtr hwnd, string path)
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(1500);
                    using (var writer = new StreamWriter(client))
                    {
                        writer.AutoFlush = true;
                        writer.Write($"{hwnd.ToInt64()}|{path}");
                    }
                }
            }
            catch { /* 影は黙って消えます */ }
        }
    }
}