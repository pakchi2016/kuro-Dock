// ViewModels/FileItemViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace Kuro_Dock.ViewModels
{
    public partial class FileItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? name;

        [ObservableProperty]
        private string? fullPath;

        [ObservableProperty]
        private string? itemType; // "ファイル" など

        [ObservableProperty]
        private long size;

        [ObservableProperty]
        private DateTime lastWriteTime;

        [ObservableProperty]
        private ImageSource? icon;
    }
}