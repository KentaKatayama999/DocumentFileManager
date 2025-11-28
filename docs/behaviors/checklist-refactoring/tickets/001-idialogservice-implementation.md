# チケット #001: IDialogService実装（TDD）

## 基本情報

- **ステータス**: Done
- **優先度**: High
- **見積もり**: 2時間
- **作成日**: 2025-11-28
- **更新日**: 2025-11-28
- **依存チケット**: なし
- **タグ**: infrastructure, tdd, dialog

## 概要

MessageBoxを抽象化し、テスト可能なIDialogServiceインターフェースとその実装（WpfDialogService）を作成します。これにより、後続のChecklistStateManagerで依存注入を使用したテスト可能な設計が実現できます。

## 実装内容

### 1. CommunityToolkit.Mvvmパッケージのインストール

```bash
dotnet add src/DocumentFileManager.UI package CommunityToolkit.Mvvm
```

### 2. IDialogService.cs インターフェース作成

**ファイル**: `src/DocumentFileManager.UI/Services/Abstractions/IDialogService.cs`

**メソッド**:
- `Task<bool> ShowConfirmationAsync(string message, string title)` - 確認ダイアログ（はい/いいえ）
- `Task<DialogResult> ShowYesNoCancelAsync(string message, string title)` - 3択ダイアログ（はい/いいえ/キャンセル）
- `Task ShowInformationAsync(string message, string title)` - 情報ダイアログ
- `Task ShowErrorAsync(string message, string title)` - エラーダイアログ

**enum定義**:
```csharp
public enum DialogResult
{
    Yes,
    No,
    Cancel
}
```

### 3. WpfDialogService.cs 実装クラス作成

**ファイル**: `src/DocumentFileManager.UI/Services/WpfDialogService.cs`

**実装要件**:
- MessageBox.Showのラッパーとして実装
- Dispatcher対応（UIスレッドで実行保証）
- 各メソッドで適切なMessageBoxButton、MessageBoxImageを使用

### 4. WpfDialogServiceTests.cs テストクラス作成

**ファイル**: `src/DocumentFileManager.UI.Tests/Services/WpfDialogServiceTests.cs`

**テストケース**:
- `ShowConfirmationAsync_ユーザーがはいを選択_trueを返す`
- `ShowYesNoCancelAsync_ユーザーがキャンセルを選択_Cancelを返す`

**注意**: MessageBoxをモック化する必要があるため、IDialogServiceのモック実装を使用

### 5. AppInitializer.cs にIDialogServiceを登録

```csharp
services.AddSingleton<IDialogService, WpfDialogService>();
```

## 完了条件（チェックリスト）

- [x] CommunityToolkit.Mvvmパッケージがインストールされている
- [x] IDialogService.csインターフェースが作成されている
- [x] DialogResult enumが定義されている
- [x] WpfDialogService.csが実装されている
- [x] ShowConfirmationAsyncが正しく動作する
- [x] ShowYesNoCancelAsyncが正しく動作する
- [x] ShowInformationAsyncが正しく動作する
- [x] ShowErrorAsyncが正しく動作する
- [x] Dispatcher対応が実装されている
- [x] WpfDialogServiceTests.csが作成されている
- [x] テストケースが2つ以上作成されている（7件）
- [x] AppInitializer.csにサービス登録が追加されている
- [x] ビルドが成功する
- [x] すべてのテストがパスする（123件）

## 技術メモ

### Dispatcher対応の実装例

```csharp
public async Task<bool> ShowConfirmationAsync(string message, string title)
{
    return await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return result == MessageBoxResult.Yes;
    });
}
```

### テスト時のモック化

実際のMessageBoxを表示するとテストが対話的になるため、テストではIDialogServiceのモック実装を使用します。

```csharp
var mockDialogService = new Mock<IDialogService>();
mockDialogService.Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
    .ReturnsAsync(true);
```

## 関連ドキュメント

- `docs/behaviors/checklist-refactoring/plan.md` - Phase 1
- `src/DocumentFileManager.UI/Services/` - サービス配置先
