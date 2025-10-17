using System;
using System.Collections.Generic;
using DocumentFileManager.Entities;

namespace DocumentFileManager.UI.Services
{
    /// <summary>
    /// データ整合性チェックレポート
    /// </summary>
    public class IntegrityReport
    {
        /// <summary>
        /// チェック実行日時
        /// </summary>
        public DateTime CheckedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 物理ファイルが見つからない資料のリスト
        /// </summary>
        public List<Document> MissingFiles { get; set; } = new();

        /// <summary>
        /// 孤立したキャプチャ画像のパスリスト
        /// </summary>
        public List<string> OrphanedCaptures { get; set; } = new();

        /// <summary>
        /// 整合性に問題がないかどうか
        /// </summary>
        public bool IsHealthy => MissingFiles.Count == 0 && OrphanedCaptures.Count == 0;

        /// <summary>
        /// 問題の総数
        /// </summary>
        public int TotalIssues => MissingFiles.Count + OrphanedCaptures.Count;

        /// <summary>
        /// サマリーテキストを取得
        /// </summary>
        public string GetSummary()
        {
            if (IsHealthy)
            {
                return "データ整合性に問題はありません。";
            }

            var issues = new List<string>();

            if (MissingFiles.Count > 0)
            {
                issues.Add($"見つからない資料ファイル: {MissingFiles.Count}件");
            }

            if (OrphanedCaptures.Count > 0)
            {
                issues.Add($"孤立したキャプチャ画像: {OrphanedCaptures.Count}件");
            }

            return $"問題が見つかりました。{string.Join(", ", issues)}";
        }
    }

    /// <summary>
    /// データ整合性修復オプション
    /// </summary>
    public class RepairOptions
    {
        /// <summary>
        /// 見つからない資料ファイルのDBレコードを削除するか
        /// </summary>
        public bool RemoveMissingDocuments { get; set; } = true;

        /// <summary>
        /// 孤立したキャプチャ画像を削除するか
        /// </summary>
        public bool RemoveOrphanedCaptures { get; set; } = true;

        /// <summary>
        /// 削除前にバックアップを作成するか
        /// </summary>
        public bool CreateBackup { get; set; } = true;
    }
}
