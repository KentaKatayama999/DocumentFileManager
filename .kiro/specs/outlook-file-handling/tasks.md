# Implementation Tasks: outlook-file-handling

## Status
- **Phase**: Tasks (Generated)
- **Created**: 2026-01-20
- **Updated**: 2026-01-20

---

## Requirements Coverage

| 要件ID | タスク |
|--------|--------|
| 1.1 | Task 1 |
| 1.2 | 既存実装で対応済み |
| 2.1 | Task 1で自動解決 |
| 3.1 | 既存実装で対応済み |
| 3.2 | 既存実装で対応済み |

---

## Tasks

### Task 1: ExternalWindowReadyイベント発火の追加

**要件**: 1.1, 2.1

**概要**: メールファイルを開いた後にExternalWindowReadyイベントを発火し、LoadingWindowが閉じるようにする

**対象ファイル**: `src/DocumentFileManager.Viewer/ViewerWindow.xaml.cs`

#### 1.1 OpenWithDefaultProgramメソッドにイベント発火を追加
- [x] `OpenWithDefaultProgram`メソッド内の`OpenEmailFile`呼び出し後に`ExternalWindowReady`イベントを発火する
- [x] `Dispatcher.Invoke`を使用してUIスレッドでイベントを発火する
- [x] 修正箇所: 334-338行目付近のメールファイル処理分岐

**修正内容**:
```csharp
// メールファイルの場合は専用処理
if (extension is ".msg" or ".eml")
{
    _externalWindowHandle = OpenEmailFile(filePath);

    // ExternalWindowReadyイベントを発火（LoadingWindow閉じるため）
    Dispatcher.Invoke(() => ExternalWindowReady?.Invoke(this, _externalWindowHandle));

    return _externalWindowHandle;
}
```

---

### Task 2: 動作確認テスト

**要件**: 1.1, 1.2, 2.1, 3.1, 3.2

**概要**: 修正後の動作を手動テストで確認する

#### 2.1 正常系テスト
- [x] .msgファイルを開き、LoadingWindowが表示後に自動的に閉じることを確認
- [x] Outlookが起動し、ウィンドウが画面左2/3に配置されることを確認
- [x] ChecklistWindow（登録フォーム）が正常に表示されることを確認

#### 2.2 異常系テスト（オプション）
- [ ]* Outlookウィンドウ検出タイムアウト時にLoadingWindowが閉じることを確認
- [ ]* エラー発生時にLoadingWindowが閉じることを確認

---

## Task Dependencies

```
Task 1 (実装) → Task 2 (テスト)
```

---

## Estimated Effort

| タスク | 見積もり |
|--------|---------|
| Task 1.1 | 15分 |
| Task 2.1 | 15分 |
| Task 2.2 | 10分（オプション） |
| **合計** | **30-40分** |
