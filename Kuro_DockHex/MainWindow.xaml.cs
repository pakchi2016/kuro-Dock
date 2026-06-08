using Kuro_DockHex.Models;
using Kuro_DockHex.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Kuro_DockHex
{
    public partial class MainWindow : Window
    {
        // ★ 待機位置と完全表示位置を記憶するための変数ですわ
        private double _targetTop;
        private double _hiddenTop;
        private DispatcherTimer _alarmTimer;

        private System.Windows.Forms.NotifyIcon _notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.MainViewModel();
            SetupNotifyIcon();
            SetupAlarmTimer();
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

        // ★ 新設：タスクを完了（削除）する際の儀式です
        private void DeleteMemo_Click(object sender, RoutedEventArgs e)
        {
            // 1. クリックされたメニュー項目(MenuItem)を捕捉します
            if (sender is System.Windows.Controls.MenuItem menuItem)
            {
                // 2. そのメニューを統括している親(ContextMenu)を捕捉します
                if (menuItem.Parent is System.Windows.Controls.ContextMenu contextMenu)
                {
                    // 3. そのメニューが「どこから開かれたか(PlacementTarget = Border)」を捕捉し、
                    //    そこからようやく、対象のルーン(MemoItemModel)を引きずり出しますわ！
                    if (contextMenu.PlacementTarget is FrameworkElement placementTarget &&
                        placementTarget.DataContext is Models.MemoItemModel memo)
                    {
                        if (this.DataContext is ViewModels.MainViewModel vm)
                        {
                            // 1. まず、消え去る前に歴史（CSV）へと刻み込みます
                            Helpers.MemoManager.ArchiveToCsv(memo);

                            // 2. 魔法陣（UIとViewModel）から対象のルーンを消去します
                            vm.Memos.Remove(memo);

                            // 3. 現在の魔法陣の状態を直ちにJSONへと上書き保存しますわ
                            Helpers.MemoManager.Save(vm.Memos);
                        }
                    }
                }
            }
        }

        // ★ 新設：タスク上での右クリックが、Grid（追加ダイアログ）へ伝播するのを防ぎますわ
        private void MemoBorder_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            // これでイベントの「バブリング（浮上）」が止まり、タスク専用のメニューだけが平和に開かれます
            e.Handled = true;

            if (sender is Border border && border.ContextMenu != null)
            {
                border.ContextMenu.PlacementTarget = border;
                border.ContextMenu.IsOpen = true; // メニューを開けという絶対命令です
            }
        }
        private void SetupAlarmTimer()
        {
            _alarmTimer = new DispatcherTimer();
            _alarmTimer.Interval = TimeSpan.FromSeconds(1); // 1秒ごとに実行
            _alarmTimer.Tick += AlarmTimer_Tick;
            _alarmTimer.Start();
            AlarmTimer_Tick(null, EventArgs.Empty);
        }

        // ★ 毎秒実行される監視の儀式ですわ
        private void AlarmTimer_Tick(object? sender, EventArgs e)
        {
            if (this.DataContext is ViewModels.MainViewModel vm)
            {
                DateTime now = DateTime.Now;
                bool isStateChanged = false;

                foreach (var memo in vm.Memos)
                {
                    // 期限が設定されており、現在時刻がそれを過ぎており、かつ「まだ通知していない」場合を狙撃します
                    if (memo.TargetDate.HasValue && now >= memo.TargetDate.Value && !memo.IsNotified)
                    {
                        memo.UpdateUrgency();
                        memo.IsNotified = true; // 二度鳴り防止の刻印
                        isStateChanged = true;
                        var targetMemo = memo;

                        // 通知ウィンドウをこの世界に具現化（召喚）しますわ！
                        var notifyWindow = new Views.NotificationWindow(
                            targetMemo,
                            () => Helpers.MemoManager.Save(vm.Memos),
                            () => {
                                // 1. CSVファイル（歴史書）へ追記
                                Helpers.MemoManager.ArchiveToCsv(targetMemo);
                                // 2. 魔法陣の一覧から削除
                                vm.Memos.Remove(targetMemo);
                                // 3. 最新の状態をJSONへ上書き保存
                                Helpers.MemoManager.Save(vm.Memos);
                            }
                        );
                        notifyWindow.Show();
                    }
                }

                // 1つでも通知済みに変化したなら、その状態を直ちにJSONへ永続化します
                if (isStateChanged)
                {
                    Helpers.MemoManager.Save(vm.Memos);
                }
            }
        }
        private void MemoBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // クリック2回（ダブルクリック）を厳密に検知します
            {
                e.Handled = true; // 親（Grid）への伝播を遮断し、不要な暴発を防ぎます

                if (sender is FrameworkElement fe && fe.DataContext is Models.MemoItemModel memo)
                {
                    // 先ほど作った「記憶を引き継ぐコンストラクタ」でダイアログを召喚します
                    var dialog = new Views.MemoInputDialog(memo.Text, memo.TargetDate);
                    dialog.Topmost = true;

                    if (dialog.ShowDialog() == true)
                    {
                        // 変更内容をルーンに上書きします
                        memo.Text = dialog.MemoText;
                        memo.TargetDate = dialog.TargetDate;

                        // 時間や内容が変わったため、過去の通知フラグを白紙に戻します
                        memo.IsNotified = false;

                        // JSON（歴史）へ新たな姿を上書き保存します
                        if (this.DataContext is ViewModels.MainViewModel vm)
                        {
                            Helpers.MemoManager.Save(vm.Memos);
                        }
                    }
                }
            }
        }

    }
}