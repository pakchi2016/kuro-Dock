using System.Collections.Generic;

namespace Kuro_DockGrimoire.Models
{
    // ★ 子：具体的な転移先を表すルーンです
    public class BookmarkModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }

    // ★ 親：複数のブックマークを束ねる「インデックス（禁書目録）」ですわ
    public class IndexModel
    {
        public string Name { get; set; }
        public List<BookmarkModel> Bookmarks { get; set; } = new List<BookmarkModel>();
    }
}