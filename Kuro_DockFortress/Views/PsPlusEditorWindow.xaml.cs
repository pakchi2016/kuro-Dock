using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace Kuro_DockFortress.Views
{
    public partial class PsPlusEditorWindow : Window
    {
        private readonly TerminalControl _terminal;
        private string _targetFilePath;
        private bool _isDirty = false;

        public PsPlusEditorWindow(TerminalControl terminal, string targetFilePath = null)
        {
            InitializeComponent();
            _terminal = terminal;

            // パス指定がない場合は、現在のターミナル座標に「scratchpad.ps+」を仮作成しますわ
            _targetFilePath = string.IsNullOrWhiteSpace(targetFilePath)
                ? Path.Combine(_terminal.CurrentPath, "scratchpad.ps+")
                : targetFilePath;

            LoadFileContent();
        }

        private void LoadFileContent()
        {
            try
            {
                if (File.Exists(_targetFilePath))
                {
                    CodeEditor.Text = File.ReadAllText(_targetFilePath);
                }
                else
                {
                    CodeEditor.Text = "// PS+ C# Scripting IDE\n// 上部の[保存]を押すか、F5キーでいつでも要塞主砲へ流し込めますわ。\n\nPrint(\"Hello from PS+ IDE!\");\n";
                }
                UpdateTitleStatus(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ファイルの展開に失敗しましたわ: {ex.Message}", "PS+ IDE 警報", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveScript()
        {
            try
            {
                File.WriteAllText(_targetFilePath, CodeEditor.Text);
                UpdateTitleStatus(false);
                _terminal.AppendOutputFromScript($"\n[IDE] スクリプトを保存しましたわ: {_targetFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存魔術が失敗しましたわ: {ex.Message}", "PS+ IDE 警報", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteScript()
        {
            // プロの鉄則：実行前には必ず「エディタ上の最新の文字」をファイルへ自動保存させます
            if (_isDirty) SaveScript();

            _terminal.AppendOutputFromScript($"\nC# {_terminal.CurrentPath}> [IDE実行] {_targetFilePath}");

            // 本体のRoslynセッションへ、エディタの全文字列を直接叩き込みますわ！
            _terminal.ExecuteCSharpScriptAsync(CodeEditor.Text);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) => SaveScript();

        private void RunButton_Click(object sender, RoutedEventArgs e) => ExecuteScript();

        private void CodeEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!_isDirty) UpdateTitleStatus(true);
        }

        private void UpdateTitleStatus(bool isEdited)
        {
            _isDirty = isEdited;
            string dirtyMark = isEdited ? " *" : "";
            this.Title = $"PS+ 魔導書エディタ - {Path.GetFileName(_targetFilePath)}{dirtyMark}";
            StatusText.Text = $"{_targetFilePath}{dirtyMark}";
        }

        // キーボードショートカットの絶対結界（Ctrl+S と F5）
        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                ExecuteScript();
                e.Handled = true;
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveScript();
                e.Handled = true;
            }
        }
    }
}