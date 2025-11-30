using System.Collections.ObjectModel;
using DocumentFileManager.Entities;
using DocumentFileManager.UI.Models;
using DocumentFileManager.UI.ViewModels;

namespace DocumentFileManager.UI.Factories;

/// <summary>
/// CheckItemViewModelを生成するファクトリのインターフェース
/// </summary>
public interface ICheckItemViewModelFactory
{
    /// <summary>
    /// 単一のEntityからViewModelを生成
    /// </summary>
    /// <param name="entity">チェック項目エンティティ</param>
    /// <param name="windowMode">ウィンドウモード</param>
    /// <param name="checkItemDocument">紐づけ情報（オプション）</param>
    /// <returns>生成されたViewModel</returns>
    CheckItemViewModel Create(
        CheckItem entity,
        WindowMode windowMode,
        CheckItemDocument? checkItemDocument = null);

    /// <summary>
    /// Entity階層からViewModel階層を生成
    /// </summary>
    /// <param name="entities">チェック項目エンティティのリスト</param>
    /// <param name="windowMode">ウィンドウモード</param>
    /// <param name="checkItemDocuments">紐づけ情報のディクショナリ（オプション）</param>
    /// <returns>生成されたViewModelのコレクション</returns>
    List<CheckItemViewModel> CreateHierarchy(
        List<CheckItem> entities,
        WindowMode windowMode,
        Dictionary<int, CheckItemDocument>? checkItemDocuments = null);
}
