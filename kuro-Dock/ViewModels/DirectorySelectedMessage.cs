using CommunityToolkit.Mvvm.Messaging.Messages;
using Kuro_Dock.ViewModels;

// 選択されたViewModelを運ぶためのメッセージクラス
public class DirectorySelectedMessage : ValueChangedMessage<DirectoryItemViewModel>
{
    public DirectorySelectedMessage(DirectoryItemViewModel value) : base(value)
    {
    }
}