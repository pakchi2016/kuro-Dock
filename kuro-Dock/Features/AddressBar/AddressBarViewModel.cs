using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Kuro_Dock.Features.AddressBar
{
    public partial class AddressBarViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? currentPath;

        // こちらもC#の標準的なイベントで通知します
        public event Action<string>? NavigationRequested;

        public AddressBarViewModel() { }

        [RelayCommand]
        private void Navigate()
        {
            if (!string.IsNullOrEmpty(CurrentPath))
            {
                // イベントを発行します
                NavigationRequested?.Invoke(CurrentPath);
            }
        }
    }
}