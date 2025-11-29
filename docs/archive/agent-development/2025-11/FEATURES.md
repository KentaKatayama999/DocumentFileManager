# DocumentFileManager 機能一覧

最終更新日: 2025-10-08

## 概要

DocumentFileManagerは、資料ファイルの管理とチェック項目の紐づけを行うWPFアプリケーションです。資料ごとにチェックリストを表示し、確認状態を管理できます。

---

## 1. 資料管理機能

### 1.1 資料の登録

**実装場所**: `MainWindow.xaml.cs`

- **ファイル選択ダイアログ**: 複数ファイルを一度に選択可能
- **ドラッグ＆ドロップ**: ファイルをリストビューにドロップして登録
- **相対パス管理**: プロジェクトルートからの相対パスで保存
- **重複チェック**: 既に登録済みの資料は警告を表示

```csharp
// 主要メソッド
private async Task<bool> RegisterDocumentAsync(string filePath)
private string GetRelativePath(string absolutePath)
```

**対応ファイル形式**:
- PDF (*.pdf)
- Word (*.docx, *.doc)
- Excel (*.xlsx, *.xls)
- その他すべてのファイル

### 1.2 資料の表示・操作

- **リスト表示**: ListView で資料一覧を表示
- **ファイルを開く**: 資料をダブルクリックで外部アプリケーションで開く
- **件数表示**: 登録された資料の総数を表示

### 1.3 資料情報

各資料には以下の情報を保持:
- ファイル名 (FileName)
- 相対パス (RelativePath)
- ファイル種別 (FileType)
- 登録日時 (AddedAt)

---

## 2. チェック項目管理機能

### 2.1 チェック項目の階層構造

**実装場所**: `CheckItemUIBuilder.cs`

- **3階層構造**: 大分類 → 中分類 → 小分類 → チェック項目
- **動的UI構築**: JSON定義からWPF UI（GroupBox/CheckBox）を自動生成
- **色分け表示**: 階層ごとに異なる枠線色で視覚的に区別

```csharp
// 主要メソッド
public async Task BuildAsync(Panel containerPanel, Document? document = null)
private UIElement CreateGroupBox(CheckItemViewModel viewModel, int depth)
```

### 2.2 チェック項目の表示制御

- **WrapPanel対応**: 項目数が多い場合は複数列で表示
  - チェック項目: 15個以上で自動的に複数列表示
  - 分類: 3個以上で自動的に複数列表示
- **最大列数**: 4列まで（設定変更可能）
- **幅の自動調整**: 内容に応じて最適な幅を計算

### 2.3 チェック項目の色設定

**設定ファイル**: `appsettings.json` → `UISettings.Colors`

| 階層 | デフォルト色 | 用途 |
|------|------------|------|
| Depth0 | `#2C3E50` | 大分類 |
| Depth1 | `#34495E` | 中分類 |
| Depth2 | `#5D6D7E` | 小分類・チェック項目を含むGroupBox |
| DepthDefault | `#95A5A6` | それ以外 |

**特記事項**: チェックボックスを含むGroupBoxは、階層に関わらず常に小分類色（Depth2）を使用

---

## 3. チェックリストウィンドウ機能

### 3.1 ウィンドウの起動

**実装場所**: `MainWindow.xaml.cs:325`

- **起動方法**: 資料リストからファイルをダブルクリック
- **同時動作**: ファイルを外部アプリで開き、同時にチェックリストウィンドウを表示
- **シングルトン管理**: 同時に1つのチェックリストウィンドウのみ表示

```csharp
// 主要メソッド
private void DocumentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
private void OpenChecklistWindow(Document document)
```

### 3.2 ウィンドウのサイズと配置

**実装場所**: `ChecklistWindow.xaml.cs:98-109`

- **幅**: 画面の1/3
- **高さ**: 画面全体（タスクバーを除く作業領域）
- **デフォルト位置**: 画面右端
- **リサイズ**: 幅のみ変更可能（高さは常に画面全体に固定）

```csharp
private void InitializeWindowSize()
{
    var workArea = SystemParameters.WorkArea;
    Width = workArea.Width / 3.0;
    Height = workArea.Height;
    Left = workArea.Right - Width;  // 右端配置
    Top = workArea.Top;
}
```

### 3.3 ウィンドウの固定機能

**実装場所**: `ChecklistWindow.xaml.cs:79-93`

**Win32 APIによる移動ブロック**:
- `WM_WINDOWPOSCHANGING` メッセージをインターセプト
- `SWP_NOMOVE` フラグでOS レベルで位置変更を完全ブロック
- ドラッグ操作による一切のぶれを防止

