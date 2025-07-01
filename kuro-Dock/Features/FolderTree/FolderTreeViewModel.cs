using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Services;
using Kuro_Dock.Core.Models;
using System.Linq;
using System;
using CommunityToolkit.Mvvm.Messaging;

namespace Kuro_Dock.Features.FolderTree
{
    public partial class FolderTreeViewModel : ObservableObject
    {
        private readonly IMessenger _messenger;
        private readonly DirectoryService _directoryService;
        public ObservableCollection<DirectoryItemViewModel> RootDirectories { get; } = new();

        [ObservableProperty]
        private DirectoryItemViewModel? selectedItem;

        public FolderTreeViewModel(IMessenger messenger)
        {
            _messenger = messenger;
            _directoryService = new DirectoryService();
            LoadRootDirectories();
        }

        partial void OnSelectedItemChanged(DirectoryItemViewModel? value)
        {
            if (value?.FullPath is string path)
            {
                _messenger.Send(new SelectedPathChangedMessage(path));
            }
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

        /// <summary>
        /// 指定されたパスまでツリーを展開し、項目を選択しますの。
        /// </summary>
        public async Task NavigateTo(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            // パスを正規化し、各部分に分割しますわ
            var parts = path.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
            // ドライブ名（例: "C:\"）を正しく扱います
            var rootPart = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(rootPart)) return;

            // まずはルートドライブを探しますの
            var currentNode = RootDirectories.FirstOrDefault(r => r.FullPath.Equals(rootPart, StringComparison.OrdinalIgnoreCase));
            if (currentNode == null) return; // 見つからなければ終了

            // パスの残りの部分を辿っていきますわ
            // parts[0]はドライブ名なので、インデックス1から開始します
            for (int i = 1; i < parts.Length; i++)
            {
                // 現在のノードを展開し、子を読み込みます
                currentNode.IsExpanded = true;
                await currentNode.LoadChildrenAsync();

                var nextPart = parts[i];
                var nextNode = currentNode.Children.FirstOrDefault(c => c.Name.Equals(nextPart, StringComparison.OrdinalIgnoreCase));

                if (nextNode == null)
                {
                    // パスがツリー内に見つかりませんでしたわ
                    this.SelectedItem = currentNode; // 途中まで選択
                    return;
                }
                currentNode = nextNode;
            }

            // 最終的に見つかったノードを選択状態にしますの
            this.SelectedItem = currentNode;
        }
    }
}