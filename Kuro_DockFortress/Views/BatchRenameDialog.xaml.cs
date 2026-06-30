using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kuro_DockFortress.Views
{
    public partial class BatchRenameDialog : Window
    {
        private List<RenameItem> _masterList = new List<RenameItem>();
        private int _sortState = 0; // 0: 初期順, 1: 昇順, 2: 降順

        // ★ MVVMモデル：ユーザーが手動で右列TextBoxを1文字弄った瞬間を検知しますわ
        public class RenameItem : INotifyPropertyChanged
        {
            public string FullPath { get; set; }
            public string OriginalName { get; set; }
            public int InitialIndex { get; set; }

            private string _newName;
            public string NewName
            {
                get => _newName;
                set
                {
                    if (_newName != value)
                    {
                        _newName = value;
                        OnPropertyChanged();
                        OnItemChanged?.Invoke(); // ボトムアップ検証へ即時伝播
                    }
                }
            }

            private string _status = "対象外";
            public string Status
            {
                get => _status;
                set { _status = value; OnPropertyChanged(); }
            }

            private System.Drawing.Brush _statusColor = System.Drawing.Brushes.Gray;
            public System.Drawing.Brush StatusColor
            {
                get => _statusColor;
                set { _statusColor = value; OnPropertyChanged(); }
            }

            public Action OnItemChanged { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public BatchRenameDialog(List<string> targetFullPaths)
        {
            InitializeComponent();

            for (int i = 0; i < targetFullPaths.Count; i++)
            {
                string path = targetFullPaths[i];
                var item = new RenameItem
                {
                    FullPath = path,
                    OriginalName = Path.GetFileName(path),
                    InitialIndex = i,
                    OnItemChanged = () => ValidateAllItems()
                };
                item.NewName = item.OriginalName;
                _masterList.Add(item);
            }

            TargetInfoText.Text = $"対象トポロジー: 計 {_masterList.Count} 件のファイル";
            PreviewListView.ItemsSource = _masterList;
            ValidateAllItems();
        }

        // ====================================================================
        // 仕様4＆5：ジェネレータによる「拡張子聖域保護・一括展開」
        // ====================================================================
        private void GenerateBtn_Click(object sender, RoutedEventArgs e)
        {
            string pattern = SearchBox.Text;
            string netRep = TranslateSakuraToNet(ReplaceBox.Text);

            bool isRegexValid = false;
            Regex re = null;

            try { if (!string.IsNullOrEmpty(pattern)) { re = new Regex(pattern); isRegexValid = true; } }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"正規表現の構文が破綻していますわ:\n{ex.Message}", "詠唱エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in _masterList)
            {
                if (isRegexValid && re.IsMatch(item.OriginalName))
                {
                    // ★ 聖域分離：幹(body)にだけ置換を流し込み、拡張子(ext)を後から安全結合します
                    string ext = Path.GetExtension(item.OriginalName);
                    string body = Path.GetFileNameWithoutExtension(item.OriginalName);

                    try
                    {
                        string replacedBody = re.Replace(body, netRep);
                        item.NewName = replacedBody + ext;
                    }
                    catch { /* 個別の破綻は下のValidateが捕獲します */ }
                }
                else
                {
                    item.NewName = item.OriginalName;
                }
            }

            ValidateAllItems();
        }

        // 仕様6：サクラエディタ筋肉記憶トランスパイラ
        private string TranslateSakuraToNet(string sakuraRep)
        {
            if (string.IsNullOrEmpty(sakuraRep)) return "";
            string net = Regex.Replace(sakuraRep, @"\\([1-9])", "$$$1"); // \1 -> $1
            net = Regex.Replace(net, @"(?<![\$\\])&", "$&&");          // 単体 & -> $&
            return net;
        }

        // ====================================================================
        // 仕様8：ボトムアップ・フェールセーフ（1件の異常で全主砲を物理ロック）
        // ====================================================================
        private void ValidateAllItems()
        {
            if (_masterList == null || _masterList.Count == 0) return;

            var duplicates = _masterList.Select(x => x.NewName)
                                        .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                                        .Where(g => g.Count() > 1)
                                        .Select(g => g.Key)
                                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

            int errorCount = 0;
            int changeCount = 0;

            foreach (var item in _masterList)
            {
                if (item.NewName == item.OriginalName)
                {
                    item.Status = "対象外"; item.StatusColor = System.Drawing.Brushes.Gray;
                }
                else if (item.NewName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    item.Status = "⚠️禁則文字"; item.StatusColor = System.Drawing.Brushes.Red; errorCount++;
                }
                else if (duplicates.Contains(item.NewName))
                {
                    item.Status = "⚠️名前重複"; item.StatusColor = System.Drawing.Brushes.Red; errorCount++;
                }
                else
                {
                    item.Status = "変更あり"; item.StatusColor = System.Drawing.Brushes.DeepSkyBlue; changeCount++;
                }
            }

            if (errorCount > 0)
            {
                ErrorSummaryText.Text = $"※致命的衝突が {errorCount} 件あります！執行できませんわ。";
                ExecBtn.IsEnabled = false;
            }
            else
            {
                ErrorSummaryText.Text = changeCount > 0 ? $"変更対象: {changeCount} 件" : "変更がありませんわ";
                ExecBtn.IsEnabled = changeCount > 0;
            }
        }

        // ====================================================================
        // 仕様7：手入力データごと1対1でペア追随するフィジカルソート
        // ====================================================================
        private void OriginalHeader_Click(object sender, RoutedEventArgs e)
        {
            _sortState = (_sortState + 1) % 3;

            if (_sortState == 1)
            {
                _masterList = _masterList.OrderBy(x => x.OriginalName, StringComparer.OrdinalIgnoreCase).ToList();
                ((GridViewColumnHeader)sender).Content = "📄 元のファイル名 ▲";
            }
            else if (_sortState == 2)
            {
                _masterList = _masterList.OrderByDescending(x => x.OriginalName, StringComparer.OrdinalIgnoreCase).ToList();
                ((GridViewColumnHeader)sender).Content = "📄 元のファイル名 ▼";
            }
            else
            {
                _masterList = _masterList.OrderBy(x => x.InitialIndex).ToList();
                ((GridViewColumnHeader)sender).Content = "📄 元のファイル名 (クリックでソート)";
            }

            PreviewListView.ItemsSource = _masterList;
            ValidateAllItems();
        }

        // ====================================================================
        // 玉突き事故を100%回避する2段階一括退避ムーブ
        // ====================================================================
        private void ExecBtn_Click(object sender, RoutedEventArgs e)
        {
            var targets = _masterList.Where(x => x.Status == "変更あり").ToList();
            if (targets.Count == 0) return;

            var movePlans = new List<(string Src, string Tmp, string Dest)>();
            string dir = Path.GetDirectoryName(targets[0].FullPath);

            try
            {
                foreach (var item in targets)
                {
                    string tmpPath = Path.Combine(dir, $"__fortress_tmp_{Guid.NewGuid():N}__.tmp");
                    string destPath = Path.Combine(dir, item.NewName);
                    movePlans.Add((item.FullPath, tmpPath, destPath));
                }

                foreach (var p in movePlans) File.Move(p.Src, p.Tmp);
                foreach (var p in movePlans) File.Move(p.Tmp, p.Dest);

                System.Windows.MessageBox.Show($"{targets.Count} 件のリネーム魔術を執行いたしましたわ！", "任務完了", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"執行中に物理エラーが発生しましたわ:\n{ex.Message}", "致命的例外", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}