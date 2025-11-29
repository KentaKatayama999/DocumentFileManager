# CheckItem関連コードの現状分析

## 1. 概要

本ドキュメントは、DocumentFileManagerのチェック項目（CheckItem）機能に関する現状分析をまとめたものです。大規模リファクタリングを実施する前の状態を記録し、問題点と改善方針を明確化することを目的としています。

**分析日**: 2025-11-29
**対象ブランチ**: `feature/state-based-checkitem`
**前回のリファクタリング**: MVVMパターン導入（DataTemplate + Attached Behaviors）

---

## 2. アーキテクチャ図

### 現在の全体構成

```
┌─────────────────────────────────────────────────────────────┐
│                   現在のアーキテクチャ                        │
└─────────────────────────────────────────────────────────────┘

[View層]
├── MainWindow.xaml(.cs)           ← 読み取り専用モード
├── ChecklistWindow.xaml(.cs)      ← 編集可能モード
└── DataTemplate (CheckItemTemplate) ← XAML定義

           ↑ バインディング

[ViewModel層]
└── CheckItemViewModel             ← UI状態管理 (INotifyPropertyChanged)

           ↑ 構築・コマンド設定

[Builder層]
└── CheckItemUIBuilder             ← ★問題あり（God Class）
    ├── UI階層構築
    ├── ViewModel構築
    ├── コマンド設定
    └── 状態変更ハンドリング

           ↑ 状態遷移管理

[Service層]
├── ChecklistStateManager          ← 状態管理サービス
└── CheckItemTransition            ← 状態遷移ロジック

           ↑ データアクセス

[Repository層]
├── ICheckItemRepository
└── ICheckItemDocumentRepository

           ↑

[DB層]
└── CheckItemDocument              ← 永続化エンティティ
```

### データフロー

```
[チェックON操作時]
User Click
    ↓
CheckBox Event (via Attached Behavior)
    ↓
ViewModel.IsChecked = true
    ↓
CheckedChangedCommand.Execute()
    ↓
CheckItemUIBuilder.HandleCheckOnAsync()
    ↓
ChecklistStateManager.HandleCheckOnAsync()
    ↓
CheckItemTransition.TransitionTo10/11()
    ↓
CommitTransitionAsync() → DB保存
    ↓
ViewModel更新 → UI自動更新
```

---

## 3. クラス責務一覧

### 3.1 CheckItemViewModel

**ファイル**: `src/DocumentFileManager.UI/ViewModels/CheckItemViewModel.cs`
**行数**: 約230行
**評価**: ★★★★☆ 概ね良好

**責務**:
- UI表示用の状態管理（IsChecked, CaptureFilePath）
- プロパティ変更通知（INotifyPropertyChanged）
- コマンドプロパティの保持（SelectCommand, CheckedChangedCommand, ViewCaptureCommand）
- CameraButtonVisibilityの計算

**問題点**:
```csharp
public Visibility CameraButtonVisibility
{
    get
    {
        // ★問題: getter内でファイルI/O
        if (!File.Exists(absolutePath))
            return Visibility.Collapsed;
    }
}
```

### 3.2 CheckItemTransition

**ファイル**: `src/DocumentFileManager.UI/Models/CheckItemTransition.cs`
**評価**: ★★★★★ 優秀

**責務**:
- 状態遷移ロジックの一元管理
- 状態コード体系の定義

**状態コード体系**:
```
状態コード: "XY" (X=チェック状態, Y=キャプチャ状態)

00 = 未紐づけ
10 = チェックON、キャプチャなし
11 = チェックON、キャプチャあり
20 = チェックOFF（履歴あり）、キャプチャなし
22 = チェックOFF（履歴あり）、キャプチャあり
```

**遷移メソッド**:
| メソッド | 状態遷移 | 用途 |
|---------|--------|------|
| `TransitionTo10()` | → "10" | チェックON（キャプチャなし） |
| `TransitionTo11()` | → "11" | チェックON（キャプチャあり） |
| `RestoreTo11()` | → "11" | 既存キャプチャを復帰 |
| `TransitionToOff()` | → "20"/"22" | チェックOFF |
| `Rollback()` | 元の状態に戻す | キャンセル時 |

### 3.3 ChecklistStateManager

**ファイル**: `src/DocumentFileManager.UI/Services/ChecklistStateManager.cs`
**評価**: ★★★★★ 優秀

**責務**:
- ビジネスロジックの統括
- DB操作の管理
- ダイアログ表示（既存キャプチャ確認）

