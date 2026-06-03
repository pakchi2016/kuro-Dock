using System.Windows;

namespace Kuro_DockFortress.Views
{
    public partial class InputDialog : Window
    {
        public string InputText => InputTextBox.Text;

        public InputDialog(string message, string title, bool isMultiLine)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;

            // 複数行を許可するかどうかの美しい制御ですわ
            InputTextBox.AcceptsReturn = isMultiLine;

            if (!isMultiLine)
            {
                // 単一行の場合は、無駄な余白を削ぎ落としてスマートな高さにします
                this.Height = 150;
            }

            // 起動時にすぐ文字が打てるようフォーカスを合わせます
            InputTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}