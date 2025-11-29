# チケット #004: CheckItemViewModel拡張（Part 1: テストケース作成）

## 基本情報

- **ステータス**: Done
- **優先度**: High
- **見積もり**: 1時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: #003
- **タグ**: tdd, viewmodel, testing

## 概要

CheckItemViewModelのテストケースを先行して作成します（TDDアプローチ）。既存のViewModelにICommand、プロパティを追加するため、既存機能の保持とINotifyPropertyChangedの正しい発火を確認します。

## 実装内容

### 1. CheckItemViewModelTests.cs テストクラス作成

**ファイル**: `src/DocumentFileManager.UI.Tests/ViewModels/CheckItemViewModelTests.cs`

### 2. テストケース一覧

#### 2.1 INotifyPropertyChanged発火確認

- **`IsChecked_変更時_PropertyChangedイベント発火`**
  - Given: CheckItemViewModel作成
  - When: IsCheckedを変更
  - Then: PropertyChangedイベントが発火（プロパティ名: "IsChecked"）

- **`CaptureFilePath_変更時_PropertyChangedとHasCaptureイベント発火`**
  - Given: CheckItemViewModel作成
  - When: CaptureFilePathを変更
  - Then: PropertyChangedイベントが2回発火（"CaptureFilePath", "HasCapture"）

#### 2.2 CameraButtonVisibility テスト

- **`CameraButtonVisibility_HasCaptureとファイル存在で表示`**
  - Given: CaptureFilePathが設定されている、ファイルが存在する
  - When: CameraButtonVisibilityを取得
  - Then: Visibility.Visible

- **`CameraButtonVisibility_HasCapture=falseで非表示`**
  - Given: CaptureFilePathがnull
  - When: CameraButtonVisibilityを取得
  - Then: Visibility.Collapsed

- **`CameraButtonVisibility_ファイル存在しない場合は非表示`**
  - Given: CaptureFilePathが設定されているが、ファイルが存在しない
  - When: CameraButtonVisibilityを取得
  - Then: Visibility.Collapsed

#### 2.3 IsCheckBoxEnabled テスト

- **`IsCheckBoxEnabled_MainWindowモードでfalse`**
  - Given: CheckItemViewModel作成（isMainWindow: true）
  - When: IsCheckBoxEnabledを取得
  - Then: false

- **`IsCheckBoxEnabled_ChecklistWindowモードでtrue`**
  - Given: CheckItemViewModel作成（isMainWindow: false）
  - When: IsCheckBoxEnabledを取得
  - Then: true

#### 2.4 IsCheckedの初期化ロジックテスト

- **`IsChecked_MainWindow_最新キャプチャあり_trueで初期化`**
  - Given: isMainWindow: true、最新キャプチャあり
  - When: CheckItemViewModel作成
  - Then: IsChecked = true、IsCheckBoxEnabled = false

- **`IsChecked_ChecklistWindow_CheckItemDocumentから復元`**
  - Given: isMainWindow: false、CheckItemDocument.IsChecked = true
  - When: CheckItemViewModel作成
  - Then: IsChecked = true、IsCheckBoxEnabled = true

- **`IsChecked_StatusがDone_trueで初期化`**
  - Given: Entity.Status == CheckItemStatus.Done
  - When: CheckItemViewModel作成
  - Then: IsChecked = true

### 3. テストデータ準備

```csharp
private CheckItemEntity CreateTestEntity(CheckItemStatus status = CheckItemStatus.Pending)
{
    return new CheckItemEntity
    {
        Id = 1,
        Label = "テスト項目",
        Status = status
    };
}

private string CreateTempCaptureFile()
{
    var tempPath = Path.Combine(Path.GetTempPath(), "test_capture.png");
    File.WriteAllText(tempPath, "test");
    return tempPath;
}
```

### 4. モック対象

- ファイルシステムのモック化（File.Existsの挙動制御）

## 完了条件（チェックリスト）

- [ ] CheckItemViewModelTests.csファイルが作成されている
- [ ] INotifyPropertyChanged発火確認テスト2つが作成されている
- [ ] CameraButtonVisibilityテスト3つが作成されている
- [ ] IsCheckBoxEnabledテスト2つが作成されている
- [ ] IsChecked初期化ロジックテスト3つが作成されている
- [ ] 合計10個のテストケースが作成されている
- [ ] テストデータ作成用のヘルパーメソッドが実装されている
- [ ] 一時ファイル作成・削除のクリーンアップ処理が実装されている
- [ ] ビルドが成功する（実装がないためテストは失敗する）

## 技術メモ

### PropertyChangedイベントの検証例

```csharp
[Fact]
public void IsChecked_変更時_PropertyChangedイベント発火()
{
    // Arrange
    var entity = CreateTestEntity();
    var viewModel = new CheckItemViewModel(entity, "C:\\TestDocuments", false);

    var propertyChangedRaised = false;
    var propertyName = string.Empty;

    viewModel.PropertyChanged += (sender, e) =>
    {
        propertyChangedRaised = true;
        propertyName = e.PropertyName;
    };

    // Act
    viewModel.IsChecked = true;

    // Assert
    Assert.True(propertyChangedRaised);
    Assert.Equal("IsChecked", propertyName);
}
```

### ファイルシステムのモック化

```csharp
// File.Existsをモック化する場合、System.IO.Abstractions パッケージを使用
// または、テスト用に実際の一時ファイルを作成・削除する
```

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 3
- `src/DocumentFileManager.UI.Tests/ViewModels/` - テスト配置先
- `src/DocumentFileManager.UI/ViewModels/CheckItemViewModel.cs` - 拡張対象
