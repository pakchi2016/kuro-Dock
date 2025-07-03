using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Features.AddressBar;
using Kuro_Dock.Features.FileList;
using Kuro_Dock.Features.FolderTree;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Kuro_Dock.ViewModels
{
    public partial class TabViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? header;

        public FolderTreeViewModel FolderTree { get; }
        public FileListViewModel FileList { get; }
        public AddressBarViewModel AddressBar { get; }

        public TabViewModel(FolderTreeViewModel folderTree, FileListViewModel fileList, AddressBarViewModel addressBar)
        {
            FolderTree = folderTree;
            FileList = fileList;
            AddressBar = addressBar;

            // FolderTreeViewModelのプロパティ変更を、このメソッドで受け取りますわ
            FolderTree.PropertyChanged += FolderTree_PropertyChanged;

            // FileListやAddressBarからのナビゲーション要求も、同様に受け取ります
            FileList.DirectoryNavigationRequested += FileList_DirectoryNavigationRequested;
            AddressBar.NavigationRequested += AddressBar_NavigationRequested;

            var initialDrive = FolderTree.RootDirectories.FirstOrDefault();
            if (initialDrive != null)
            {
                FolderTree.SelectedItem = initialDrive;
            }
        }

        /// <summary>
        /// FolderTreeViewModelからの報告を受け取るための、重要なメソッドですわ。
        /// </summary>
        private async void FolderTree_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 変更されたプロパティが「SelectedItem」であった場合にのみ、処理を実行します
            if (e.PropertyName == nameof(FolderTreeViewModel.SelectedItem))
            {
                var path = FolderTree.SelectedItem?.FullPath;
                if (path != null)
                {
                    // ここで、右ペイン（FileList）や他のUIを更新しますの
                    await FileList.LoadItemsAsync(path);
                    AddressBar.CurrentPath = path;
                    Header = string.IsNullOrEmpty(Path.GetFileName(path)) ? path.TrimEnd('\\') : Path.GetFileName(path);
                }
            }
        }

        private async void FileList_DirectoryNavigationRequested(string path)
        {
            await FolderTree.NavigateTo(path);
        }

        private async void AddressBar_NavigationRequested(string path)
        {
            if (Directory.Exists(path))
            {
                await FolderTree.NavigateTo(path);
            }
        }
    }
}