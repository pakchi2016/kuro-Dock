using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kuro_DockHex.Models
{
    public class MemoItemModel : INotifyPropertyChanged
    {
        // データを一意に識別するための絶対の刻印ですわ
        public string Id { get; set; } = Guid.NewGuid().ToString();

        private string _text = "";
        public string Text
        {
            get => _text;
            set { _text = value; OnPropertyChanged(); }
        }

        private DateTime? _targetDate;
        public DateTime? TargetDate
        {
            get => _targetDate;
            set
            {
                _targetDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UrgencyColor)); // 期限が変われば色も変わるため、色の再計算も命じます
            }
        }

        // ★ 新設：すでに通知を送ったかどうかの絶対記憶ですわ
        public bool IsNotified { get; set; } = false;

        // ★ 新設：現在時刻と期限から、纏うべきオーラ（色）を決定して返しますわ
        public string UrgencyColor
        {
            get
            {
                if (!TargetDate.HasValue) return "#8E44AD"; // 期限なしは通常の紫

                var diff = TargetDate.Value - DateTime.Now;

                if (diff.TotalSeconds < 0) return "#FF0033"; // 最優先警告（ビビッドレッド）

                if (diff.TotalHours < 1) return "#E74C3C";   // 1時間を切ったら危険の「赤色」
                if (diff.TotalHours < 24) return "#F1C40F";  // 24時間を切ったら警告の「黄色」

                return "#8E44AD"; // それ以上先なら通常の紫
            }
        }
        // ★ 新設：時の監視者から呼ばれ、UIへ「色を再計算して塗り直せ」と命令を発します
        public void UpdateUrgency()
        {
            OnPropertyChanged(nameof(UrgencyColor));
        }

        // --- 以下、画面へ変更を通知するための定型呪文ですわ ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}