using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Services;
using Kuro_Dock.Core.Models;
using System.Linq;

namespace Kuro_Dock.Features.FolderTree
{
    public partial class FolderTreeViewModel : ObservableObject
    {
        private readonly DirectoryService _directoryService;
        public ObservableCollection<DirectoryItemViewModel> RootDirectories { get; } = new();

        [ObservableProperty]
        private DirectoryItemViewModel? selectedItem;

        public FolderTreeViewModel()
        {
            _directoryService = new DirectoryService();
            LoadRootDirectories();
        }

        private void LoadRootDirectories()
        {
            RootDirectories.Clear();
            var rootDirModels = _directoryService.GetRootDirectories();
            foreach (var model in rootDirModels)
            {
                RootDirectories.Add(new DirectoryItemViewModel(model, _directoryService));
            }
        }

        // ★★★ 宰相からの勅命で、指定の場所へ移動する権能を追加 ★★★
        public void NavigateTo(string path)
        {
            // Note: This is a simplified navigation. A more robust solution
            // would involve expanding the tree to the specified path.
            // For now, we just set the selection.
            this.SelectedItem = new DirectoryItemViewModel(
                new DirectoryItem { FullPath = path, Name = System.IO.Path.GetFileName(path) },
                _directoryService
            );
        }
    }
}
