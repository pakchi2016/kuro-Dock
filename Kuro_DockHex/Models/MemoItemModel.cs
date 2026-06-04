using System;

namespace Kuro_DockHex.Models
{
    public class MemoItemModel
    {
        // データを一意に識別するための絶対の刻印ですわ
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Text { get; set; }

        // 期限がないタスクも考慮して Nullable (?) にしておきます
        public DateTime? TargetDate { get; set; }
    }
}