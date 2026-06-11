using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Kuro_DockGenesis.Models
{
    public class BookmarkItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Path { get; set; }

        // 別名（エイリアス）ですわ。JSONにも自動保存されます。
        private string _alias;
        public string Alias
        {
            get => _alias;
            set
            {
                _alias = value;
                // 値が変わった瞬間に、画面に「表示を更新しなさい」と美しく命じます
                OnPropertyChanged(nameof(Alias));
                OnPropertyChanged(nameof(Name));
            }
        }

        [JsonIgnore]
        public string Name
        {
            get
            {
                // チェック3： Alias に文字が入っていれば、それを最優先で返すようになっているか
                if (!string.IsNullOrWhiteSpace(Alias)) return Alias;

                return string.IsNullOrEmpty(Path) ? string.Empty : System.IO.Path.GetFileName(Path);
            }
        }

        [JsonIgnore]
        public bool IsFolder => Directory.Exists(Path);

        // 通知魔法のコア部分ですわ
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private double _initialOffsetX = 0;
        [JsonIgnore]
        public double InitialOffsetX
        {
            get => _initialOffsetX;
            set { _initialOffsetX = value; OnPropertyChanged(nameof(InitialOffsetX)); }
        }

        private double _initialOffsetY = -20;
        [JsonIgnore]
        public double InitialOffsetY
        {
            get => _initialOffsetY;
            set { _initialOffsetY = value; OnPropertyChanged(nameof(InitialOffsetY)); }
        }
        private ImageSource _icon;
        [JsonIgnore]
        public ImageSource Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(nameof(Icon)); }
        }
    }
}
