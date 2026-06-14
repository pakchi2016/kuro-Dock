using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Kuro_DockThrone.Core.Models;

namespace Kuro_DockThrone.Core.Storage
{
    public static class ThroneStorage
    {
        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static readonly string KuroDockDir = Path.Combine(AppDataPath, "Kuro-Dock");
        private static readonly string JsonPath = Path.Combine(KuroDockDir, "shared_bookmarks.json");

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions WriteOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        /// <summary>
        /// 聖域から法典（JSON）を解読し、データモデルとして召喚しますわ。
        /// ファイルが存在しない場合は、初期データを自動で錬成して保存します。
        /// </summary>
        public static List<IndexModel> LoadBookmarks()
        {
            // 領地が存在しなければ開拓しますわ
            if (!Directory.Exists(KuroDockDir))
            {
                Directory.CreateDirectory(KuroDockDir);
            }

            // 法典がなければ、初期の契約データを書き込みます
            if (!File.Exists(JsonPath))
            {
                var defaultData = CreateDefaultData();
                SaveBookmarks(defaultData);
                return defaultData;
            }

            try
            {
                string jsonContent = File.ReadAllText(JsonPath, Encoding.UTF8);
                var data = JsonSerializer.Deserialize<List<IndexModel>>(jsonContent, ReadOptions);
                return data ?? CreateDefaultData();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"法典の解読中に未知の汚染を検知しましたわ: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 現在のデータモデルを、美しいBOM付きUTF-8の法典（JSON）へと刻み込み、永続化します。
        /// </summary>
        public static void SaveBookmarks(List<IndexModel> data)
        {
            if (data == null) return;

            try
            {
                if (!Directory.Exists(KuroDockDir))
                {
                    Directory.CreateDirectory(KuroDockDir);
                }

                string jsonString = JsonSerializer.Serialize(data, WriteOptions);
                // 卿の確立した、日本語を美しく保つためのエンコード魔法（BOM付きUTF-8）ですわ
                File.WriteAllText(JsonPath, jsonString, new UTF8Encoding(true));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"法典への記録（書き込み）に失敗しましたわ: {ex.Message}", ex);
            }
        }

        private static List<IndexModel> CreateDefaultData()
        {
            return new List<IndexModel>
            {
                new IndexModel
                {
                    Name = "📁 システム領域",
                    Bookmarks = new List<BookmarkModel>
                    {
                        new BookmarkModel { Alias = "◆ Cドライブ", Path = @"C:\" },
                        new BookmarkModel { Alias = "◆ ドキュメント", Path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }
                    }
                }
            };
        }
    }
}