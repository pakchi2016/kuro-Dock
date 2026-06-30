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
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Kuro_DockFortress.Views
{
    public partial class TerminalControl : System.Windows.Controls.UserControl
    {
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

        private ScriptState<object> _csScriptState;
        private FortressScriptGlobals _scriptGlobals;
        private readonly string _globalLibraryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GlobalLibrary.csx");

        // ★ 新設：ターミナル側での手動cdをMainWindow（ファイラー側）に逆伝播させるための通信デリゲートですわ
        public Action<string> OnPathChangedFromTerminal { get; set; }

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

            var iss = System.Management.Automation.Runspaces.InitialSessionState.CreateDefault2();
            _completionPs = PowerShell.Create(iss);
            LoadMacros();
            WarmupCSharpScriptEngine();
        }

        // ★ 修正：PowerShellのprompt関数を完全に掌握し、現在地を常に裏で盗み聞きしますわ！
        private void StartPowerShell()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                // prompt関数をハックし、コマンド実行完了のたびに特殊なトークンで実パスを報告させます
                Arguments = "-NoExit -Command \"[Console]::OutputEncoding = [System.Text.Encoding]::UTF8; function Out-Default { $input | Microsoft.PowerShell.Core\\Out-Default; [Console]::WriteLine('__FORTRESS_PWD__:' + $PWD.ProviderPath + '__') }\"",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            _process = new Process { StartInfo = startInfo };

            // 標準出力の監視結界
            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null && e.Data.StartsWith("__FORTRESS_PWD__:") && e.Data.EndsWith("__"))
                {
                    // 隠しトークンを検知！画面（OutputBox）には出力せず、C#側のカレントパスのみを優雅に更新しますわ
                    string rawPath = e.Data.Substring(17, e.Data.Length - 19);
                    Dispatcher.InvokeAsync(() =>
                    {
                        UpdateCurrentPathFromProcess(rawPath);
                    });
                    return;
                }
                AppendOutput(e.Data);
            };

            _process.ErrorDataReceived += (s, e) => AppendOutput(e.Data);

            _process.Start();
            _streamWriter = _process.StandardInput;
            _streamWriter.AutoFlush = true;

            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }

        // ★ 新設：裏のプロセスが cd 移動した結果を、要塞の全脳（UI・インテリセンス・MainWindow）に無血同期させます
        private void UpdateCurrentPathFromProcess(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;

            _currentPath = path.TrimEnd('\\') + "\\";
            if (PromptText != null) PromptText.Text = $"PS {_currentPath}>";

            // Roslynタブ補完エンジンのカレントも手動移動先に追随させますわ
            if (_completionPs != null)
            {
                try { _completionPs.Runspace.SessionStateProxy.Path.SetLocation(_currentPath); }
                catch { }
            }

            // MainWindow（ファイラー側）の同期防壁へ「手動でここへ移動した」事実を電報します
            OnPathChangedFromTerminal?.Invoke(_currentPath);
        }

        private async void WarmupCSharpScriptEngine()
        {
            try
            {
                var options = ScriptOptions.Default
                    .WithImports("System", "System.IO", "System.Linq", "System.Collections.Generic", "System.Text", "System.Text.RegularExpressions")
                    .WithReferences(typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(File).Assembly, typeof(Regex).Assembly, typeof(FortressScriptGlobals).Assembly);

                _scriptGlobals = new FortressScriptGlobals(this);
                _csScriptState = await CSharpScript.RunAsync("", options, _scriptGlobals);

                if (File.Exists(_globalLibraryPath))
                {
                    string globalCode = File.ReadAllText(_globalLibraryPath);
                    _csScriptState = await _csScriptState.ContinueWithAsync(globalCode);
                    AppendOutput("\n[Roslyn魔導炉] 共通ライブラリ 'GlobalLibrary.csx' を胎内に受肉させましたわ。");
                }
                else
                {
                    string initialLib = "// Kuro-Dock Fortress - グローバル共通ライブラリ法典\nstring GetExt(string path) => Path.GetExtension(path);\nstring[] GetFiles(string pattern) => Directory.GetFiles(CurrentPath, pattern);\n";
                    File.WriteAllText(_globalLibraryPath, initialLib);
                }
            }
            catch (Exception ex) { AppendOutput($"\n[Roslyn起動警報] 魔導炉の励起に失敗しましたわ: {ex.Message}"); }
        }

        internal async void ExecuteCSharpScriptAsync(string code, string[] args = null)
        {
            if (_csScriptState == null) { AppendOutput("\n[警告] Roslyn魔導炉がまだ温まっておりませんわ！"); return; }
            try
            {
                if (args != null) _scriptGlobals.Args = args;
                _csScriptState = await _csScriptState.ContinueWithAsync(code);
                if (_csScriptState.ReturnValue != null) AppendOutput($"=> {_csScriptState.ReturnValue}");
            }
            catch (Exception ex) { AppendOutput($"\n[C#実行例外] {ex.Message}"); }
        }

        internal void AppendOutputFromScript(string text) => AppendOutput(text);

        private void InputBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
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

            if (e.Key == System.Windows.Input.Key.Enter)
            {
                string rawCmd = InputBox.Text;
                if (string.IsNullOrWhiteSpace(rawCmd))
                {
                    AppendOutput($"PS {_currentPath}> ");
                    InputBox.Clear();
                    e.Handled = true;
                    return;
                }

                string trimmed = rawCmd.Trim();

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

                if (trimmed.StartsWith(".\\") && (trimmed.Contains(".ps+") || trimmed.Contains(".csx")))
                {
                    var parts = Regex.Matches(trimmed, @"[\""].+?[\""]|[^ ]+").Cast<Match>().Select(m => m.Value.Trim('"')).ToArray();
                    string scriptName = parts[0].Substring(2);
                    string[] targetArgs = parts.Skip(1).ToArray();
                    string targetFilePath = Path.Combine(_currentPath, scriptName);

                    AppendOutput($"\nC# {_currentPath}> [スクリプト召喚] {trimmed}");
                    if (File.Exists(targetFilePath)) ExecuteCSharpScriptAsync(File.ReadAllText(targetFilePath), targetArgs);
                    else AppendOutput($"\n[警告] 魔導書が見つかりませんわ: {targetFilePath}");

                    _commandHistory.Add(rawCmd);
                    _historyIndex = _commandHistory.Count;
                    InputBox.Clear();
                    e.Handled = true;
                    return;
                }

                if (trimmed.StartsWith("#") && trimmed.Contains("="))
                {
                    int eqIdx = trimmed.IndexOf('=');
                    string namePart = trimmed.Substring(0, eqIdx).Trim().Split(' ')[0];
                    string bodyPart = trimmed.Substring(eqIdx + 1).Trim();

                    if (!string.IsNullOrEmpty(namePart))
                    {
                        _macros[namePart] = bodyPart;
                        SaveMacros();
                        AppendOutput($"PS {_currentPath}> {rawCmd}\n[マクロ記憶完了] '{namePart}' を永続化させましたわ。");
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
                        for (int i = 1; i < parts.Length; i++) expandedCmd = expandedCmd.Replace($"#{i}", parts[i].Trim('"'));
                        AppendOutput($"PS {_currentPath}> {rawCmd}\n[マクロ展開] => {expandedCmd}");
                        _streamWriter.WriteLine(expandedCmd);
                        _commandHistory.Add(rawCmd);
                        _historyIndex = _commandHistory.Count;
                        InputBox.Clear();
                        e.Handled = true;
                        return;
                    }
                }

                _commandHistory.Add(rawCmd);
                _historyIndex = _commandHistory.Count;
                AppendOutput($"PS {_currentPath}> {rawCmd}");
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
                    else { InputBox.Text = _commandHistory[_historyIndex]; InputBox.CaretIndex = InputBox.Text.Length; }
                }
                e.Handled = true;
            }
        }

        private void LoadMacros()
        {
            try { if (File.Exists(_macroFilePath)) { var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_macroFilePath)); if (loaded != null) { _macros = new Dictionary<string, string>(loaded, StringComparer.OrdinalIgnoreCase); return; } } } catch { }
            _macros = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "#renameFiles", "Get-Childitem -Path \"#1\" | foreach {rename-item -Path $_.FullName -newname \"#2\"}" } };
            SaveMacros();
        }

        private void SaveMacros() { try { File.WriteAllText(_macroFilePath, JsonSerializer.Serialize(_macros, new JsonSerializerOptions { WriteIndented = true })); } catch { } }

        private void ApplyCompletionMatch()
        {
            var match = _lastCompletion.CompletionMatches[_completionMatchIndex];
            int repIdx = _lastCompletion.ReplacementIndex;
            int repLen = _lastCompletion.ReplacementLength;
            InputBox.Text = _textBeforeCompletion.Substring(0, repIdx) + match.CompletionText + _textBeforeCompletion.Substring(repIdx + repLen);
            InputBox.CaretIndex = repIdx + match.CompletionText.Length;
        }

        public void ExecuteCommand(string cmd) => _streamWriter?.WriteLine(cmd);

        private void AppendOutput(string text) { if (text == null) return; Dispatcher.InvokeAsync(() => { OutputBox.AppendText(text + Environment.NewLine); OutputBox.ScrollToEnd(); }); }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e) { try { if (_process != null && !_process.HasExited) _process.Kill(); } catch { } finally { _completionPs?.Dispose(); } }

        // ★ 新設：人間の明示的な命令によってのみ発動する、真のカレント転移魔術ですわ
        public void ChangeDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "PC" || !Directory.Exists(path)) return;

            // 1. まずC#側の意識（プロンプト表示とRoslynエンジン）を楽観的に即時更新しますわ
            CurrentPath = path;

            // 2. 裏にいるPowerShell自身の肉体にも転移命令を叩き込みます
            ExecuteCommand($"cd \"{path}\"");

            AppendOutput($"\n[カレント同期] ファイラーの座標へ転移しましたわ => {CurrentPath}");
        }
    }

    public class FortressScriptGlobals
    {
        private readonly TerminalControl _terminal;
        public FortressScriptGlobals(TerminalControl terminal) { _terminal = terminal; }
        public string[] Args { get; set; } = new string[0];
        public string CurrentPath => _terminal.CurrentPath;
        public void Print(object obj) => _terminal.AppendOutputFromScript(obj?.ToString() ?? "null");
        public void Echo(object obj) => Print(obj);
        public string ReadFile(string path) => File.ReadAllText(Path.IsPathRooted(path) ? path : Path.Combine(CurrentPath, path));
        public void WriteFile(string path, string content) => File.WriteAllText(Path.IsPathRooted(path) ? path : Path.Combine(CurrentPath, path), content);
    }
}