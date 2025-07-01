using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Features.AddressBar;
using Kuro_Dock.Features.FileList;
using Kuro_Dock.Features.FolderTree;
using System.IO;

namespace Kuro_Dock.ViewModels
{
    // メッセージ受信のためにインターフェースを実装しますの
    public partial class MainViewModel : ObservableObject,
        IRecipient<SelectedPathChangedMessage>,
        IRecipient<NavigatePathMessage>
    {
        private readonly IMessenger _messenger;
        public FolderTreeViewModel FolderTree { get; }
        public FileListViewModel FileList { get; }
        public AddressBarViewModel AddressBar { get; }

        // コンストラクタで、DIコンテナから各インスタンスを受け取りますわ
        public MainViewModel(
            IMessenger messenger,
            FolderTreeViewModel folderTree,
            FileListViewModel fileList,
            AddressBarViewModel addressBar)
        {
            _messenger = messenger;
            FolderTree = folderTree;
            FileList = fileList;
            AddressBar = addressBar;

            // 自身をメッセンジャーに受信者として登録しますの
            _messenger.RegisterAll(this);
        }

        // FolderTreeからのメッセージをここで受け取りますわ
        public async void Receive(SelectedPathChangedMessage message)
        {
            var path = message.Value;
            await FileList.LoadItemsAsync(path);
            AddressBar.CurrentPath = path;
        }

        // AddressBarやFileListからのメッセージをここで受け取りますわ
        public async void Receive(NavigatePathMessage message)
        {
            var path = message.Value;
            if (Directory.Exists(path))
            {
                await FolderTree.NavigateTo(path);
            }
        }
    }
}