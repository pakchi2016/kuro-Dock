using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Kuro_DockGrimoire
{
    public partial class App : Application
    {
        // ★ Windows APIの召喚：最前面のウィンドウハンドルを取得しますわ
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // 捕捉したHWNDとパスを保持する静的プロパティです
        public static IntPtr TargetExplorerHwnd { get; private set; }
        public static string TargetExplorerPath { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. 最速で現在アクティブなウィンドウ（エクスプローラーのはずです）のHWNDを捕捉します
            TargetExplorerHwnd = GetForegroundWindow();

            // 2. レジストリから %V（パス）が渡された場合、それも記憶します
            if (e.Args.Length > 0)
            {
                TargetExplorerPath = e.Args[0];
            }

            base.OnStartup(e);
        }
    }
}