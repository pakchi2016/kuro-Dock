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
        private bool _isLoaded = false;

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
            Children.Add(new DirectoryItemViewModel());
        }

        private DirectoryItemViewModel()
        {
            _model = new DirectoryItem { Name = "DUMMY" };
            _directoryService = null!;
        }

        /// <summary>
        /// 子要素を非同期で読み込みますの。
        /// </summary>
        public async Task LoadChildrenAsync()
        {
            // 既に読み込み済み、またはダミーノードの場合は何もしませんわ
            if (_isLoaded || _directoryService == null)
            {
                return;
            }

            Children.Clear(); // ダミーノードを削除
            var subDirModels = await Task.Run(() => _directoryService.GetSubDirectories(FullPath));
            foreach (var dirModel in subDirModels)
            {
                Children.Add(new DirectoryItemViewModel(dirModel, _directoryService));
            }
            _isLoaded = true; // 読み込み完了の印
        }

        // IsExpandedプロパティが変更されたときに、子の読み込みを実行しますわ
        async partial void OnIsExpandedChanged(bool value)
        {
            if (value)
            {
                await LoadChildrenAsync();
            }
        }
    }
}