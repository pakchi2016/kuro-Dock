using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.CSharp.Scripting; // ★受肉したRoslynスクリプト魔術
using Microsoft.CodeAnalysis.Scripting;        // ★スクリプトセッション制御

namespace Kuro_DockFortress.Views
{
    public partial class TerminalControl : System.Windows.Controls.UserControl
    {
        // --- 従来の美しき遺産群 ---
        private Process _process;
        private StreamWriter _streamWriter;
        private List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private PowerShell _completionPs;
        private CommandCompletion _lastCompletion;
        private int _completionMatchIndex = -1;
        private bool _isTabCompleting = false;
        private string _textBeforeCompletion;
        private Dictionary<string, string> _macros = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _macroFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "macros.json");

        // ★ 新設：Roslyn C#スクリプトの「継続する意識（セッション）」とグローバルコンテキストですわ！
        private ScriptState<object> _csScriptState;
        private FortressScriptGlobals _scriptGlobals;
        private readonly string _globalLibraryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GlobalLibrary.csx");

        private string _currentPath = @"C:\";
        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                if (string.IsNullOrWhiteSpace(value) || value == "PC") return;

                _currentPath = value.TrimEnd('\\') + "\\";
                if (PromptText != null) PromptText.Text = $"PS {_currentPath}>";

