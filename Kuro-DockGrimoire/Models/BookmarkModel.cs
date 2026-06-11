using System.Text.Json.Serialization; // ★ 追加：JSONの読み書きから除外する呪文に必要です

namespace Kuro_DockGrimoire.Models
{
    public class BookmarkModel
    {
        // Genesisの規格に合わせた真名ですわ
        public string Alias { get; set; }
        public string Path { get; set; }

        // ★ 新設：画面に表示するためだけの、動的な「仮の姿」です。
        // JSONには書き込まれないよう [JsonIgnore] で隠蔽します。
        [JsonIgnore]
        public string DisplayName
        {
            get
            {
                // 1. Aliasが存在するなら、当然それを優先します
                if (!string.IsNullOrWhiteSpace(Alias))
                    return Alias;

                // 2. そもそもパスすら空なら警告を表示します
                if (string.IsNullOrWhiteSpace(Path))
                    return "未設定の領域";

                try
                {
                    // 3. "C:\" などのルートドライブはファイル名が取れないため、そのまま返します
                    if (Path.Length <= 3 && Path.EndsWith(":\\"))
                        return Path;

                    // 4. パスの末尾からフォルダ名（またはファイル名）だけを美しく抽出します
                    string name = System.IO.Path.GetFileName(Path.TrimEnd('\\', '/'));
                    return string.IsNullOrEmpty(name) ? Path : name;
                }
                catch
                {
                    // 万が一解析に失敗した場合は、パスをそのまま表示する防壁ですわ
                    return Path;
                }
            }
        }
    }

    public class IndexModel
    {
        public string Name { get; set; }
        public System.Collections.Generic.List<BookmarkModel> Bookmarks { get; set; } = new System.Collections.Generic.List<BookmarkModel>();
    }
}