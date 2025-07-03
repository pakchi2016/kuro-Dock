using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Kuro_Dock.Core.Models
{
    /// <summary>
    /// どのViewModelから送信されたかを示すための基底クラスですわ
    /// </summary>
    public abstract class ViewModelMessage
    {
        public object Sender { get; }

        protected ViewModelMessage(object sender)
        {
            Sender = sender;
        }
    }

    /// <summary>
    /// 特定のパスへの画面遷移を要求するメッセージですわ。
    /// </summary>
    public class NavigatePathMessage : ViewModelMessage
    {
        public string Path { get; }
        public NavigatePathMessage(string path, object sender) : base(sender)
        {
            Path = path;
        }
    }

    /// <summary>
    /// フォルダーツリーで選択されたパスが変更されたことを通知するメッセージですわ。
    /// </summary>
    public class SelectedPathChangedMessage : ViewModelMessage
    {
        public string? Path { get; }
        public SelectedPathChangedMessage(string? path, object sender) : base(sender)
        {
            Path = path;
        }
    }
}