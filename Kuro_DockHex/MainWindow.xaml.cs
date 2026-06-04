using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Kuro_DockHex.Models;
using Kuro_DockHex.ViewModels;

namespace Kuro_DockHex
{
    public partial class MainWindow : Window
    {
        // ★ 待機位置と完全表示位置を記憶するための変数ですわ
        private double _targetTop;
        private double _hiddenTop;

        private System.Windows.Forms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainViewModel();
            SetupNotifyIcon();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = SystemParameters.WorkArea;

            // X座標（右上の定位置）は固定します
            this.Left = desktopWorkingArea.Right - this.Width - 100;

            // 完全表示位置（少し隙間を空けて美しく配置）
            _targetTop = desktopWorkingArea.Top;

            // ★ 待機位置の計算です。六角形の下の先端「50ピクセル」だけが画面に残るように隠しますわ
            _hiddenTop = desktopWorkingArea.Top - this.Height + 1;

            // 起動時は「待機状態」からスタートします
            this.Top = _hiddenTop;
        }

        // ★ 先端をクリックした瞬間、完全な姿を現します
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SlideWindow(_targetTop);
        }

        // ★ マウスカーソルが六角形から外に出た瞬間、再び空へ隠れます
        private async void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!this.IsMouseOver)
            {
                SlideWindow(_hiddenTop);
            }
        }

        // ★ アニメーションを司る魔法のメソッドですわ
        private void SlideWindow(double toValue)
        {
            DoubleAnimation slideAnimation = new DoubleAnimation
            {
                To = toValue,
                Duration = new Duration(TimeSpan.FromMilliseconds(300)), // 0.3秒の機敏な動きです
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(Window.TopProperty, slideAnimation);
        }

        // ★ 1. ダイアログを呼び出し、ViewModelへデータを追加する共通メソッドですわ
        private void ShowAddMemoDialog()
        {
            var dialog = new Views.MemoInputDialog();
            // ダイアログは最前面に出さないと魔法陣の下に隠れる可能性があるため、Topmostを指定します
            dialog.Topmost = true;

            if (dialog.ShowDialog() == true)
            {
                if (this.DataContext is MainViewModel vm)
                {
                    vm.Memos.Add(new MemoItemModel
                    {
                        Text = dialog.MemoText,
                        TargetDate = dialog.TargetDate
                    });
                    Helpers.MemoManager.Save(vm.Memos);
                }
            }
        }

        // ★ 2. 魔法陣の上で右クリックした時の処理
        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowAddMemoDialog();
        }

        // ★ 3. SetupNotifyIcon の中に、「メモ追加」のメニューを増設します
        private void SetupNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();

            // 実行ファイル自身のアイコンを抽出してタスクトレイに表示させますわ
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _notifyIcon.Text = "Kuro-DockHex";
            _notifyIcon.Visible = true;

            // 右クリックメニューの構築です
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            // 新設：タスクトレイからの追加コマンド
            var addItem = new System.Windows.Forms.ToolStripMenuItem("メモの追加");
            addItem.Click += (s, e) => ShowAddMemoDialog();

            // ★ 安全に魔法陣を消去する「終了」コマンドです
            var exitItem = new System.Windows.Forms.ToolStripMenuItem("終了");
            exitItem.Click += (s, e) =>
            {
                // アイコンを片付けてからプロセスを完全に沈めます
                _notifyIcon.Dispose();
                System.Windows.Application.Current.Shutdown();
            };

            contextMenu.Items.Add(addItem);
            contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator()); // 美しい区切り線ですわ
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }
    }
}