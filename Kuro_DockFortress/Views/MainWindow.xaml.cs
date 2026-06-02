using Kuro_DockFortress.Models;
using Kuro_DockFortress.ViewModels;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Kuro_DockFortress.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel(); // 紐付けの儀式ですわ
        }

        // ★ 1. リストの項目をダブルクリックした時の処理ですわ
        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.ListView listView && listView.SelectedItem is FileItemModel file)
            {
                // クリックされたのが「フォルダ」だった場合のみ、パスを更新して中に入ります
                if (file.IsDirectory)
                {
                    // DataContext（タブのデータ）を取得して、現在のパスを上書きしますわ
                    if (listView.DataContext is TabItemModel tab)
                    {
                        tab.CurrentPath = file.Path;
                    }
                }
                else
                {
                    // ファイルだった場合は、とりあえずシステムの標準アプリで開くようにしてあげますわ
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.Path) { UseShellExecute = true });
                    }
                    catch { /* 実行できないファイルは優雅に無視します */ }
                }
            }
        }

        // ★ 2. 「↑」ボタンを押した時の処理ですわ
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel tab)
            {
                // 現在のパスの「親ディレクトリ」を取得します
                var parent = Directory.GetParent(tab.CurrentPath);
                if (parent != null)
                {
                    tab.CurrentPath = parent.FullName;
                }
                else
                {
                    tab.CurrentPath = "PC";
                }
            }
        }

        // ★ 3. パスバーで直接文字を打ち込み、Enterキーを押した時の処理ですわ
        private void PathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is System.Windows.Controls.TextBox tb && tb.Tag is TabItemModel tab)
                {
                    // 打ち込まれたパスが実在するか確認してから移動します
                    if (tb.Text == "PC" || Directory.Exists(tb.Text))
                    {
                        tab.CurrentPath = tb.Text;
                    }
                    else
                    {
                        // 存在しないデタラメなパスだった場合は、元の正しいパスに強制的に戻しますわ
                        tb.Text = tab.CurrentPath;
                    }
                }
            }
        }

        // ★ 4. 「＋」ボタンを押した時の処理ですわ
        private void AddTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel currentTab)
            {
                if (this.DataContext is MainViewModel vm)
                {
                    // 押されたボタンがどちらの陣営に属しているか判定し、現在のパスを引き継いだ新しいタブを生み出します
                    if (vm.LeftTabs.Contains(currentTab))
                    {
                        vm.LeftTabs.Add(new TabItemModel { CurrentPath = currentTab.CurrentPath });
                    }
                    else if (vm.RightTabs.Contains(currentTab))
                    {
                        vm.RightTabs.Add(new TabItemModel { CurrentPath = currentTab.CurrentPath });
                    }
                }
            }
        }

        // ★ 5. タブの「✕」ボタンを押した時の処理ですわ
        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TabItemModel targetTab)
            {
                if (this.DataContext is MainViewModel vm)
                {
                    // 最後の1つのタブを閉じられてしまうとUIが崩壊するため、1つより多い場合のみ削除を許可しますわ
                    if (vm.LeftTabs.Contains(targetTab) && vm.LeftTabs.Count > 1)
                    {
                        vm.LeftTabs.Remove(targetTab);
                    }
                    else if (vm.RightTabs.Contains(targetTab) && vm.RightTabs.Count > 1)
                    {
                        vm.RightTabs.Remove(targetTab);
                    }
                }
            }
        }
    }
}