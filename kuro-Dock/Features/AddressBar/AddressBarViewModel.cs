using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging; // 追加
using Kuro_Dock.Core.Models; // 追加

namespace Kuro_Dock.Features.AddressBar
{

    public partial class AddressBarViewModel : ObservableObject
    {
        private readonly IMessenger _messenger;

        [ObservableProperty]
        private string? currentPath;

        public AddressBarViewModel(IMessenger messenger)
        {
            _messenger = messenger;
        }

        [RelayCommand]
        private void Navigate()
        {
            if (!string.IsNullOrEmpty(CurrentPath))
            {
                _messenger.Send(new NavigatePathMessage(CurrentPath));
            }
        }
    }
}
