using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuro_DockGenesis.Models
{
    public class IndexItem
    {
        public string Name { get; set; }

        // インデックスの中に、複数のブックマーク（中身）を保持するリストですわ
        public ObservableCollection<BookmarkItem> Bookmarks { get; set; } = new ObservableCollection<BookmarkItem>();
    }
}
