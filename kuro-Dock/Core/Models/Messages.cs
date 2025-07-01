using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Kuro_Dock.Core.Models
{
    /// <summary>
    /// 特定のパスへの画面遷移を要求するメッセージですわ。
    /// </summary>
    public class NavigatePathMessage : ValueChangedMessage<string>
    {
        public NavigatePathMessage(string path) : base(path) { }
    }

    /// <summary>
    /// フォルダーツリーで選択されたパスが変更されたことを通知するメッセージですわ。
    /// </summary>
    public class SelectedPathChangedMessage : ValueChangedMessage<string>
    {
        public SelectedPathChangedMessage(string path) : base(path) { }
    }
}