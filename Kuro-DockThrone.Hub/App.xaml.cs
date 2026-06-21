using System;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using Kuro_DockThrone.Core.Communication;

namespace Kuro_DockThrone.Hub
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;

        // ★ メモリリーク（ハンドルの残骸）を完全に消し去るためのWindows APIですわ
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _notifyIcon = new NotifyIcon
            {
                Text = "Throne",
                Visible = true
            };

            // ★ PNG画像を読み込み、透過情報を保ったままアイコンへと錬成しますわ
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Necronomicon.png");
            if (System.IO.File.Exists(iconPath))
            {
                using (var bmp = new System.Drawing.Bitmap(iconPath))
                {
                    IntPtr hIcon = bmp.GetHicon();

                    // 錬成したアイコンをタスクトレイに適用します
                    _notifyIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);

                    // 適用が終わったら、元の型（IntPtr）は美しく破棄しますわ
                    DestroyIcon(hIcon);
                }
            }
            else
            {
                System.Windows.MessageBox.Show($"アイコン画像が見つかりませんわ！\n探求したパス: {iconPath}", "Throne.Hub 警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                // 万が一画像が見つからなかった場合のフォールバック（自身のexeアイコン）です
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            }

            var menu = new ContextMenuStrip();

            // --- 魔導書 (Grimoire) の階層 ---
            var grimoireItem = new ToolStripMenuItem("魔導書 (Grimoire)");
            grimoireItem.DropDownItems.Add("再読み込み", null, (s, args) => SendCommand("Grimoire", "Reload"));
            grimoireItem.DropDownItems.Add("終了", null, (s, args) => SendCommand("Grimoire", "Exit"));
            menu.Items.Add(grimoireItem);

            // --- 創世記 (Genesis) の階層 ---
            var genesisItem = new ToolStripMenuItem("創世記 (Genesis)");
            genesisItem.DropDownItems.Add("表示", null, (s, args) => StartApp("Kuro-DockGenesis.exe"));
            genesisItem.DropDownItems.Add("終了", null, (s, args) => SendCommand("Genesis", "Exit"));
            menu.Items.Add(genesisItem);

            // --- 要塞 (Fortress) の階層 ---
            var fortressItem = new ToolStripMenuItem("要塞 (Fortress)");
            fortressItem.DropDownItems.Add("表示", null, (s, args) => StartApp("Kuro-DockFortress.exe"));
            fortressItem.DropDownItems.Add("終了", null, (s, args) => SendCommand("Fortress", "Exit"));
            menu.Items.Add(fortressItem);

            menu.Items.Add(new ToolStripSeparator());

            // --- 帝国全域のシャットダウン ---
            var exitAllItem = new ToolStripMenuItem("帝国全域の活動停止 (全終了)");
            exitAllItem.Click += (s, args) =>
            {
                SendCommand("All", "Exit");
                _notifyIcon.Dispose();
                Shutdown();
            };
            menu.Items.Add(exitAllItem);

            _notifyIcon.ContextMenuStrip = menu;
        }

        // 魔導具を直接叩き起こす本物の魔法ですわ
        private void StartApp(string exeName)
        {
            try
            {
                // 玉座自身の存在位置を基点に、対象の魔導具のパスを割り出します
                string exePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName);

                if (System.IO.File.Exists(exePath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = exePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    System.Windows.MessageBox.Show($"{exeName} が見つかりませんわ。\n同じ領地（フォルダ）に存在するか確認しなさいな。\n探求パス: {exePath}",
                                                   "Throne.Hub - 召喚失敗", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"起動の儀式に失敗しましたわ: {ex.Message}",
                                               "Throne.Hub - エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 常駐している魔導具へ、パイプ通信で思念（命令）を送る本物の魔法ですわ
        private void SendCommand(string target, string command)
        {
            if (target == "All")
            {
                // 帝国全土への命令（シャットダウン等）ですわ
                _ = ThronePipeClient.SendCommandAsync("Grimoire", command);
                _ = ThronePipeClient.SendCommandAsync("Genesis", command);
                _ = ThronePipeClient.SendCommandAsync("Fortress", command);
            }
            else
            {
                // 個別の魔導具への命令です
                _ = ThronePipeClient.SendCommandAsync(target, command);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnExit(e);
        }
    }
}