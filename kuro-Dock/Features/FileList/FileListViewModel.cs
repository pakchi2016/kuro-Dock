using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
        private readonly IMessenger _messenger;
        private readonly DirectoryService _directoryService;
        private readonly FileService _fileService;

        public ObservableCollection<object> Items { get; } = new();
        
        public FileListViewModel(IMessenger messenger)
        {
            _messenger = messenger;
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

        [RelayCommand]
        private void OpenItem(object? item)
        {
            if (item is DirectoryItem dir)
            {
                _messenger.Send(new NavigatePathMessage(dir.FullPath));
            }
            else if (item is FileItem file)
            {
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
