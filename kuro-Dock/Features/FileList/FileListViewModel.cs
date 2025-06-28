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

        // ★★★ 宰相への伝令イベントを追加 ★★★
        public event Action<string>? DirectoryNavigationRequested;

        public FileListViewModel()
        {
            _directoryService = new DirectoryService();
            _fileService = new FileService();
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

        // ★★★ 新しい命令（コマンド）を追加 ★★★
        [RelayCommand]
        private void OpenItem(object? item)
        {
            if (item is DirectoryItem dir)
            {
                // フォルダの場合、宰相に「この場所へ移動したい」と伝令を送ります
                DirectoryNavigationRequested?.Invoke(dir.FullPath);
            }
            else if (item is FileItem file)
            {
                // ファイルの場合、OSに開封を命じます
                try
                {
                    var psi = new ProcessStartInfo(file.FullPath) { UseShellExecute = true };
                    Process.Start(psi);
                }
                catch (Exception)
                {
                    // 開けなかった場合のエラー処理（今は何もしません）
                }
            }
        }
    }
}
