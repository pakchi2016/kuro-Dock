using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        // ★ object型ではなく、アイコンプロパティを持つ新しいクラスに変更しますわ
        public ObservableCollection<FileSystemItemViewModel> Items { get; } = new();

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
                // ★ 生のデータではなく、ViewModelで包んでから追加しますのよ
                Items.Add(new FileSystemItemViewModel(dir));
            }

            var files = await Task.Run(() => _fileService.GetFiles(path));
            foreach (var file in files)
            {
                // ★ こちらも同様ですわ
                Items.Add(new FileSystemItemViewModel(file));
            }
        }

        [RelayCommand]
        private void OpenItem(FileSystemItemViewModel? item) // ★ 引数の型も変更します
        {
            if (item == null) return;

            // ★ is判定ではなく、プロパティでフォルダかどうかを判定します
            if (item.IsDirectory)
            {
                DirectoryNavigationRequested?.Invoke(item.FullPath);
            }
            else
            {
                try
                {
                    var psi = new ProcessStartInfo(item.FullPath) { UseShellExecute = true };
                    Process.Start(psi);
                }
                catch (Exception) { /* エラー処理 */ }
            }
        }
    }
}