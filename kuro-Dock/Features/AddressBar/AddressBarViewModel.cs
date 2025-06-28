using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;

namespace Kuro_Dock.Features.AddressBar
{
    /// <summary>
    /// アドレスバーの表示と動作を管理するViewModelです。
    /// </summary>
    public partial class AddressBarViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? currentPath;

        // 宰相へ「この場所へ移動したい」と伝えるための伝令イベントです
        public event Action<string>? NavigationRequested;

        [RelayCommand]
        private void Navigate()
        {
            if (!string.IsNullOrEmpty(CurrentPath))
            {
                NavigationRequested?.Invoke(CurrentPath);
            }
        }
    }
}
