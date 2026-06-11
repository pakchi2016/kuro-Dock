using System.Windows;

namespace Kuro_DockGenesis.Views
{
    public partial class RenameWindow : Window
    {
        public string NewAlias { get; private set; }

        // 現在の名前を最初からテキストボックスに入れておくという、心遣いですわ
        public RenameWindow(string currentName)
        {
            InitializeComponent();
            NameTextBox.Text = currentName;
            NameTextBox.SelectAll();
            NameTextBox.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // 空白の場合はエイリアス解除とみなしますわ
            NewAlias = NameTextBox.Text.Trim();
            this.DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}