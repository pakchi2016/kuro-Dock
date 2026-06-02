using System;
using System.Windows.Media;

namespace Kuro_DockFortress.Models
{
    public class FileItemModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool IsDirectory { get; set; }
        public string Size { get; set; }
        public DateTime LastModified { get; set; }

        public ImageSource Icon { get; set; }

        // フォルダかファイルかを画面上で分かりやすくするためのプロパティですわ
        public string Type => IsDirectory ? "ファイル フォルダー" : System.IO.Path.GetExtension(Path).ToUpper();
    }
}