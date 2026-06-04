using System;
using System.Collections.ObjectModel;
using Kuro_DockHex.Models;

namespace Kuro_DockHex.ViewModels
{
    public class MainViewModel
    {
        // 魔法陣に刻まれるルーンのリストですわ
        public ObservableCollection<MemoItemModel> Memos { get; set; }

        public MainViewModel()
        {
            Memos = Helpers.MemoManager.Load();
        }
    }
}