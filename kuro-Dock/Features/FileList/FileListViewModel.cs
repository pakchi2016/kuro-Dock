using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kuro_Dock.Features.FileList
{
    public partial class FileListViewModel : ObservableObject
    {
        private readonly DirectoryService _directoryService;
        private readonly FileService _fileService;

        public ObservableCollection<object> Items { get; } = new();

        // C#の標準的なイベントで、ナビゲーション要求を通知します
        public event Action<string>? DirectoryNavigationRequested;

        public FileListViewModel(DirectoryService directoryService, FileService fileService)
        {
            _directoryService = directoryService;
            _fileService = fileService;
        }

        public async Task LoadItemsAsync(string? path)
        {
            Items.Clear();
            if (string.IsNullOrEmpty(path)) return;

            var directories = await Task.Run(() => _directoryService.GetSubDirectories(path));
            foreach (var dir in directories)
            {
                Items.Add(dir);
            }

            var files = await Task.Run(() => _fileService.GetFiles(path));
            foreach (var file in files)
            {
                Items.Add(file);
            }
        }

        [RelayCommand]
        private void OpenItem(object? item)
        {
            if (item is DirectoryItem dir)
            {
                // イベントを発行します
                DirectoryNavigationRequested?.Invoke(dir.FullPath);
            }
            else if (item is FileItem file)
            {
                try
                {
                    var psi = new ProcessStartInfo(file.FullPath) { UseShellExecute = true };
                    Process.Start(psi);
                }
                catch (Exception) { /* エラー処理 */ }
            }
        }
    }
}