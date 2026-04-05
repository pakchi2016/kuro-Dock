using CommunityToolkit.Mvvm.ComponentModel;
using Kuro_Dock.Core.Models;
using Kuro_Dock.Core.Utilities;
using System;
using System.Drawing;
using System.Windows.Media;

namespace Kuro_Dock.Features.FileList
{
    public partial class FileSystemItemViewModel : ObservableObject
    {
        public string FullPath { get; }
        public string Name { get; }
        public DateTime LastWriteTime { get; }
        public string ItemType { get; }
        public long Size { get; }
        public bool IsDirectory { get; }

        [ObservableProperty]
        private ImageSource? icon;

        // フォルダ用のコンストラクタですわ
        public FileSystemItemViewModel(DirectoryItem dir)
        {
            FullPath = dir.FullPath;
            Name = dir.Name;
            LastWriteTime = dir.LastWriteTime;
            ItemType = dir.ItemType;
            Size = 0; // フォルダサイズは取得が重いため、一旦0としておきます
            IsDirectory = true;

            LoadIconAsync();
        }

        // ファイル用のコンストラクタですわ
        public FileSystemItemViewModel(FileItem file)
        {
            FullPath = file.FullPath;
            Name = file.Name;
            LastWriteTime = file.LastWriteTime;
            ItemType = file.ItemType;
            Size = file.Size;
            IsDirectory = false;

            LoadIconAsync();
        }
        private async void LoadIconAsync()
        {
            // UIスレッドを解放し、別スレッドで重い抽出処理（またはキャッシュからの取り出し）を行います
            var loadedIcon = await Task.Run(() => IconManager.GetIcon(FullPath, IsDirectory));

            // 処理が終わると、このプロパティに値が入ります。
            // すると [ObservableProperty] の効果で、画面上の <Image> にアイコンがポンッと表示されますわ！
            Icon = loadedIcon;
        }
    }
}