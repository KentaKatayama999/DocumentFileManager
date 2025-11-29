# CheckItemUIBuilderリファクタリング 実装記録

## 概要

CheckItemUIBuilderクラスの単一責任原則違反を解消し、MVVMパターンに準拠した設計に再構成しました。

## 実装期間

- 開始日: 2025-11-28
- 完了日: 2025-11-28

## 変更サマリー

### 新規作成クラス

1. **IDialogService** (`Services/Abstractions/IDialogService.cs`)
   - MessageBoxを抽象化
   - テスト可能なダイアログ表示
   - メソッド: ShowConfirmationAsync, ShowYesNoCancelAsync, ShowInformationAsync, ShowErrorAsync

2. **WpfDialogService** (`Services/WpfDialogService.cs`)
   - IDialogServiceの実装クラス
   - Dispatcher.InvokeAsyncを使用してUIスレッド対応

3. **IChecklistStateManager** (`Services/Abstractions/IChecklistStateManager.cs`)
   - 状態遷移ロジックのインターフェース
   - HandleCheckOnAsync, HandleCheckOffAsync, CommitCaptureAsync, CommitTransitionAsync, RollbackTransitionAsync, CreateTransitionAsync

4. **ChecklistStateManager** (`Services/ChecklistStateManager.cs`)
   - IChecklistStateManagerの実装クラス
   - DB操作と状態遷移ロジックを一元管理

5. **CheckItemTransition** (`Models/CheckItemTransition.cs`)
   - 状態遷移を表現するモデルクラス
   - 状態コード: 00, 10, 11, 20, 22

### 拡張クラス

1. **CheckItemViewModel** (`ViewModels/CheckItemViewModel.cs`)
   - IsCheckBoxEnabledプロパティ追加（MainWindowでは無効）
   - CameraButtonVisibilityプロパティ追加（キャプチャ有無で表示切替）
   - CheckedChangedCommandプロパティ追加（ICommand）
   - ViewCaptureCommandプロパティ追加（ICommand）
   - UpdateCaptureButton()メソッド追加
   - 拡張コンストラクタ追加（documentRootPath, isMainWindow）

### リファクタリングクラス

1. **CheckItemUIBuilder** (`Helpers/CheckItemUIBuilder.cs`)
   - IChecklistStateManagerの依存注入追加
   - SetupCommands()メソッド追加（AsyncRelayCommand/RelayCommand設定）
   - CreateCheckBox()をバインディングベースに変更
   - SaveStatusAsync()メソッド削除
   - 直接イベントハンドラ削除

2. **ChecklistWindow** (`Windows/ChecklistWindow.xaml.cs`)
   - PerformCaptureForCheckItem()からUI直接更新コードを削除
   - ViewModelのバインディングでUI自動更新に移行

### XAML変更

1. **ChecklistWindow.xaml**
   - CheckItemTemplate DataTemplate追加
   - CheckItemCheckBoxStyle追加
   - CameraButtonStyle追加

2. **MainWindow.xaml**
   - 同様のDataTemplateとスタイル追加

## 責務分離の結果

### CheckItemUIBuilder（変更前）

| 責務 | 状態 |
|------|------|
| UI構築 | 維持 |
| イベント処理 | ChecklistStateManagerへ移動 |
| DB操作 | ChecklistStateManagerへ移動 |
| 状態管理 | CheckItemViewModelへ移動 |

### CheckItemUIBuilder（変更後）

| 責務 | 状態 |
|------|------|
| UI構築 | 維持 |
| バインディング設定 | 新規追加 |
| コマンド設定 | 新規追加 |

### ChecklistStateManager（新規）

| 責務 | 状態 |
|------|------|
| イベント処理 | 担当 |
| DB操作 | 担当 |
| 状態遷移ロジック | 担当 |
| ダイアログ表示 | IDialogService経由 |

### CheckItemViewModel（拡張）

| 責務 | 状態 |
|------|------|
| 状態管理 | 担当 |
| INotifyPropertyChanged | 維持 |
| ICommand | 新規追加 |
| UI表示状態 | 新規追加 |

## 状態遷移

```
状態00 (未紐づけ)
    ↓ チェックON
状態10 (チェックON、キャプチャなし)
    ↓ キャプチャ保存
状態11 (チェックON、キャプチャあり)
    ↓ チェックOFF
状態22 (チェックOFF、キャプチャ維持)
    ↓ チェックON（復帰）
状態11 (チェックON、キャプチャあり)
```

## テスト結果

### 単体テスト

| テストクラス | 件数 | 結果 |
|-------------|------|------|
| DialogServiceTests | 7 | 成功 |
| ChecklistStateManagerTests | 10 | 成功 |
| CheckItemViewModelTests | 19 | 成功 |
| CheckItemUIBuilderTests | 10 | 成功 |
| 他 | 96 | 成功 |
| **合計** | **142** | **成功** |

### 統合テスト（手動確認）

| シナリオ | 状態 |
|---------|------|
| チェックON → キャプチャ取得 → 正常保存 | 要確認 |
| チェックON → キャプチャなし | 要確認 |
| 既存キャプチャの復帰 | 要確認 |
| チェックOFF → キャプチャ維持 | 要確認 |
| MainWindowでチェックボックス無効化 | 要確認 |
| カメラボタンクリックで画像表示 | 要確認 |
| async/await競合の解消 | 要確認 |
| 既存キャプチャの破棄 | 要確認 |
| 既存キャプチャ復帰のキャンセル | 要確認 |

## 依存関係

```
ChecklistWindow
    ├── CheckItemUIBuilder
    │       ├── IChecklistStateManager
    │       │       ├── ICheckItemDocumentRepository
    │       │       └── IDialogService
    │       └── ILogger
    └── CheckItemViewModel
            ├── ICommand (CheckedChangedCommand)
            └── ICommand (ViewCaptureCommand)
```

## DIコンテナ登録

```csharp
services.AddSingleton<IDialogService, WpfDialogService>();
services.AddScoped<IChecklistStateManager, ChecklistStateManager>();
services.AddScoped<CheckItemUIBuilder>();
```

## 学んだこと

1. **IDialogServiceの重要性**: MessageBox抽象化により、テスト容易性が大幅に向上
2. **CommunityToolkit.Mvvm**: AsyncRelayCommand/RelayCommandの実装が簡潔で保守性が高い
3. **DataTemplate**: XAMLでUI定義することで、デザイナーとの分業が可能に
4. **状態遷移の明確化**: CheckItemTransitionにより、ロールバックが容易に
5. **TDDアプローチ**: Red-Green-Refactorサイクルで品質を担保

## 改善の余地

1. **ItemsControl導入**: GroupBoxのChildren手動管理を自動化
2. **VirtualizingStackPanel**: 大量チェック項目のパフォーマンス向上
3. **ファイル存在確認のキャッシュ**: CameraButtonVisibilityの性能改善
4. **完全なDataTemplate化**: 現在はハイブリッド（コード生成 + DataTemplate）

## 参考資料

- [docs/behaviors/checklist-refactoring/plan.md](plan.md) - 実装プラン
- [CommunityToolkit.Mvvm公式ドキュメント](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- WPF MVVMパターンベストプラクティス
