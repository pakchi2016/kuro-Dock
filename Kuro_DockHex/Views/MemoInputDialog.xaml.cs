using System;
using System.Windows;

namespace Kuro_DockHex.Views
{
    public partial class MemoInputDialog : Window
    {
        public string MemoText => TextBox_Text.Text.Trim();
        public DateTime? TargetDate { get; private set; }

        public MemoInputDialog()
        {
            InitializeComponent();
            TextBox_Text.Focus(); // 開いた瞬間にすぐ入力できるようにします

            // ★ 新設：現在の日時を取得し、初期値として入力欄にセットしますわ
            DateTime now = DateTime.Now;

            // カレンダーには「日付のみ」をセットします
            DatePicker_Date.SelectedDate = now.Date;

            // 時刻欄には「HH:mm（24時間表記の時:分）」の形式で文字列としてセットします
            TextBox_Time.Text = now.ToString("HH:mm");
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MemoText))
            {
                System.Windows.MessageBox.Show("メモの内容が空ですわ。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // DatePickerのテキスト（手入力またはカレンダー選択）と、時刻のテキストを取得します
            string dateInput = DatePicker_Date.Text.Trim();
            string timeInput = TextBox_Time.Text.Trim();

            if (!string.IsNullOrEmpty(dateInput) || !string.IsNullOrEmpty(timeInput))
            {
                // 日付と時刻の文字列を結合しますわ（例: "2026/06/06 15:00"）
                string combinedDateTime = dateInput;
                if (!string.IsNullOrEmpty(timeInput))
                {
                    // 日付がある場合はスペースを挟んで時刻を繋ぎます
                    if (!string.IsNullOrEmpty(dateInput)) combinedDateTime += " " + timeInput;
                    else combinedDateTime = timeInput;
                }

                // 結合した文字列が正しい日時か検証します
                if (DateTime.TryParse(combinedDateTime, out DateTime parsedDate))
                {
                    TargetDate = parsedDate;
                }
                else
                {
                    System.Windows.MessageBox.Show("日時の形式が正しくありませんわ。\n例: 2026/06/06 15:00", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            this.DialogResult = true; // 成功の証を返します
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}