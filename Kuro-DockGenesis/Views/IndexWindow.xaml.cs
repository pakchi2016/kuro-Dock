using System.Windows;

namespace Kuro_DockGenesis.Views
{
    public partial class IndexWindow : Window
    {
        // 親ウィンドウがこの値を受け取りますわ
        public string IndexName { get; private set; }

        public IndexWindow()
        {
            InitializeComponent();
            // 起動時に自動でテキストボックスにフォーカスを当てます
            NameTextBox.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                IndexName = NameTextBox.Text.Trim();
                this.DialogResult = true; // OKで閉じたという美しい合図ですわ
            }
            else
            {
                System.Windows.MessageBox.Show("名前を入力しなさいな。", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}