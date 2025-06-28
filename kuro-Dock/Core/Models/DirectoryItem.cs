namespace Kuro_Dock.Core.Models
{
/// <summary>
/// ディレクトリに関する純粋なデータを保持するクラスです。
/// このクラスは、いかなる動作も持ちません。
/// </summary>
    public class DirectoryItem
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
    }
}
