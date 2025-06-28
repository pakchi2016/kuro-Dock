using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Core.Services;
using System.Threading.Tasks;
using System.Linq;

namespace Kuro_Dock.Features.FolderTree
{
    public partial class DirectoryItemViewModel : ObservableObject
    {
        private readonly DirectoryItem _model;
        private readonly DirectoryService _directoryService;

        public string Name => _model.Name;
        public string FullPath => _model.FullPath;

        [ObservableProperty]
        private bool isExpanded;

        public bool IsLoaded => Children.Count > 0 && Children.First()._model.Name != null;

        public ObservableCollection<DirectoryItemViewModel> Children { get; } = new();

        public DirectoryItemViewModel(DirectoryItem model, DirectoryService directoryService)
        {
            _model = model;
            _directoryService = directoryService;
            if (_directoryService.GetSubDirectories(FullPath).Any())
            {
                Children.Add(new DirectoryItemViewModel());
            }
        }

        private DirectoryItemViewModel()
        {
            _model = new DirectoryItem { Name = null! };
            _directoryService = new DirectoryService();
        }

        async partial void OnIsExpandedChanged(bool value)
        {
            if (value && !IsLoaded)
            {
                Children.Clear();
                var subDirModels = await Task.Run(() => _directoryService.GetSubDirectories(FullPath));
                foreach (var dirModel in subDirModels)
                {
                    Children.Add(new DirectoryItemViewModel(dirModel, _directoryService));
                }
            }
        }
    }
}
