using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kuro_Dock.Features.FolderTree
{
    public partial class FolderTreeViewModel : ObservableObject
    {
        private readonly DirectoryService? _directoryService;
        public ObservableCollection<DirectoryItemViewModel> RootDirectories { get; } = new();

        private DirectoryItemViewModel? _selectedItem;
        public DirectoryItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (ReferenceEquals(_selectedItem, value)) return;

                if (_selectedItem != null)
                {
                    _selectedItem.IsSelected = false;
                }

                if (SetProperty(ref _selectedItem, value))
                {
                    if (_selectedItem != null)
                    {
                        _selectedItem.IsSelected = true;
                    }
                }
            }
        }

        // デザイン時用の空のコンストラクタ
        public FolderTreeViewModel() { }

        public FolderTreeViewModel(DirectoryService directoryService)
        {
            _directoryService = directoryService;
            LoadRootDirectories();
        }

        private void LoadRootDirectories()
        {
            if (_directoryService == null) return;

            RootDirectories.Clear();
            var rootDirModels = _directoryService.GetRootDirectories();
            foreach (var model in rootDirModels)
            {
                RootDirectories.Add(new DirectoryItemViewModel(model, _directoryService, this));
            }
        }

        public async Task NavigateTo(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path) || _directoryService == null) return;

            var parts = path.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            var rootPart = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(rootPart)) return;

            var currentNode = RootDirectories.FirstOrDefault(r => r.FullPath.Equals(rootPart, StringComparison.OrdinalIgnoreCase));
            if (currentNode == null) return;

            for (int i = 1; i < parts.Length; i++)
            {
                currentNode.IsExpanded = true;
                await currentNode.LoadChildrenAsync();

                var nextPart = parts[i];
                var nextNode = currentNode.Children.FirstOrDefault(c => c.Name.Equals(nextPart, StringComparison.OrdinalIgnoreCase));

                if (nextNode == null)
                {
                    this.SelectedItem = currentNode;
                    return;
                }
                currentNode = nextNode;
            }
            this.SelectedItem = currentNode;
        }
    }
}