```csharp
private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
    if (msg == WM_WINDOWPOSCHANGING && !_isAdjustingPosition)
    {
        var windowPos = Marshal.PtrToStructure<WINDOWPOS>(lParam);
        windowPos.flags |= SWP_NOMOVE;  // 移動をブロック
        Marshal.StructureToPtr(windowPos, lParam, true);
        handled = false;
    }
    return IntPtr.Zero;
}
```

### 3.4 配置ボタン

**実装場所**: `ChecklistWindow.xaml.cs:189-230`

- **左に配置**: ウィンドウを画面左端に移動
- **右に配置**: ウィンドウを画面右端に移動
- **位置調整フラグ**: `_isAdjustingPosition` でボタン操作時のみ移動を許可

### 3.5 常に手前に表示

**実装場所**: `ChecklistWindow.xaml:28`, `ChecklistWindow.xaml.cs:52`

- **デフォルト**: ON（チェック済み）
- **切り替え可能**: ツールバーのチェックボックスでON/OFF
- **効果**: 他のウィンドウの前面に常に表示

### 3.6 MainWindowとの連携

**実装場所**: `MainWindow.xaml.cs:373-415`

- **表示切り替え**:
  - ChecklistWindow表示時 → MainWindowを非表示（Hide）
  - ChecklistWindow終了時 → MainWindowを再表示（Show + Activate）
- **親子関係の切断**: `Owner = null` で親子関係を切り、MainWindowのポップアップを防止

```csharp
// ChecklistWindow表示時
Hide();
_checklistWindow.Show();

// ChecklistWindow終了時
_checklistWindow.Closed += (s, args) =>
{
    _checklistWindow = null;
    Show();
    Activate();
};
```

---

## 4. 資料-チェック項目の紐づけ機能

### 4.1 チェック状態の保存

**実装場所**: `CheckItemUIBuilder.cs:268-330`

**2つの保存モード**:

1. **グローバルモード** (Document未指定)
   - `CheckItem.Status` を更新
   - 全体的なチェック状態を管理

2. **資料別モード** (Document指定)
   - `CheckItemDocument` テーブルに保存
   - 資料ごとのチェック状態を管理

```csharp
private async Task SaveStatusAsync(CheckItemViewModel viewModel)
{
    if (_currentDocument == null)
    {
        // グローバルモード: CheckItem.Statusを更新
        await _repository.UpdateAsync(viewModel.Entity);
    }
    else
    {
        // 資料別モード: CheckItemDocumentに保存
        if (viewModel.IsChecked)
        {
            // チェックON → 紐づけを追加
            var checkItemDocument = new CheckItemDocument
            {
                DocumentId = _currentDocument.Id,
                CheckItemId = viewModel.Entity.Id,
                LinkedAt = DateTime.UtcNow
            };
            await _checkItemDocumentRepository.AddAsync(checkItemDocument);
        }
        else
        {
            // チェックOFF → 紐づけを削除
            await _checkItemDocumentRepository.DeleteAsync(existing.Id);
        }
    }
}
```

### 4.2 チェック状態の復元

**実装場所**: `CheckItemUIBuilder.cs:42-72`

- **復元タイミング**: ChecklistWindow読み込み時
- **データ取得**: `CheckItemDocumentRepository.GetByDocumentIdAsync()`
- **状態適用**: ViewModelのIsCheckedプロパティに反映

```csharp
// Documentと紐づいたチェック項目を取得
var linkedItems = await _checkItemDocumentRepository.GetByDocumentIdAsync(document.Id);
var checkItemDocuments = linkedItems.ToDictionary(x => x.CheckItemId);

// ViewModelに反映
if (checkItemDocuments.TryGetValue(item.Id, out var linkedItem))
{
    viewModel.IsChecked = true;
}
```

---

## 5. UI設定機能

### 5.1 設定の外部化

**設定ファイル**: `appsettings.json`

```json
{
  "UISettings": {
    "GroupBox": { /* GroupBoxのサイズ・余白設定 */ },
    "CheckBox": { /* CheckBoxのサイズ・余白設定 */ },
    "Colors": { /* 階層別の色設定 */ },
    "Layout": { /* レイアウト設定 */ }
  },
  "PathSettings": {
    "ChecklistJsonPath": "data/checklist.json",
    "ProjectRootLevelsUp": 5
  }
}
```

### 5.2 設定ウィンドウ

