using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Kuro_DockThrone.Core.Models;
using Kuro_DockThrone.Core.Storage;
using Kuro_DockThrone.Core.Helpers;

namespace Kuro_DockGrimoire
{
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        private bool _isClosing = false;
        private int _currentAnimationId = 0;
        private int _currentSubAnimationId = 0;

        public ObservableCollection<GrimoireUIBookmark> CurrentBookmarks { get; set; } = new ObservableCollection<GrimoireUIBookmark>();
        public ObservableCollection<GrimoireUIBookmark> CurrentSubBookmarks { get; set; } = new ObservableCollection<GrimoireUIBookmark>();

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public MainWindow()
        {
            InitializeComponent();
            BookmarksPanel.ItemsSource = CurrentBookmarks;
            SubBookmarksPanel.ItemsSource = CurrentSubBookmarks;
            LoadBookmarks();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Win32Point mousePos = new Win32Point();
            GetCursorPos(ref mousePos);

            this.Left = mousePos.X - 104;
            this.Top = mousePos.Y;

            // ★ 究極の調和：画面下端までの「残りサイズ」を計算し、これ以上の膨張を許さない結界（MaxHeight）を張りますわ！
            var workArea = SystemParameters.WorkArea;
            double availableHeight = workArea.Bottom - this.Top - 5; // 5pxの美しい余白

            if (availableHeight > 100) // 最低限の器の高さ（100px）は保証します
            {
                this.MaxHeight = availableHeight;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // 横幅の補正：ウィンドウが広がった分だけ左へ引き戻し、右端のインデックスを完全固定します
            if (e.WidthChanged && e.PreviousSize.Width > 0)
            {
                this.Left -= (e.NewSize.Width - e.PreviousSize.Width);
            }

            // ★ 縦幅の補正（上に押し上げてUIを破壊する粗暴な魔法）は、結界によって不要となったため完全に消し去りましたわ！
        }

        private void LoadBookmarks()
        {
            try
            {
                var multiLevelData = ThroneStorage.LoadBookmarks();
                ItemsControl_Indexes.ItemsSource = multiLevelData;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "Kuro-Dock Grimoire - 法典エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void IndexButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is IndexModel indexItem)
            {
                int animationId = ++_currentAnimationId;
                await Task.Delay(150);
                if (animationId != _currentAnimationId) return;

                System.Windows.Point pos = element.TranslatePoint(new System.Windows.Point(0, 0), DockPanel);
                BookmarksPanel.Margin = new Thickness(0, pos.Y, 0, 0);

                CurrentBookmarks.Clear();
                _currentSubAnimationId++;
                CurrentSubBookmarks.Clear();

                if (indexItem.Bookmarks.Count > 0)
                {
                    var first = indexItem.Bookmarks[0];
                    var firstUI = new GrimoireUIBookmark { Name = !string.IsNullOrWhiteSpace(first.Alias) ? first.Alias : Path.GetFileName(first.Path), Path = first.Path, InitialOffsetX = 50, InitialOffsetY = 0 };
                    CurrentBookmarks.Add(firstUI);

                    await Task.Delay(180);

                    for (int i = 1; i < indexItem.Bookmarks.Count; i++)
                    {
                        if (animationId != _currentAnimationId) break;
                        var b = indexItem.Bookmarks[i];
                        CurrentBookmarks.Add(new GrimoireUIBookmark
                        {
                            Name = !string.IsNullOrWhiteSpace(b.Alias) ? b.Alias : Path.GetFileName(b.Path),
                            Path = b.Path,
                            InitialOffsetX = 0,
                            InitialOffsetY = -20
                        });
                        await Task.Delay(30);
                    }
                }
            }
        }

        private async void Bookmark_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GrimoireUIBookmark bookmark)
            {
                int animationId = ++_currentSubAnimationId;
                await Task.Delay(150);
                if (animationId != _currentSubAnimationId) return;

                System.Windows.Point pos = element.TranslatePoint(new System.Windows.Point(0, 0), DockPanel);
                SubBookmarksPanel.Margin = new Thickness(0, pos.Y, 0, 0);

                CurrentSubBookmarks.Clear();

                if (Directory.Exists(bookmark.Path))
                {
                    try
                    {
                        var entries = Directory.EnumerateFileSystemEntries(bookmark.Path).ToList();
                        if (entries.Count > 0)
                        {
                            string firstPath = entries[0];
                            bool isFirstFolder = Directory.Exists(firstPath);

                            CurrentSubBookmarks.Add(new GrimoireUIBookmark
                            {
                                Name = Path.GetFileName(firstPath),
                                Path = firstPath,
                                InitialOffsetX = 50,
                                InitialOffsetY = 0,
                                Icon = IconHelper.GetIcon(firstPath, isFirstFolder)
                            });

                            await Task.Delay(180);

                            for (int i = 1; i < entries.Count; i++)
                            {
                                if (animationId != _currentSubAnimationId) break;
                                string path = entries[i];
                                bool isFolder = Directory.Exists(path);

                                CurrentSubBookmarks.Add(new GrimoireUIBookmark
                                {
                                    Name = Path.GetFileName(path),
                                    Path = path,
                                    InitialOffsetX = 0,
                                    InitialOffsetY = -20,
                                    Icon = IconHelper.GetIcon(path, isFolder)
                                });

                                await Task.Delay(30);
                            }
                        }
                    }
                    catch { }
                }
            }
        }

        private void Bookmark_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is GrimoireUIBookmark bookmark)
            {
                NavigateExplorer(bookmark.Path);
            }
        }

        private void NavigateExplorer(string targetPath)
        {
            if (App.TargetExplorerHwnd == IntPtr.Zero) return;

            if (!Directory.Exists(targetPath) && !File.Exists(targetPath))
            {
                System.Windows.MessageBox.Show($"指定された座標は既にこの世界から消失していますわ。\n{targetPath}", "Kuro-Dock Grimoire", MessageBoxButton.OK, MessageBoxImage.Warning);
                SafeClose();
                return;
            }

            try
            {
                Type shellAppType = Type.GetTypeFromProgID("Shell.Application");
                if (shellAppType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellAppType);
                    dynamic windows = shell.Windows();

                    foreach (dynamic ie in windows)
                    {
                        if (ie != null && new IntPtr((long)ie.HWND) == App.TargetExplorerHwnd)
                        {
                            ie.Navigate(targetPath);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"転移魔術の詠唱中に未知の妨害を受けましたわ:\n{ex.Message}", "Kuro-Dock Grimoire", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            SafeClose();
        }

        public void Reload法典()
        {
            LoadBookmarks();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            SafeClose();
        }

        private void SafeClose()
        {
            if (!_isClosing)
            {
                _isClosing = true;
                this.Close();
            }
        }
    }

    public class GrimoireUIBookmark : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Name { get; set; }
        public string Path { get; set; }

        private double _initialOffsetX = 0;
        public double InitialOffsetX
        {
            get => _initialOffsetX;
            set { _initialOffsetX = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitialOffsetX))); }
        }

        private double _initialOffsetY = -20;
        public double InitialOffsetY
        {
            get => _initialOffsetY;
            set { _initialOffsetY = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InitialOffsetY))); }
        }

        private System.Windows.Media.ImageSource _icon;
        public System.Windows.Media.ImageSource Icon
        {
            get => _icon;
            set { _icon = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Icon))); }
        }
    }
}