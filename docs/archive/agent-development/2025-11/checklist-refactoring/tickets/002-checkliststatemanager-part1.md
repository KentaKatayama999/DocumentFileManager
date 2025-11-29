# チケット #002: ChecklistStateManager実装（Part 1: テストケース作成）

## 基本情報

- **ステータス**: Done
- **優先度**: High
- **見積もり**: 2時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: #001
- **タグ**: tdd, state-management, testing

## 概要

ChecklistStateManagerのテストケースを先行して作成します（TDDアプローチ）。これにより、実装すべき仕様が明確になり、後続の実装作業が容易になります。

## 実装内容

### 1. ChecklistStateManagerTests.cs テストクラス作成

**ファイル**: `src/DocumentFileManager.UI.Tests/Services/ChecklistStateManagerTests.cs`

### 2. テストケース一覧

#### 2.1 チェックON時のテスト

- **`HandleCheckOnAsync_未紐づけ_キャプチャなし_状態10へ遷移`**
  - Given: 状態00（未紐づけ）
  - When: チェックON、キャプチャ確認で「いいえ」
  - Then: 状態10へ遷移（IsChecked=true, CaptureFile=null）

- **`HandleCheckOnAsync_既存キャプチャあり_復帰選択_状態11へ遷移`**
  - Given: 状態22（キャプチャあり、チェックOFF）
  - When: チェックON、復帰確認で「はい」
  - Then: 状態11へ遷移（IsChecked=true, CaptureFile維持）

- **`HandleCheckOnAsync_既存キャプチャあり_破棄選択_状態10へ遷移`**
  - Given: 状態22（キャプチャあり、チェックOFF）
  - When: チェックON、復帰確認で「いいえ」
  - Then: 状態10へ遷移（IsChecked=true, CaptureFile=null）

- **`HandleCheckOnAsync_既存キャプチャあり_キャンセル_ロールバック`**
  - Given: 状態22（キャプチャあり、チェックOFF）
  - When: チェックON、復帰確認で「キャンセル」
  - Then: 状態22に維持（ロールバック）

#### 2.2 チェックOFF時のテスト

- **`HandleCheckOffAsync_キャプチャあり_状態22へ遷移`**
  - Given: 状態11（チェックON、キャプチャあり）
  - When: チェックOFF
  - Then: 状態22へ遷移（IsChecked=false, CaptureFile維持）

- **`HandleCheckOffAsync_キャプチャなし_状態20へ遷移`**
  - Given: 状態10（チェックON、キャプチャなし）
  - When: チェックOFF
  - Then: 状態20へ遷移（IsChecked=false, CaptureFile=null）

#### 2.3 キャプチャ保存のテスト

- **`CommitCaptureAsync_新規キャプチャ保存_状態11へ遷移`**
  - Given: 状態10（チェックON、キャプチャなし）
  - When: キャプチャ保存
  - Then: 状態11へ遷移（CaptureFile設定）

#### 2.4 DB操作のテスト

- **`CommitTransitionAsync_新規レコード作成`**
  - Given: CheckItemDocumentレコードが存在しない
  - When: CommitTransitionAsync実行
  - Then: 新規レコードが作成される

- **`CommitTransitionAsync_既存レコード更新`**
  - Given: CheckItemDocumentレコードが存在する
  - When: CommitTransitionAsync実行
  - Then: 既存レコードが更新される

#### 2.5 ロールバックのテスト

- **`RollbackTransitionAsync_元の状態に復元`**
  - Given: 状態遷移が発生している（CheckItemTransition.HasChanges = true）
  - When: RollbackTransitionAsync実行
  - Then: 元の状態に復元される

### 3. モック対象

- **ICheckItemDocumentRepository** - DB操作のモック
- **IDialogService** - ダイアログ表示のモック
- **ILogger<ChecklistStateManager>** - ログ出力のモック

### 4. テストデータ準備

各テストケースで使用するViewModelとDocumentのテストデータを用意します。

```csharp
private CheckItemViewModel CreateTestViewModel(bool isChecked = false, string captureFilePath = null)
{
    return new CheckItemViewModel(
        new CheckItemEntity { Id = 1, Label = "テスト項目" },
        documentRootPath: "C:\\TestDocuments",
        isMainWindow: false
    )
    {
        IsChecked = isChecked,
        CaptureFilePath = captureFilePath
    };
}

private Document CreateTestDocument()
{
    return new Document { Id = 1, Title = "テストドキュメント" };
}
```

## 完了条件（チェックリスト）

- [x] ChecklistStateManagerTests.csファイルが作成されている
- [x] チェックON時のテストケース4つが作成されている
- [x] チェックOFF時のテストケース2つが作成されている
- [x] キャプチャ保存のテストケース1つが作成されている
- [x] DB操作のテストケース2つが作成されている
- [x] ロールバックのテストケース1つが作成されている
- [x] 合計10個のテストケースが作成されている
- [x] ICheckItemDocumentRepositoryのモックが設定されている
- [x] IDialogServiceのモックが設定されている
- [x] ILoggerのモックが設定されている
- [x] テストデータ作成用のヘルパーメソッドが実装されている
- [x] ビルドが成功する（実装がないためテストは失敗する）

## 技術メモ

### Moqを使用したモック例

```csharp
[Fact]
public async Task HandleCheckOnAsync_未紐づけ_キャプチャなし_状態10へ遷移()
{
    // Arrange
    var mockRepository = new Mock<ICheckItemDocumentRepository>();
    var mockDialogService = new Mock<IDialogService>();
    mockDialogService.Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(false); // キャプチャ確認で「いいえ」

    var mockLogger = new Mock<ILogger<ChecklistStateManager>>();

    var stateManager = new ChecklistStateManager(
        mockRepository.Object,
        mockDialogService.Object,
        mockLogger.Object
    );

    var viewModel = CreateTestViewModel();
    var document = CreateTestDocument();

    // Act
    var transition = await stateManager.HandleCheckOnAsync(viewModel, document);

    // Assert
    Assert.True(transition.NewIsChecked);
    Assert.Null(transition.NewCaptureFilePath);
    Assert.True(transition.HasChanges);
}
```

### 状態コード定義（参考）

- **状態00**: 未紐づけ（IsChecked=false, CaptureFile=null, レコードなし）
- **状態10**: チェックON、キャプチャなし（IsChecked=true, CaptureFile=null）
- **状態11**: チェックON、キャプチャあり（IsChecked=true, CaptureFile設定）
- **状態20**: チェックOFF、履歴のみ（IsChecked=false, CaptureFile=null）
- **状態22**: チェックOFF、キャプチャ維持（IsChecked=false, CaptureFile設定）

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 2
- `src/DocumentFileManager.UI.Tests/Services/` - テスト配置先