                if (_completionPs != null)
                {
                    try { _completionPs.Runspace.SessionStateProxy.Path.SetLocation(_currentPath); }
                    catch { /* 握り潰しますわ */ }
                }
            }
        }

        public TerminalControl()
        {
            InitializeComponent();
            CurrentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            StartPowerShell();
            LoadMacros();

            var iss = System.Management.Automation.Runspaces.InitialSessionState.CreateDefault2();
            _completionPs = PowerShell.Create(iss);

            // ★ 魔導炉の事前予熱：起動の裏側で静かにRoslynコンパイラに火を入れ、コールドスタートを排除しますわ
            WarmupCSharpScriptEngine();
        }

        // ====================================================================
        // ★ 第3の道：Roslyn C#スクリプト魔導炉の初期化と「共通法典」の受肉
        // ====================================================================
        private async void WarmupCSharpScriptEngine()
        {
            try
            {
                // 王者の作法：スクリプト側での using を極限まで省く暗黙のインポート結界ですわ
                var options = ScriptOptions.Default
                    .WithImports("System", "System.IO", "System.Linq", "System.Collections.Generic", "System.Text", "System.Text.RegularExpressions")
                    .WithReferences(
                        typeof(object).Assembly,
                        typeof(Enumerable).Assembly,
                        typeof(File).Assembly,
                        typeof(Regex).Assembly,
                        typeof(FortressScriptGlobals).Assembly // 要塞自身のクラス群も参照させます
                    );

                _scriptGlobals = new FortressScriptGlobals(this);

                // 1. まず無の詠唱でコンパイラを励起（予熱完了）
                _csScriptState = await CSharpScript.RunAsync("", options, _scriptGlobals);

                // 2. 卿の「成長する魔導書（GlobalLibrary.csx）」があれば、セッションに永続結合しますわ！
                if (File.Exists(_globalLibraryPath))
                {
                    string globalCode = File.ReadAllText(_globalLibraryPath);
                    _csScriptState = await _csScriptState.ContinueWithAsync(globalCode);
                    AppendOutput("\n[Roslyn魔導炉] 共通ライブラリ 'GlobalLibrary.csx' を胎内に受肉させましたわ。");
                }
                else
                {
                    // 初回配備：卿が使い回すであろう便利関数の雛形を自動生成してあげますわ
                    string initialLib =
@"// Kuro-Dock Fortress - グローバル共通ライブラリ法典
// ここに記述したメソッドは、全C#スクリプトおよびワンライナーからいつでも名前だけで召喚できますわ。

string GetExt(string path) => Path.GetExtension(path);
string[] GetFiles(string pattern) => Directory.GetFiles(CurrentPath, pattern);
";
                    File.WriteAllText(_globalLibraryPath, initialLib);
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"\n[Roslyn起動警報] 魔導炉の励起に失敗しましたわ: {ex.Message}");
            }
        }

        // ====================================================================
        // ★ C#スクリプトおよび魔導書（.ps+ / .csx）の非同期実行ロジック
        // ====================================================================
        internal async void ExecuteCSharpScriptAsync(string code, string[] args = null)
        {
            if (_csScriptState == null)
            {
                AppendOutput("\n[警告] Roslyn魔導炉がまだ温まっておりませんわ！数秒お待ちになって。");
                return;
            }

            try
            {
                // 実行時引数が渡されていればグローバル変数へ注入しますわ
                if (args != null) _scriptGlobals.Args = args;

                // セッションを継続（ContinueWithAsync）させることで前回の変数やGlobalLibraryを完全維持します
                _csScriptState = await _csScriptState.ContinueWithAsync(code);

                // スクリプトが評価結果（戻り値）を持っていた場合、ターミナルへ美しく出力しますわ
                if (_csScriptState.ReturnValue != null)
                {
                    AppendOutput($"=> {_csScriptState.ReturnValue}");
                }
            }
            catch (Exception ex)
            {
                AppendOutput($"\n[C#実行例外] {ex.Message}");
            }
        }

        internal void AppendOutputFromScript(string text) => AppendOutput(text);

        private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // --- Tab補完（変更なし） ---
            if (e.Key == System.Windows.Input.Key.Tab)
            {
                e.Handled = true;
                if (!_isTabCompleting)
                {
                    string currentInput = InputBox.Text;
                    int caret = InputBox.CaretIndex;
                    _textBeforeCompletion = currentInput;
                    _lastCompletion = CommandCompletion.CompleteInput(currentInput, caret, null, _completionPs);

                    if (_lastCompletion != null && _lastCompletion.CompletionMatches.Count > 0)
                    {
                        _isTabCompleting = true;
                        _completionMatchIndex = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? _lastCompletion.CompletionMatches.Count - 1 : 0;
                        ApplyCompletionMatch();
                    }
                }
                else
                {
                    if (_lastCompletion != null && _lastCompletion.CompletionMatches.Count > 0)
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                        {
                            _completionMatchIndex--;
                            if (_completionMatchIndex < 0) _completionMatchIndex = _lastCompletion.CompletionMatches.Count - 1;
                        }
                        else
                        {
                            _completionMatchIndex++;
                            if (_completionMatchIndex >= _lastCompletion.CompletionMatches.Count) _completionMatchIndex = 0;
                        }
                        ApplyCompletionMatch();
                    }
                }
                return;
            }

            if (e.Key != System.Windows.Input.Key.LeftShift && e.Key != System.Windows.Input.Key.RightShift)
            {
                _isTabCompleting = false;
                _lastCompletion = null;
            }

            // ====================================================================
            // ★ 入力コマンドのハイブリッド・ルーティング（Enter押下時）
            // ====================================================================
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string rawCmd = InputBox.Text;
                if (string.IsNullOrWhiteSpace(rawCmd))
                {
                    AppendOutput($"\nPS {_currentPath}> ");
                    InputBox.Clear();
                    e.Handled = true;
                    return;
                }

                string trimmed = rawCmd.Trim();

                // ----------------------------------------------------------------
                // ★ 新ルートA：生のC#ワンライナー直接実行 (接頭辞: > )
                // 例: > Print("Hello " + CurrentPath);
                // ----------------------------------------------------------------
                if (trimmed.StartsWith(">"))
                {
                    string csCode = trimmed.Substring(1).Trim();
                    AppendOutput($"\nC# {_currentPath}> {csCode}");
                    _commandHistory.Add(rawCmd);
                    _historyIndex = _commandHistory.Count;
                    InputBox.Clear();
                    e.Handled = true;

                    ExecuteCSharpScriptAsync(csCode);
                    return;
                }

                // ----------------------------------------------------------------
                // ★ 新ルートB：拡張スクリプト (.ps+ / .csx) のファイル実行
                // 例: .\test.ps+ arg1 arg2 または .\build.csx
                // ----------------------------------------------------------------
                if (trimmed.StartsWith(".\\") && (trimmed.Contains(".ps+") || trimmed.Contains(".csx")))
                {
                    // クォートされた空白パスを死守しつつ引数を分解する黒魔術
                    var parts = Regex.Matches(trimmed, @"[\""].+?[\""]|[^ ]+")
                                     .Cast<Match>()
                                     .Select(m => m.Value.Trim('"'))
                                     .ToArray();

                    string scriptName = parts[0].Substring(2); // ".\test.ps+" -> "test.ps+"
                    string[] targetArgs = parts.Skip(1).ToArray();
                    string targetFilePath = Path.Combine(_currentPath, scriptName);

                    AppendOutput($"\nC# {_currentPath}> [スクリプト召喚] {trimmed}");

                    if (File.Exists(targetFilePath))
                    {
                        string scriptContent = File.ReadAllText(targetFilePath);
                        ExecuteCSharpScriptAsync(scriptContent, targetArgs);
                    }
                    else
                    {
                        AppendOutput($"\n[警告] 座標に魔導書が見つかりませんわ: {targetFilePath}");
                    }

                    _commandHistory.Add(rawCmd);
                    _historyIndex = _commandHistory.Count;
                    InputBox.Clear();
                    e.Handled = true;
                    return;
                }

                // --- 既存のルートC：マクロの保存・展開 ---
                if (trimmed.StartsWith("#") && trimmed.Contains("="))
                {
                    int eqIdx = trimmed.IndexOf('=');
                    string namePart = trimmed.Substring(0, eqIdx).Trim().Split(' ')[0];
                    string bodyPart = trimmed.Substring(eqIdx + 1).Trim();

                    if (!string.IsNullOrEmpty(namePart))
                    {
                        _macros[namePart] = bodyPart;
                        SaveMacros();
                        AppendOutput($"\nPS {_currentPath}> {rawCmd}\n[マクロ記憶完了] '{namePart}' を永続化させましたわ。");
                        _commandHistory.Add(rawCmd);
                        _historyIndex = _commandHistory.Count;
                        InputBox.Clear();
                        e.Handled = true;
                        return;
                    }
                }
                else if (trimmed.StartsWith("#"))
                {
                    var parts = Regex.Matches(trimmed, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value).ToArray();
                    string macroName = parts[0];

                    if (_macros.TryGetValue(macroName, out string macroBody))
                    {
                        string expandedCmd = macroBody;
                        for (int i = 1; i < parts.Length; i++)
                        {
                            expandedCmd = expandedCmd.Replace($"#{i}", parts[i].Trim('"'));
                        }
                        AppendOutput($"\nPS {_currentPath}> {rawCmd}\n[マクロ展開] => {expandedCmd}");
                        _streamWriter.WriteLine(expandedCmd);
                        _commandHistory.Add(rawCmd);
                        _historyIndex = _commandHistory.Count;
                        InputBox.Clear();
                        e.Handled = true;
                        return;
                    }
                }

                // ----------------------------------------------------------------
                // ★ 新ルートD：統合開発環境（IDE）の召喚命令
                // 例: edit または edit test.ps+
                // ----------------------------------------------------------------
                if (trimmed.StartsWith("edit", StringComparison.OrdinalIgnoreCase))
                {
                    string[] editParts = trimmed.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    string targetFile = editParts.Length > 1 ? Path.Combine(_currentPath, editParts[1]) : null;

                    var ide = new PsPlusEditorWindow(this, targetFile) { Owner = Window.GetWindow(this) };
                    ide.Show();

                    _commandHistory.Add(rawCmd);
                    _historyIndex = _commandHistory.Count;
                    InputBox.Clear();
                    e.Handled = true;
                    return;
                }

                // --- 通常のPowerShellルート ---
                _commandHistory.Add(rawCmd);
                _historyIndex = _commandHistory.Count;
                AppendOutput($"\nPS {_currentPath}> {rawCmd}");
                _streamWriter.WriteLine(rawCmd);
                InputBox.Clear();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Up)
            {
                if (_commandHistory.Count > 0 && _historyIndex > 0)
                {
                    _historyIndex--;
                    InputBox.Text = _commandHistory[_historyIndex];
                    InputBox.CaretIndex = InputBox.Text.Length;
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Down)
            {
                if (_commandHistory.Count > 0 && _historyIndex < _commandHistory.Count)
                {
                    _historyIndex++;
                    if (_historyIndex == _commandHistory.Count) InputBox.Clear();
                    else
                    {
                        InputBox.Text = _commandHistory[_historyIndex];
                        InputBox.CaretIndex = InputBox.Text.Length;
                    }
                }
                e.Handled = true;
            }
        }

        private void LoadMacros()
        {
            try
            {
                if (File.Exists(_macroFilePath))
                {
                    string json = File.ReadAllText(_macroFilePath);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (loaded != null) { _macros = new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase); return; }
                }
            }
            catch { }
            _macros = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "#renameFiles", "Get-Childitem -Path \"#1\" | foreach {rename-item -Path $_.FullName -newname \"#2\"}" } };
            SaveMacros();
        }

        private void SaveMacros()
        {
            try { File.WriteAllText(_macroFilePath, JsonSerializer.Serialize(_macros, new JsonSerializerOptions { WriteIndented = true })); }
            catch { }
        }

        private void ApplyCompletionMatch()
        {
            var match = _lastCompletion.CompletionMatches[_completionMatchIndex];
            int repIdx = _lastCompletion.ReplacementIndex;
            int repLen = _lastCompletion.ReplacementLength;
            InputBox.Text = _textBeforeCompletion.Substring(0, repIdx) + match.CompletionText + _textBeforeCompletion.Substring(repIdx + repLen);
            InputBox.CaretIndex = repIdx + match.CompletionText.Length;
        }

        public void ExecuteCommand(string cmd) => _streamWriter?.WriteLine(cmd);

        private void StartPowerShell()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoExit -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            _process = new Process { StartInfo = startInfo };
            _process.OutputDataReceived += (s, e) => AppendOutput(e.Data);
            _process.ErrorDataReceived += (s, e) => AppendOutput(e.Data);
            _process.Start();
            _streamWriter = _process.StandardInput;
            _streamWriter.AutoFlush = true;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        private void AppendOutput(string text)
        {
            if (text == null) return;
            Dispatcher.InvokeAsync(() => { OutputBox.AppendText(text + Environment.NewLine); OutputBox.ScrollToEnd(); });
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            try { if (_process != null && !_process.HasExited) _process.Kill(); }
            catch { }
            finally { _completionPs?.Dispose(); }
        }
    }

    // ====================================================================
    // ★ スクリプト側から無宣言で触れる「要塞のグローバルAPI」定義ですわ！
    // ====================================================================
    public class FortressScriptGlobals
    {
        private readonly TerminalControl _terminal;
        public FortressScriptGlobals(TerminalControl terminal) { _terminal = terminal; }

        // 実行時引数 ( .\test.ps+ arg1 arg2 の arg1〜 が入りますわ )
        public string[] Args { get; set; } = new string[0];

        // 現在のファイラーの絶対パス
        public string CurrentPath => _terminal.CurrentPath;

        // ターミナル画面への超速出力魔法
        public void Print(object obj) => _terminal.AppendOutputFromScript(obj?.ToString() ?? "null");
        public void Echo(object obj) => Print(obj);

        // 痒いところに手が届く超便利IOラッパー
        public string ReadFile(string path) => File.ReadAllText(Path.IsPathRooted(path) ? path : Path.Combine(CurrentPath, path));
        public void WriteFile(string path, string content) => File.WriteAllText(Path.IsPathRooted(path) ? path : Path.Combine(CurrentPath, path), content);
    }
}