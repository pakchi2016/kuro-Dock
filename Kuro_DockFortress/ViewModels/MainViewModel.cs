using System.Collections.ObjectModel;
using Kuro_DockFortress.Models;
using Kuro_DockThrone.Core.Models;
using Kuro_DockThrone.Core.Storage;

namespace Kuro_DockFortress.ViewModels
{
    public class MainViewModel
    {
        // 左陣営と右陣営、それぞれのタブを管理するリストですわ
        public ObservableCollection<TabItemModel> LeftTabs { get; set; }
        public ObservableCollection<TabItemModel> RightTabs { get; set; }
        public List<IndexModel> CoreBookmarks { get; set; }
        public MainViewModel()
        {
            ReloadBookmarks();
            LeftTabs = new ObservableCollection<TabItemModel>();
            RightTabs = new ObservableCollection<TabItemModel>();

            // 起動時に空っぽでは寂しいので、初期タブを1つずつ配備しておきます
            LeftTabs.Add(new TabItemModel { CurrentPath = "PC" });
            RightTabs.Add(new TabItemModel { CurrentPath = "PC" });
        }
        public void ReloadBookmarks() => CoreBookmarks = ThroneStorage.LoadBookmarks();
        public void SaveBookmarks() => ThroneStorage.SaveBookmarks(CoreBookmarks);
    }
}