**主要メソッド**:
- `HandleCheckOnAsync()`: チェックON時の処理
- `HandleCheckOffAsync()`: チェックOFF時の処理
- `CommitTransitionAsync()`: DB確定

### 3.4 CheckItemUIBuilder

**ファイル**: `src/DocumentFileManager.UI/Helpers/CheckItemUIBuilder.cs`
**行数**: 約450行
**評価**: ★★☆☆☆ 要改善（God Class）

**現在の責務（過剰）**:
1. UI階層構築（GroupBox/ContentControl生成）
2. ViewModel階層構築
3. コマンド設定（MainWindow用/ChecklistWindow用分岐）
4. 状態変更ハンドリング（HandleCheckOnAsync/HandleCheckOffAsync呼び出し）
5. キャプチャ削除処理
6. パス解決

**問題のあるコード**:
```csharp
private void SetupCommands(CheckItemViewModel viewModel)
{
    if (viewModel.IsMainWindow)
    {
        // MainWindow用コマンド設定
        viewModel.SelectCommand = new AsyncRelayCommand(...);
    }
    else
    {
        // ChecklistWindow用コマンド設定
        viewModel.CheckedChangedCommand = new AsyncRelayCommand(...);
    }

    // 共通コマンド（キャプチャ表示）
    viewModel.ViewCaptureCommand = new RelayCommand(() =>
    {
        // ★問題: UIBuilder内でUIロジック
        var viewer = new CaptureImageViewerWindow(...);
        viewer.ShowDialog();

        // ★問題: 削除時のDB更新をTask.Run().Wait()でブロック
        if (viewer.IsDeleted)
        {
            Task.Run(async () => { ... }).Wait();
        }
    });
}
```

### 3.5 CheckBoxBehaviors

**ファイル**: `src/DocumentFileManager.UI/Behaviors/CheckBoxBehaviors.cs`
**行数**: 約170行
**評価**: ★★★★★ 優秀

**責務**:
- Attached Propertyによるコマンドバインディング
- CheckedChangedCommand: ChecklistWindow用（チェック状態変更時）
- ClickCommand: MainWindow用（クリック時、状態変更なし）

**特徴**:
- PreviewMouseLeftButtonDownでクリックをインターセプト
- メモリリーク防止（Unloadedイベントでハンドラ解除）

---

## 4. 状態管理フロー

### 4.1 MainWindow（読み取り専用モード）

```
特徴:
- IsMainWindow = true
- チェック状態は表示のみ（DBに書き込まない）
- クリック時はSelectCommand実行（資料フィルタリング）
- キャプチャは全Documentの最新版を表示

フロー:
1. BuildAsync() でViewModel構築
   └─ 全チェック項目の最新キャプチャを読み込み
2. ClickCommand（PreviewMouseLeftButtonDown）
   └─ チェック状態変更をブロック（e.Handled = true）
   └─ SelectCommand.Execute()
3. OnItemSelected コールバック
   └─ HandleCheckItemSelected()
   └─ FilterDocumentsByCheckItem()
```

### 4.2 ChecklistWindow（編集可能モード）

```
特徴:
- IsMainWindow = false
- チェック状態はDBに即座に反映
- Document単位でキャプチャを管理

フロー:
1. BuildAsync() でViewModel構築
   └─ 該当DocumentのIsChecked/CaptureFileを復元
2. CheckedChangedCommand（Checked/Uncheckedイベント）
   └─ IsCheckedがTwoWayバインディングで変更
   └─ HandleCheckOnAsync() または HandleCheckOffAsync()
3. ChecklistStateManager で状態遷移
   └─ CommitTransitionAsync() でDB保存
4. OnCaptureRequested コールバック
   └─ キャプチャ取得ダイアログ表示
```

---

## 5. 問題点と影響度

### 5.1 God Class問題（CheckItemUIBuilder）

**影響度**: ★★★★★ 高

| 問題 | 影響 |
|------|------|
| 450行超の大規模クラス | 可読性低下、変更困難 |
| UI構築とロジックが混在 | 単体テスト困難 |
| MainWindow/ChecklistWindow分岐 | 条件分岐の複雑化 |
| コールバック注入が必要 | 依存関係の複雑化 |

**推奨対応**:
- UI構築とViewModel構築を分離
- コマンド設定をWindow側に移動
- Factory/Strategyパターンの検討

### 5.2 File.Exists()問題（CameraButtonVisibility）

**影響度**: ★★★☆☆ 中

