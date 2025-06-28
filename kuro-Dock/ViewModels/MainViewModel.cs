using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Features.FileList;
using Kuro_Dock.Features.FolderTree;
using System.ComponentModel;
using System.IO;

namespace Kuro_Dock.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public FolderTreeViewModel FolderTree { get; }
        public FileListViewModel FileList { get; }

        public MainViewModel()
        {
            FolderTree = new FolderTreeViewModel();
            FileList = new FileListViewModel();

            // 各公国からの報告ルートを確立します
            FolderTree.PropertyChanged += FolderTree_PropertyChanged;
            FileList.DirectoryNavigationRequested += FileList_DirectoryNavigationRequested;
        }

        // フォルダツリーからの報告（領主交代）を受けた時の処理
        private async void FolderTree_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderTreeViewModel.SelectedItem))
            {
                await FileList.LoadItemsAsync(FolderTree.SelectedItem?.FullPath);
            }
        }

        // ★★★ ファイル一覧からの報告（移動要請）を受けた時の処理を追加 ★★★
        private void FileList_DirectoryNavigationRequested(string path)
        {
            // 宰相が、フォルダツリー公国に「この地の者を、新しい領主に任命せよ」と勅命を下します。
            // これにより、FolderTree_PropertyChangedが連鎖的に呼ばれ、全てが更新されます。
            FolderTree.SelectedItem = new DirectoryItemViewModel(
                new DirectoryItem { FullPath = path, Name = Path.GetFileName(path) },
                new Core.Services.DirectoryService() // Serviceを渡します
            );
        }
    }
}
