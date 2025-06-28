using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Services;
using Kuro_Dock.Core.Models;

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
    }
}
