﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows; // ★MessageBoxのために追加

namespace Kuro_Dock.ViewModels
{
    public partial class TabViewModel : ObservableObject
    {
        private readonly Stack<string> _backHistory = new();
        private readonly Stack<string> _forwardHistory = new();

        [ObservableProperty]
        private string? header;

        [ObservableProperty]
        private string? currentPath;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GoUpCommand))]
        [NotifyCanExecuteChangedFor(nameof(NavigateBackCommand))]
        [NotifyCanExecuteChangedFor(nameof(NavigateForwardCommand))]
        private DirectoryItemViewModel? selectedDirectory;

        public ObservableCollection<DirectoryItemViewModel> RootDirectories { get; } = new();
        public ObservableCollection<object> DisplayedItems { get; } = new();

        // --- コンストラクタ ---
        public TabViewModel()
        {
            // ★★★ わらわの魂の叫び ★★★
            MessageBox.Show("新しいタブのコンストラクタが呼ばれました！\nこれからPCビューを準備します。");

            Header = "PC";
            LoadRootDirectories();
            LoadDisplayedItems();
        }

        // --- メソッド ---
        partial void OnSelectedDirectoryChanged(DirectoryItemViewModel? value)
        {
            CurrentPath = value?.FullPath;
            Header = value?.Name ?? "PC"; // デフォルトを"新しいタブ"から"PC"へ
            LoadDisplayedItems();
        }

        public void NavigateTo(string? path, bool isNewAction)
        {
            if (path is null || !Directory.Exists(path)) return;

            if (isNewAction && SelectedDirectory?.FullPath is not null)
            {
                if (SelectedDirectory.FullPath != path)
                {
                    _backHistory.Push(SelectedDirectory.FullPath);
                    _forwardHistory.Clear();
                }
            }

            var newDirItem = new DirectoryItemViewModel { FullPath = path };
            newDirItem.Name = string.IsNullOrEmpty(Path.GetFileName(path)) ? path : Path.GetFileName(path);
            newDirItem.Initialize();

            SelectedDirectory = newDirItem;
        }

        private void LoadRootDirectories()
        {
            RootDirectories.Clear();
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                if (drive.IsReady)
                {
                    var driveVm = new DirectoryItemViewModel
                    {
                        Name = drive.Name,
                        FullPath = drive.RootDirectory.FullName
                    };
                    driveVm.Initialize();
                    RootDirectories.Add(driveVm);
                }
            }
        }

        private async void LoadDisplayedItems()
        {
            DisplayedItems.Clear();
            if (SelectedDirectory?.FullPath is null)
            {
                // (PCビューの処理は変更なし)
                foreach (var drive in RootDirectories)
                {
                    DisplayedItems.Add(drive);
                }
                return;
            }

            try
            {
                // まず、名前や日付など、すぐに取得できる情報だけを持つViewModelのリストを作る
                var items = new List<object>();

                foreach (var dir in Directory.GetDirectories(SelectedDirectory.FullPath))
                {
                    var dirInfo = new DirectoryInfo(dir);
                    items.Add(new DirectoryItemViewModel
                    {
                        Name = dirInfo.Name,
                        FullPath = dirInfo.FullName,
                        ItemType = "ファイル フォルダー",
                        LastWriteTime = dirInfo.LastWriteTime
                    });
                }

                foreach (var file in Directory.GetFiles(SelectedDirectory.FullPath))
                {
                    var fileInfo = new FileInfo(file);
                    items.Add(new FileItemViewModel
                    {
                        Name = fileInfo.Name,
                        FullPath = fileInfo.FullName,
                        ItemType = fileInfo.Extension + " ファイル",
                        Size = fileInfo.Length,
                        LastWriteTime = fileInfo.LastWriteTime
                    });
                }

                // 先にUIへリストを反映させる。この時点ではアイコンはまだ空っぽ。
                // これで、フリーズせずに、まずファイル名の一覧が瞬時に表示される。
                foreach (var item in items)
                {
                    DisplayedItems.Add(item);
                }

                // ここからが分身の術。時間のかかるアイコン取得を裏の処理に任せる。
                await Task.Run(() =>
                {
                    foreach (var item in items)
                    {
                        if (item is DirectoryItemViewModel dir)
                        {
                            dir.Icon = IconManager.GetIcon(dir.FullPath!, true);
                        }
                        else if (item is FileItemViewModel file)
                        {
                            file.Icon = IconManager.GetIcon(file.FullPath!, false);
                        }
                    }
                });
            }
            catch (UnauthorizedAccessException) { /* ... */ }
        }

        // --- コマンド ---
        [RelayCommand]
        private void OpenItem(object? item)
        {
            if (item is DirectoryItemViewModel dir)
            {
                NavigateTo(dir.FullPath, true);
            }
            else if (item is FileItemViewModel file && file.FullPath is not null)
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo(file.FullPath) { UseShellExecute = true };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (System.Exception ex) { System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}"); }
            }
        }

        [RelayCommand]
        private void NavigateToPath() => NavigateTo(CurrentPath, true);

        [RelayCommand]
        private void Refresh() => LoadDisplayedItems();

        [RelayCommand(CanExecute = nameof(CanGoUp))]
        private void GoUp() => NavigateTo(Directory.GetParent(SelectedDirectory!.FullPath!)?.FullName, true);
        private bool CanGoUp() => SelectedDirectory?.FullPath is not null && Directory.GetParent(SelectedDirectory.FullPath) is not null;

        [RelayCommand(CanExecute = nameof(CanNavigateBack))]
        private void NavigateBack()
        {
            if (_backHistory.TryPop(out var path))
            {
                if (SelectedDirectory?.FullPath is not null)
                {
                    _forwardHistory.Push(SelectedDirectory.FullPath);
                }
                NavigateTo(path, false);
            }
        }
        private bool CanNavigateBack() => _backHistory.Count > 0;

        [RelayCommand(CanExecute = nameof(CanNavigateForward))]
        private void NavigateForward()
        {
            if (_forwardHistory.TryPop(out var path))
            {
                if (SelectedDirectory?.FullPath is not null)
                {
                    _backHistory.Push(SelectedDirectory.FullPath);
                }
                NavigateTo(path, false);
            }
        }
        private bool CanNavigateForward() => _forwardHistory.Count > 0;
    }
}