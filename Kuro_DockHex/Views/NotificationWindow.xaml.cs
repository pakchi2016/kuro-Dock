using System;
using System.Windows;

namespace Kuro_DockHex.Views
{
    public partial class NotificationWindow : Window
    {
        private Models.MemoItemModel _memo;
        private Action _saveAction;
        private Action _completeAction;

        public NotificationWindow(Models.MemoItemModel memo, Action saveAction, Action completeAction)
        {
            InitializeComponent();
            _memo = memo;
            _saveAction = saveAction;
            _completeAction = completeAction;

            TextBlock_Text.Text = memo.Text; // 画面への表示はここでセットします
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Windowsの作業領域を取得し、画面の「右下」に寄り添うように配置しますわ
            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 15;
            this.Top = desktopWorkingArea.Bottom - this.Height - 15;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(); // 用が済めば消滅させます
        }

        // ★ 新設：運命を遅延させる黒魔術ですわ
        private void SnoozeButton_Click(object sender, RoutedEventArgs e)
        {
            // 期限を「今この瞬間から10分後」に書き換えます
            _memo.TargetDate = DateTime.Now.AddMinutes(10);

            // 再び監視対象に戻すため、通知済みフラグ（IsNotified）を折ります
            _memo.IsNotified = false;

            // 本体（MainWindow側）のセーブ処理を呼び出し、JSONにこの改変を刻み込みます
            _saveAction?.Invoke();

            this.Close(); // 10分後の再会を約束し、一度姿を消します
        }
        // ★ 新設：完了ボタンが押された時の儀式ですわ
        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            // 親玉（MainWindow）に「このタスクをCSVに刻んで消し去りなさい」と命じます
            _completeAction?.Invoke();

            this.Close(); // 任務を果たしたため、静かに消滅します
        }
    }
}