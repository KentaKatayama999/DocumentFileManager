using System.Collections.Generic;
using System.Threading.Tasks;
using DocumentFileManager.Entities;

namespace DocumentFileManager.UI.Services
{
    /// <summary>
    /// データ整合性サービスのインターフェース
    /// データベースと物理ファイルの整合性をチェック・修復する
    /// </summary>
    public interface IDataIntegrityService
    {
        /// <summary>
        /// データ整合性をチェックする
        /// </summary>
        /// <returns>整合性チェックレポート</returns>
        Task<IntegrityReport> CheckIntegrityAsync();

        /// <summary>
        /// 物理ファイルが見つからない資料を検索する
        /// </summary>
        /// <returns>見つからない資料のリスト</returns>
        Task<List<Document>> FindMissingFilesAsync();

        /// <summary>
        /// 孤立したキャプチャ画像を検索する
        /// （DBに紐づいていないキャプチャファイル）
        /// </summary>
        /// <returns>孤立したキャプチャファイルパスのリスト</returns>
        Task<List<string>> FindOrphanedCapturesAsync();

        /// <summary>
        /// データ整合性を修復する
        /// </summary>
        /// <param name="report">整合性チェックレポート</param>
        /// <param name="options">修復オプション</param>
        Task RepairIntegrityAsync(IntegrityReport report, RepairOptions options);
    }
}