**実装場所**: `SettingsWindow.xaml.cs`

- **UI設定**: GroupBox/CheckBoxのサイズ、色、レイアウト
- **パス設定**: チェックリストJSONファイルのパス、プロジェクトルート階層
- **設定保存**: JSON形式でファイルに保存
- **再起動で反映**: 設定変更後はアプリケーション再起動が必要

---

## 6. データベース機能

### 6.1 Entity Framework Core

**実装場所**: `DocumentManagerContext.cs`

- **DBプロバイダー**: SQLite
- **データベースファイル**: `documentmanager.db`
- **マイグレーション**: Code-First アプローチ

### 6.2 テーブル構成

| テーブル名 | 説明 | 主要カラム |
|-----------|------|----------|
| Documents | 資料情報 | Id, FileName, RelativePath, FileType, AddedAt |
| CheckItems | チェック項目 | Id, Label, Path, IsItem, ParentId, Status |
| CheckItemDocuments | 資料-チェック項目紐づけ | Id, DocumentId, CheckItemId, LinkedAt |

### 6.3 リレーション

```
Document (1) ----< (*) CheckItemDocument (*) >---- (1) CheckItem
                        中間テーブル
```

### 6.4 リポジトリパターン

**実装場所**: `Infrastructure/Repositories/`

- `IDocumentRepository` / `DocumentRepository`
- `ICheckItemRepository` / `CheckItemRepository`
- `ICheckItemDocumentRepository` / `CheckItemDocumentRepository`

主要メソッド:
- `GetAllAsync()` - 全件取得
- `GetByIdAsync(int id)` - ID指定取得
- `AddAsync()` - 追加
- `UpdateAsync()` - 更新
- `DeleteAsync(int id)` - 削除
- `SaveChangesAsync()` - 変更保存

---

## 7. データ同期機能

### 7.1 JSON → データベース同期

**実装場所**: `ChecklistLoader.cs`

- **自動同期**: アプリケーション起動時にJSON定義をDBに同期
- **階層管理**: Parent-Child関係を自動構築
- **Path生成**: 階層構造に基づいて一意のPathを生成

```csharp
// 同期処理の流れ
1. JSONファイル読み込み (checklist.json)
2. 既存のCheckItemをすべて削除
3. JSON定義を再帰的に解析してCheckItemエンティティ作成
4. データベースに保存
```

### 7.2 チェックリストJSON形式

**ファイル**: `data/checklist.json`

```json
{
  "categories": [
    {
      "label": "大分類",
      "children": [
        {
          "label": "中分類",
          "children": [
            {
              "label": "小分類",
              "items": [
                "チェック項目1",
                "チェック項目2"
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

---

## 8. ログ機能

### 8.1 Microsoft.Extensions.Logging

**実装場所**: 全クラス

- **ログレベル**: Debug, Information, Warning, Error
- **出力先**: コンソール（デバッグ時）
- **主要ログ**:
  - 資料の登録・読み込み
  - チェック項目の読み込み・保存
  - ウィンドウの表示・非表示
  - エラー情報

```csharp
// ログ出力例
_logger.LogInformation("資料を登録しました: {FileName} ({RelativePath})",
    document.FileName, document.RelativePath);
_logger.LogError(ex, "チェック項目の読み込みに失敗しました");
```

---

## 9. ユースケース対応状況

| ID | ユースケース | 実装状況 | 備考 |
|----|------------|---------|------|
| UC-001 | 資料を登録する | ✅ 完了 | ファイル選択・D&D対応 |
| UC-002 | 資料を削除する | ⬜ 未実装 | |
| UC-003 | 資料を開く | ✅ 完了 | ダブルクリックで外部アプリ起動 |
| UC-004 | チェック項目を確認する | ✅ 完了 | 資料別チェックリスト表示 |
| UC-005 | チェック項目を編集する | ⬜ 未実装 | JSON手動編集のみ |
| UC-006 | 資料にチェック項目を紐づける | ✅ 完了 | CheckItemDocument自動管理 |

---

## 10. 今後の拡張予定

### 10.1 優先度: 高

- [ ] 資料削除機能
- [ ] 検索・フィルタ機能
- [ ] チェック項目の完了率表示

### 10.2 優先度: 中

- [ ] チェック項目のUIでの編集
- [ ] 資料のカテゴリ分類
- [ ] エクスポート機能（CSV、Excel）

### 10.3 優先度: 低

- [ ] 複数ユーザー対応
- [ ] クラウド同期
- [ ] モバイル対応
