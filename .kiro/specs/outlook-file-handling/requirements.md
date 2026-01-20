# Requirements: outlook-file-handling

> Project Description: outlookのファイルを問題なく開くようにする

## Status
- **Phase**: Requirements (Generated)
- **Created**: 2026-01-20T00:00:00Z
- **Updated**: 2026-01-20T00:00:00Z

---

## 背景

DocumentFileManagerでは、Outlookメールファイル（.msg, .eml）を外部プログラム（Outlook）で開く機能が実装されている。しかし、以下の**クリティカルな問題**が存在する：

### 発見された問題（バグ）

**LoadingWindowが閉じない問題**:
- `OpenEmailFile`メソッド（ViewerWindow.xaml.cs:506）は同期的に実行される
- `ExternalWindowReady`イベントが発火されないため、LoadingWindowが閉じない
- 結果として「Microsoft Outlookを起動しています...」ダイアログがアプリ終了まで表示され続ける
- このダイアログがChecklistWindow（登録フォーム）の表示を阻害している

**原因コード箇所**:
- `ViewerWindow.xaml.cs:336` - `OpenEmailFile`を呼び出すが、`ExternalWindowReady`イベントを発火しない
- `MainWindow.xaml.cs:816` - `ExternalWindowReady`イベントでLoadingWindowを閉じる設計だが、イベントが来ない

---

## Requirements

### 1. LoadingWindowが正しく閉じる（必須・最優先）

**概要**: Outlookファイルを開いた後、LoadingWindowを確実に閉じる

#### 1.1 ExternalWindowReadyイベントの発火
- **EARS**: システムは、Outlookでメールファイルを開いた後、ExternalWindowReadyイベントを発火しなければならない
- **現状**: `OpenEmailFile`メソッドがイベントを発火していない
- **修正方針**: `OpenEmailFile`の終了時に`ExternalWindowReady`イベントを発火する

#### 1.2 タイムアウト時のフォールバック
- **EARS**: Outlookウィンドウの検出がタイムアウトした場合でも、システムはExternalWindowReadyイベントを発火しなければならない
- **目的**: タイムアウト時もLoadingWindowが閉じるようにする

### 2. ChecklistWindowの正常表示（必須）

**概要**: Outlookファイルを開いた後、ChecklistWindow（登録フォーム）が正常に表示される

#### 2.1 モーダルダイアログの解消
- **EARS**: システムは、外部プログラムを起動した後、ChecklistWindowを表示できる状態にならなければならない
- **現状**: LoadingWindowがブロックしているためChecklistWindowが開けない

### 3. エラーハンドリング（必須）

**概要**: ファイルを開く際のエラーを適切に処理する

#### 3.1 Outlook未インストール時の対応
- **EARS**: Outlookがインストールされていない環境で.msgファイルを開こうとした場合、システムはユーザーに適切なエラーメッセージを表示し、LoadingWindowを閉じなければならない

#### 3.2 ファイル破損時の対応
- **EARS**: 破損したメールファイルを開こうとした場合、システムはユーザーに「ファイルを開けません」というエラーメッセージを表示し、LoadingWindowを閉じなければならない

---

## Acceptance Criteria

### AC1: LoadingWindowの正常終了（必須・最優先）
- [ ] .msgファイルを開くとLoadingWindowが表示される
- [ ] Outlookが起動した後、LoadingWindowが自動的に閉じる
- [ ] タイムアウト時もLoadingWindowが閉じる
- [ ] ChecklistWindow（登録フォーム）が正常に表示される

### AC2: エラーハンドリング（必須）
- [ ] Outlook未インストール時に適切なエラーメッセージが表示され、LoadingWindowが閉じる
- [ ] ファイル破損時に適切なエラーメッセージが表示され、LoadingWindowが閉じる

### AC3: ウィンドウ配置（既存機能の維持）
- [ ] 開いたOutlookウィンドウが画面左2/3に自動配置される

---

## 技術的考慮事項

### 修正対象ファイル
1. `src/DocumentFileManager.Viewer/ViewerWindow.xaml.cs`
   - `OpenEmailFile`メソッド（506行目）
   - `ExternalWindowReady`イベントを発火するように修正

### 修正方針（案）

**Option A: OpenEmailFileをasync化してイベント発火を追加**
```csharp
private async Task<IntPtr> OpenEmailFileAsync(string filePath)
{
    // ... 既存処理 ...
    var handle = await Task.Run(() => WaitForNewOutlookWindow(...));

    // イベントを発火
    Dispatcher.Invoke(() => ExternalWindowReady?.Invoke(this, handle));
    return handle;
}
```

**Option B: OpenWithDefaultProgramの統一処理に組み込む**
- メールファイルも通常ファイルと同じポーリング処理を使用
- 既存の`ExternalWindowReady`イベント発火ロジックを活用

---

## 優先順位

| 要件 | 優先度 | 理由 |
|------|--------|------|
| 1.1 ExternalWindowReadyイベント発火 | 最高 | バグ修正、現在の主要問題 |
| 1.2 タイムアウト時フォールバック | 高 | 安定性確保 |
| 2.1 ChecklistWindow表示 | 高 | UX改善（上記修正で自動解決） |
| 3. エラーハンドリング | 中 | 堅牢性向上 |

---

## スコープ外

- 内部メールビューア機能（MsgReader/MimeKit導入）
- メールの編集・返信機能
- メールの検索機能
- カレンダー/連絡先ファイル（.ics, .vcf）の対応
