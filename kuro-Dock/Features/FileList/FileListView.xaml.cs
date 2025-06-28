using Kuro_Dock.Features.FileList;
using System.Windows.Controls;

namespace Kuro_Dock.Features.FileList
{
    /// <summary>
    /// FileListView.xaml の相互作用ロジック
    /// </summary>
    public partial class FileListView : UserControl
    {
        public FileListView()
        {
            InitializeComponent();
            // このビューの魂はFileListViewModelであると定義します。
            // ただし、実際に魂を宿すのはMainWindowの役目なので、ここでは空のままにします。
        }
    }
}