```csharp
// 現在のコード
public Visibility CameraButtonVisibility
{
    get
    {
        var absolutePath = GetCaptureAbsolutePath();
        if (absolutePath == null || !File.Exists(absolutePath))  // ★I/O
            return Visibility.Collapsed;
    }
}
```

| 問題 | 影響 |
|------|------|
| getter内でファイルI/O | パフォーマンス低下 |
| ファイルシステム依存 | 単体テスト困難 |
| UI更新のたびに実行 | 不要なI/O発生 |

**推奨対応**:
- ファイル存在チェックをViewModel初期化時に実行
- 結果をプロパティとしてキャッシュ
- IFileSystemAbstraction注入でテスト容易化

### 5.3 コマンド設定の複雑さ

**影響度**: ★★★☆☆ 中

| 問題 | 影響 |
|------|------|
| SetupCommands()内の条件分岐 | 責務の混在 |
| コールバック方式 | 依存関係の不透明さ |
| Task.Run().Wait()パターン | UIスレッドブロック |

**推奨対応**:
- コマンド設定をWindow側で行う
- DIコンテナでサービス注入
- async/awaitの統一

### 5.4 ドキュメントと実装の乖離

**影響度**: ★★☆☆☆ 低（今回アーカイブ済み）

**状況**:
- 旧ドキュメント: `docs/behaviors/checklist-refactoring/`
- 11チケットすべて「Done」だったが、実際は未完了機能あり
- → `docs/archive/agent-development/2025-11/` にアーカイブ済み

---

## 6. 推奨リファクタリング方針（概要）

### Phase 1: 責務分離
1. CheckItemUIBuilder の分割
   - `CheckItemViewModelBuilder`: ViewModel構築専用
   - `CheckItemUIFactory`: UI要素生成専用
2. コマンド設定をWindow側に移動

### Phase 2: テスト容易性の向上
1. IFileSystemAbstraction 導入
2. CameraButtonVisibility のロジック簡素化
3. 単体テストの追加

### Phase 3: 状態ベース管理への移行（オプション）
1. ViewModel.State プロパティ導入
2. 状態からすべてのUI属性を派生
3. CheckItemTransition との統合

---

## 7. 関連ファイル一覧

### コアファイル

| ファイル | パス | 行数 | 評価 |
|---------|------|------|------|
| CheckItemViewModel | `ViewModels/CheckItemViewModel.cs` | ~230 | ★★★★☆ |
| CheckItemTransition | `Models/CheckItemTransition.cs` | ~150 | ★★★★★ |
| ChecklistStateManager | `Services/ChecklistStateManager.cs` | ~200 | ★★★★★ |
| CheckItemUIBuilder | `Helpers/CheckItemUIBuilder.cs` | ~450 | ★★☆☆☆ |
| CheckBoxBehaviors | `Behaviors/CheckBoxBehaviors.cs` | ~170 | ★★★★★ |

### ビューファイル

| ファイル | パス | 用途 |
|---------|------|------|
| MainWindow.xaml | `Windows/MainWindow.xaml` | 読み取り専用画面 |
| MainWindow.xaml.cs | `Windows/MainWindow.xaml.cs` | コードビハインド |
| ChecklistWindow.xaml | `Windows/ChecklistWindow.xaml` | 編集可能画面 |
| ChecklistWindow.xaml.cs | `Windows/ChecklistWindow.xaml.cs` | コードビハインド |

### エンティティ・リポジトリ

| ファイル | パス | 用途 |
|---------|------|------|
| CheckItem | `Entities/CheckItem.cs` | チェック項目エンティティ |
| CheckItemDocument | `Entities/CheckItemDocument.cs` | 紐づけエンティティ |
| ICheckItemRepository | `Infrastructure/Repositories/ICheckItemRepository.cs` | リポジトリインターフェース |

---

## 8. 付録: 良い点（維持すべき設計）

| 項目 | 評価 | 説明 |
|------|------|------|
| MVVM準拠度 | ★★★★★ | INotifyPropertyChanged完全実装 |
| DataTemplate | ★★★★★ | 動的取得・適用が正しく動作 |
| 状態遷移 | ★★★★★ | CheckItemTransitionで一元管理 |
| Attached Behavior | ★★★★★ | メモリリーク対策完備 |
| 責務分離 | ★★★★☆ | MainWindow/ChecklistWindowで明確に分岐 |
| 二重管理なし | ★★★★★ | DBが唯一の真実の光源 |

これらの良い設計はリファクタリング後も維持すべきです。
