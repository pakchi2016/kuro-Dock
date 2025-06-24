using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging; 
using System.Collections.ObjectModel;
using Kuro_Dock.Services; 
using Kuro_Dock.Models;
using System.IO;

namespace Kuro_Dock.ViewModels
{
    // ここに IRecipient<DirectorySelectedMessage> を再度追加！
    public partial class MainViewModel : ObservableObject, IRecipient<DirectorySelectedMessage>
    {
        public string WindowTitle { get; } = "Kuro-Dock by わらわ";

        public ObservableCollection<TabViewModel> Tabs { get; } = new();

        [ObservableProperty]
        private TabViewModel? selectedTab;

        private readonly BookmarkService _bookmarkService;
        public ObservableCollection<BookmarkItem> Bookmarks { get; } = new();

        public MainViewModel()
        {
            _bookmarkService = new BookmarkService();
            WeakReferenceMessenger.Default.Register(this);
            LoadBookmarks();
            AddNewTab();
        }

        private void LoadBookmarks()
        {
            var bookmarks = _bookmarkService.LoadBookmarks();
            foreach (var bookmark in bookmarks)
            {
                Bookmarks.Add(bookmark);
            }
        }

        public void Receive(DirectorySelectedMessage message)
        {
            // アクティブなタブが存在し、そのタブのSelectedDirectoryを更新する
            if (SelectedTab is not null)
            {
                SelectedTab.SelectedDirectory = message.Value;
            }
        }

        [RelayCommand]
        private void AddNewTab()
        {
            var newTab = new TabViewModel();
            Tabs.Add(newTab);
            SelectedTab = newTab;
        }

        [RelayCommand]
        private void CloseTab(TabViewModel? tab)
        {
            if (tab is not null)
            {
                Tabs.Remove(tab);
            }
        }

        [RelayCommand]
        private void AddCurrentBookmark()
        {
            if (SelectedTab?.CurrentPath is null) return;

            var newBookmark = new BookmarkItem
            {
                // 現在のフォルダ名をブックマーク名にする
                Name = SelectedTab.Header ?? Path.GetFileName(SelectedTab.CurrentPath),
                Path = SelectedTab.CurrentPath
            };

            Bookmarks.Add(newBookmark);
            _bookmarkService.SaveBookmarks(Bookmarks); // 忘れずに保存
        }

        [RelayCommand]
        private void NavigateToBookmark(BookmarkItem? bookmark)
        {
            if (bookmark is null || SelectedTab is null) return;

            // 選択中のタブに、ブックマークの場所へ移動するように命令する
            SelectedTab.NavigateTo(bookmark.Path, true);
        }
    }
}