using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;

namespace Kuro_DockFortress.Models
{
    public class TabItemModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // このタブが表示するファイルたちのリストですわ
        public ObservableCollection<FileItemModel> Files { get; set; } = new ObservableCollection<FileItemModel>();

        private string _header;
        public string Header
        {
            get => _header;
            set { _header = value; OnPropertyChanged(nameof(Header)); }
        }

        private string _currentPath;
        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                OnPropertyChanged(nameof(CurrentPath));

                if (value == "PC")
                {
                    Header = "PC";
                }
                else
                {
                    Header = new DirectoryInfo(value).Name;
                    if (string.IsNullOrEmpty(Header)) Header = value;
                }

                LoadDirectory(value);
            }
        }
        // ディレクトリの中身を美しくリストへ展開する処理です
        public void LoadDirectory(string path)
        {
            Files.Clear();

            // ★ 仮想パス "PC" が指定された場合、ドライブ一覧を展開しますわ
            if (path == "PC")
            {
                foreach (var drive in System.IO.DriveInfo.GetDrives())
                {
                    if (drive.IsReady)
                    {
                        Files.Add(new FileItemModel
                        {
                            Name = $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})", // 例: Local Disk (C:)
                            Path = drive.Name,
                            IsDirectory = true,
                            Size = (drive.TotalFreeSpace / (1024 * 1024 * 1024)).ToString("N0") + " GB 空き",
                            LastModified = DateTime.MinValue, // ドライブに更新日時は不要です
                            Icon = Helpers.IconHelper.GetIcon(drive.Name, true) // ドライブアイコンも美しく取得できますわ
                        });
                    }
                    else
                    {
                        // DVDドライブ等、準備ができていないディスクですわ
                        Files.Add(new FileItemModel
                        {
                            Name = $"({drive.Name.TrimEnd('\\')})",
                            Path = drive.Name,
                            IsDirectory = true,
                            Size = "",
                            LastModified = DateTime.MinValue,
                            Icon = Helpers.IconHelper.GetIcon(drive.Name, true)
                        });
                    }
                }
                return; // PCの描画が終わったら、以降の通常のフォルダ処理は行わずに抜けますわ
            }

            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            try
            {
                var dirInfo = new DirectoryInfo(path);

                // まずはフォルダを配備します
                foreach (var dir in dirInfo.GetDirectories())
                {
                    Files.Add(new FileItemModel
                    {
                        Name = dir.Name,
                        Path = dir.FullName,
                        IsDirectory = true,
                        Size = "",
                        LastModified = dir.LastWriteTime,
                        // ★フォルダのアイコンを要求しますわ！
                        Icon = Helpers.IconHelper.GetIcon(dir.FullName, true)
                    });
                }

                // 次にファイルを配備します
                foreach (var file in dirInfo.GetFiles())
                {
                    Files.Add(new FileItemModel
                    {
                        Name = file.Name,
                        Path = file.FullName,
                        IsDirectory = false,
                        Size = (file.Length / 1024).ToString("N0") + " KB",
                        LastModified = file.LastWriteTime,
                        // ★ファイルのアイコンを要求しますわ！
                        Icon = Helpers.IconHelper.GetIcon(file.FullName, false)
                    });
                }   
            }
            catch
            {
                // アクセス権限がないシステムフォルダ等でエラーが出た場合は握り潰しますわ
            }
        }

        // ★ 新設：外の世界（MainWindow等）から安全に再読み込みを命じるための公開窓口ですわ
        public void RefreshDirectory()
        {
            LoadDirectory(this.CurrentPath);
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}