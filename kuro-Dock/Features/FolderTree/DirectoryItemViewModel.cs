using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Kuro_Dock.Features.FolderTree
{
    [DebuggerDisplay("{FullPath}")]
    public partial class DirectoryItemViewModel : ObservableObject
    {
        private readonly DirectoryItem _model;
        private readonly DirectoryService? _directoryService;
        private readonly FolderTreeViewModel _parentViewModel;
        private bool _isLoaded = false;

        public string Name => _model.Name;
        public string FullPath => _model.FullPath;
        public bool IsLoaded => _isLoaded;

        [ObservableProperty]
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        public ObservableCollection<DirectoryItemViewModel> Children { get; } = new();

        public DirectoryItemViewModel(DirectoryItem model, DirectoryService directoryService, FolderTreeViewModel parentViewModel)
        {
            _model = model;
            _directoryService = directoryService;
            _parentViewModel = parentViewModel;
            Children.Add(new DirectoryItemViewModel(parentViewModel));
        }

        private DirectoryItemViewModel(FolderTreeViewModel parentViewModel)
        {
            _model = new DirectoryItem { Name = "DUMMY" };
            _directoryService = null;
            _parentViewModel = parentViewModel;
        }

        public async Task LoadChildrenAsync()
        {
            if (_isLoaded || _directoryService == null) return;

            Children.Clear();
            var subDirModels = await Task.Run(() => _directoryService.GetSubDirectories(FullPath));
            foreach (var dirModel in subDirModels)
            {
                Children.Add(new DirectoryItemViewModel(dirModel, _directoryService, _parentViewModel));
            }
            _isLoaded = true;
        }

        partial void OnIsSelectedChanged(bool value)
        {
            if (value)
            {
                _parentViewModel.SelectedItem = this;
            }
        }

        async partial void OnIsExpandedChanged(bool value)
        {
            if (value)
            {
                await LoadChildrenAsync();
            }
        }

        public override string ToString()
        {
            return this.FullPath ?? "Invalid Item";
        }
    }
}