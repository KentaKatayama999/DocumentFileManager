# Research Log: outlook-file-handling

## Summary

**Discovery Type**: Minimal（バグ修正のため）

**調査スコープ**: 既存コードのイベント発火パターン分析

**調査日**: 2026-01-20

---

## Research Log

### Topic 1: ExternalWindowReadyイベントの発火パターン

**調査内容**: ViewerWindow.xaml.cs内でのExternalWindowReadyイベント発火箇所

**発見事項**:

| 発火箇所 | 行番号 | トリガー条件 |
|---------|--------|-------------|
| OpenWithDefaultProgram | 408-409 | ウィンドウ検出成功時 |
| OpenWithDefaultProgram | 436-437 | タイムアウト時（handle=Zero） |

**問題**: `OpenEmailFile`（506行目）は独立した処理で、イベントを発火しない

**含意**: メール処理を統一するか、個別にイベント発火を追加する必要がある

---

### Topic 2: OpenEmailFileの呼び出しフロー

**調査内容**: メールファイルを開く際の処理フロー

**発見事項**:

```
CreateEmailViewer (307行目)
    ↓ MessageBox表示後
_ = OpenWithDefaultProgram(filePath) (312行目) ← fire-and-forget
    ↓ OpenWithDefaultProgram内 (334-338行目)
if (extension is ".msg" or ".eml")
    _externalWindowHandle = OpenEmailFile(filePath);  ← 同期呼び出し
    return _externalWindowHandle;  ← イベント発火なしで即return
```

**問題点**:
1. `OpenEmailFile`が同期的に実行される（UIスレッドをブロック）
2. `OpenWithDefaultProgram`の通常フロー（非同期ポーリング）がスキップされる
3. 結果として`ExternalWindowReady`イベントが発火されない

---

### Topic 3: 修正アプローチの比較

| アプローチ | メリット | デメリット |
|-----------|---------|-----------|
| **A: OpenEmailFile終了後にイベント発火** | 最小限の変更、影響範囲が狭い | メール処理の特殊ケースを維持 |
| **B: 汎用処理に統一** | コードの一貫性、既存ロジック再利用 | 変更範囲が広い、テスト工数増 |

**推奨**: Option A（最小限の変更）

**理由**:
- バグ修正として最も直接的
- 既存の動作（ウィンドウ検出、配置）への影響なし
- リグレッションリスクが低い

---

## Architecture Pattern Evaluation

該当なし（バグ修正のため新規アーキテクチャ検討不要）

---

## Design Decisions

### DD-1: OpenEmailFile呼び出し後のイベント発火追加

**決定**: `OpenWithDefaultProgram`内で`OpenEmailFile`呼び出し後に`ExternalWindowReady`イベントを発火

**理由**:
- 変更箇所が1箇所のみ
- 既存のDispatcher.Invokeパターンを踏襲
- タイムアウト時もイベント発火される（WaitForNewOutlookWindow内で対応済み）

---

## Risks & Mitigations

### Risk 1: WaitForNewOutlookWindowがUIスレッドをブロック

**影響**: Thread.Sleepを使用しているため、30秒間UIが固まる可能性

**緩和策**:
- 現状維持（短期的修正）
- 将来的にasync化を検討（スコープ外）

### Risk 2: 既存ウィンドウへのフォールバック時の挙動

**影響**: タイムアウト時に既存Outlookウィンドウが選択される可能性

**緩和策**: 既存動作を維持（変更なし）
