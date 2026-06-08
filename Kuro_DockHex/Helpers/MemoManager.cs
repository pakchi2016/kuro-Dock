using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using Kuro_DockHex.Models;

namespace Kuro_DockHex.Helpers
{
    public static class MemoManager
    {
        // 魔法陣の足元（実行ファイルの場所）に記録を残します
        private static readonly string FilePath = "memos.json";
        private static readonly string CsvFilePath = "archive_memos.csv";
        public static void Save(ObservableCollection<MemoItemModel> memos)
        {
            // 日本語がエスケープされるのを防ぐ絶対命令ですわ
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(memos, options);

            // BOM付きUTF-8として出力し、人間（卿）が直接編集できる美しさを保証します
            using (var sw = new StreamWriter(FilePath, false, new UTF8Encoding(true)))
            {
                sw.Write(json);
            }
        }

        public static ObservableCollection<MemoItemModel> Load()
        {
            if (!File.Exists(FilePath)) return new ObservableCollection<MemoItemModel>();

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                var list = JsonSerializer.Deserialize<ObservableCollection<MemoItemModel>>(json);
                return list ?? new ObservableCollection<MemoItemModel>();
            }
            catch
            {
                // 読み込みに失敗した場合は、エラーで落とさず空のリストを返して要塞を守ります
                return new ObservableCollection<MemoItemModel>();
            }
        }
        public static void ArchiveToCsv(MemoItemModel memo)
        {
            // メモ本文にカンマや改行、引用符が含まれていてもCSVが壊れないようエスケープします
            string safeText = memo.Text?.Replace("\"", "\"\"") ?? "";

            // 期限日時と、タスクを完了（削除）した現在日時を用意します
            string targetDateStr = memo.TargetDate?.ToString("yyyy/MM/dd HH:mm") ?? "期限なし";
            string completedDateStr = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");

            // CSVの1行を構築します (ID, 内容, 期限, 完了日時)
            string csvLine = $"\"{memo.Id}\",\"{safeText}\",\"{targetDateStr}\",\"{completedDateStr}\"";

            // true を指定することで「追記モード」となり、ファイルがなければ作成、あれば末尾に書き足します
            // もちろん、人間が美しく読めるようBOM付きUTF-8を強制しますわ
            using (var sw = new StreamWriter(CsvFilePath, true, new UTF8Encoding(true)))
            {
                sw.WriteLine(csvLine);
            }
        }
    }
}