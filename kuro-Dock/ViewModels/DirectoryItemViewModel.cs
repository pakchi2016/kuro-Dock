using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.IO;
using System.Threading.Tasks;

namespace Kuro_Dock.ViewModels
{
    public partial class DirectoryItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? name;

        [ObservableProperty]
        private string? fullPath;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsLoaded))] // IsExpandedが変わったらIsLoadedも変わったと通知
        private bool isExpanded;

        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private string? itemType; // "ファイル フォルダー" など

        [ObservableProperty]
        private DateTime lastWriteTime;

        [ObservableProperty]
        private ImageSource? icon;

        // 子要素が読み込み済みかどうかを示すプロパティ
        public bool IsLoaded => Children.Count > 0 && Children[0].FullPath is not null;
        partial void OnIsSelectedChanged(bool value)
        {
            if (value)
            {
                WeakReferenceMessenger.Default.Send(new DirectorySelectedMessage(this));
            }
        }

        public ObservableCollection<DirectoryItemViewModel> Children { get; } = new();

        // こちらが通常のフォルダ・ドライブ用のコンストラクタ
        public DirectoryItemViewModel()
        {
        }

        // こちらはUIに「+」を表示させるためのダミー要素を作る、特別なプライベートコンストラクタ
        private DirectoryItemViewModel(string dummyText)
        {
            Name = dummyText;
        }

        // IsExpandedプロパティがtrueに変更されたときに自動で呼び出される
        async partial void OnIsExpandedChanged(bool value)
        {
            if (value && !IsLoaded) // 展開され、かつ、まだ読み込んでいない場合のみ実行
            {
                await LoadChildrenAsync();
            }
        }

        private async Task LoadChildrenAsync()
        {
            if (FullPath is null) return;

            // まず、ダミーではない子要素をすべてクリアする（再読み込みの場合など）
            Children.Clear();

            try
            {
                var subDirs = await Task.Run(() => Directory.GetDirectories(FullPath));
                if (subDirs.Length > 0)
                {
                    foreach (var dir in subDirs)
                    {
                        Children.Add(new DirectoryItemViewModel
                        {
                            Name = Path.GetFileName(dir),
                            FullPath = dir
                        });
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス権がない場合は、エラーメッセージを持つ子を追加するなどしても良い
                Children.Add(new DirectoryItemViewModel("[Access Denied]"));
            }
        }

        // MainViewModelから呼ばれる初期化メソッド
        public void Initialize()
        {
            // 自分自身にサブフォルダが存在する可能性がある場合のみ、ダミーの子を追加する
            try
            {
                if (FullPath is not null && Directory.GetDirectories(FullPath).Length > 0)
                {
                    Children.Add(new DirectoryItemViewModel("")); // ダミーを追加
                }
            }
            catch (UnauthorizedAccessException)
            {
                // アクセス権がない場合は何もしない（展開ボタンが表示されない）
            }
        }
    }
}