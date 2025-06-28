using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Features.AddressBar;
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
        public AddressBarViewModel AddressBar { get; } // ★アドレスバー公国の統治者を任命

        public MainViewModel()
        {
            // 各公国の統治者を任命します
            FolderTree = new FolderTreeViewModel();
            FileList = new FileListViewModel();
            AddressBar = new AddressBarViewModel();

            // 各公国からの報告ルートを確立します
            FolderTree.PropertyChanged += FolderTree_PropertyChanged;
            FileList.DirectoryNavigationRequested += FileList_DirectoryNavigationRequested;
            AddressBar.NavigationRequested += AddressBar_NavigationRequested; // ★新しい外交ルートを確立
        }

        // フォルダツリーからの報告（領主交代）を受けた時の処理
        private async void FolderTree_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderTreeViewModel.SelectedItem))
            {
                var path = FolderTree.SelectedItem?.FullPath;
                // ファイル一覧とアドレスバーの両方に、新しい領主の土地を通知します
                await FileList.LoadItemsAsync(path);
                AddressBar.CurrentPath = path;
            }
        }

        // ファイル一覧からの報告（移動要請）を受けた時の処理
        private void FileList_DirectoryNavigationRequested(string path)
        {
            FolderTree.NavigateTo(path);
        }

        // ★★★ アドレスバーからの報告（転移要請）を受けた時の処理を追加 ★★★
        private void AddressBar_NavigationRequested(string path)
        {
            if (Directory.Exists(path))
            {
                // 宰相が、フォルダツリー公国とファイル一覧公国の両方に、転移を命じます
                FolderTree.NavigateTo(path);
            }
        }
    }
}
