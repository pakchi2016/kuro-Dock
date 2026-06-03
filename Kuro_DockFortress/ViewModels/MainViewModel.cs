using System.Collections.ObjectModel;
using Kuro_DockFortress.Models;

namespace Kuro_DockFortress.ViewModels
{
    public class MainViewModel
    {
        // 左陣営と右陣営、それぞれのタブを管理するリストですわ
        public ObservableCollection<TabItemModel> LeftTabs { get; set; }
        public ObservableCollection<TabItemModel> RightTabs { get; set; }
        public ObservableCollection<BookmarkItemModel> Bookmarks { get; set; }
        public MainViewModel()
        {
            Bookmarks = Helpers.BookmarkManager.Load();
            LeftTabs = new ObservableCollection<TabItemModel>();
            RightTabs = new ObservableCollection<TabItemModel>();

            // 起動時に空っぽでは寂しいので、初期タブを1つずつ配備しておきます
            LeftTabs.Add(new TabItemModel { CurrentPath = "PC" });
            RightTabs.Add(new TabItemModel { CurrentPath = "PC" });

            
        }
    }
}