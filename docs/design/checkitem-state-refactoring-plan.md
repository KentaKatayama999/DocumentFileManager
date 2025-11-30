# CheckItem状態ベースリファクタリング計画

## 目的
CheckItemUIBuilderのGod Class問題を解消し、状態ベースの管理でコードをシンプル化する。

---

## 1. 状態パラメータ

| パラメータ | 型 | 値 | 説明 |
|-----------|----|----|------|
| WindowMode | enum | 0=MainWindow, 1=ChecklistWindow | ウィンドウ種別 |
| ItemState | string | 00/10/11/20/22 | チェック×キャプチャ状態 |
| CaptureFileExists | bool | true/false | ファイル実在確認 |

### ItemState状態コード
```
00 = 未紐づけ
10 = チェックON、キャプチャなし
11 = チェックON、キャプチャあり
20 = チェックOFF（履歴あり）、キャプチャなし
22 = チェックOFF（履歴あり）、キャプチャあり
```

---

## 2. 派生UI属性

| 属性 | 計算ロジック |
|------|-------------|
| CameraButtonVisibility | 下記参照 |
| IsCheckBoxEnabled | WindowMode==ChecklistWindow → true |

### CameraButtonVisibility詳細
| WindowMode | 条件 | 結果 |
|------------|------|------|
| MainWindow | CaptureFileExists==true | Visible |
| MainWindow | CaptureFileExists==false | Collapsed |
| ChecklistWindow | ItemState[1]=='1' AND CaptureFileExists==true | Visible |
| ChecklistWindow | 上記以外 | Collapsed |

### コマンド（Window側で設定）
| WindowMode | コマンド |
|------------|---------|
| MainWindow | SelectCommand |
| ChecklistWindow | CheckedChangedCommand |

---

## 3. 状態遷移トリガー

| トリガー | 対象状態 | 遷移 |
|---------|---------|------|
| ViewModel生成時 | WindowMode | 固定値設定 |
| ViewModel初期化時 | CaptureFileExists | File.Exists()で1回チェック |
| チェックボックスON | ItemState | 00/20/22 → 10 or 11 |
| チェックボックスOFF | ItemState | 10→20, 11→22 |
| キャプチャ取得完了 | ItemState, CaptureFileExists | 10→11, false→true |
| キャプチャ削除 | ItemState, CaptureFileExists | 11→10 or 22→20, true→false |

---

## 4. クラス構成

### 新規作成
| クラス | 責務 |
|--------|------|
| CheckItemState | 状態保持・派生プロパティ計算 |
| CheckItemViewModelFactory | Entity → ViewModel変換 |

### 既存修正
| クラス | 変更内容 |
|--------|---------|
| CheckItemViewModel | CheckItemStateを保持、派生プロパティを委譲 |
| CheckItemUIBuilder | ViewModel → UI要素生成のみに縮小 |
| MainWindow / ChecklistWindow | コマンド設定を担当 |

### 既存維持
| クラス | 責務 |
|--------|------|
| ChecklistStateManager | ビジネスロジック・DB操作 |
| CheckItemTransition | DBトランザクション管理 |

---

## 5. 依存関係（DI）

```
MainWindow / ChecklistWindow
    ├── [DI] CheckItemViewModelFactory
    ├── [DI] CheckItemUIBuilder
    └── [DI] ChecklistStateManager
    └── コマンド設定（Window側で実装）

CheckItemViewModel
    └── CheckItemState（直接保持、ViewModelと同じライフサイクル）

CheckItemViewModelFactory
    └── Entity → CheckItemViewModel + CheckItemState 生成

CheckItemUIBuilder
    └── ViewModel階層 → UI要素階層 生成
```

### DI登録ファイル
- `src/DocumentFileManager.UI/AppInitializer.cs` (102行目付近)

---

## 6. 実装フェーズ

### Phase 1: CheckItemState作成
- 新規ファイル: `Models/CheckItemState.cs`
- 3つの状態パラメータ（WindowMode, ItemState, CaptureFileExists）
- 派生プロパティ（CameraButtonVisibility, IsCheckBoxEnabled）
- WindowModeによるCameraButtonVisibility分岐ロジック
- 単体テスト作成: `tests/.../CheckItemStateTests.cs`

### Phase 2: CheckItemViewModel修正
- CheckItemStateを保持するプロパティ追加
- 派生プロパティ（CameraButtonVisibility, IsCheckBoxEnabled）をStateに委譲
- 既存プロパティとの互換性維持
- File.Exists()をコンストラクタで1回だけ実行

### Phase 3: CheckItemViewModelFactory作成
- 新規ファイル: `Factories/CheckItemViewModelFactory.cs`
- Entity → ViewModel変換ロジックを移動
- CheckItemStateの初期化（WindowMode, 初期ItemState設定）
- DIに登録（`AppInitializer.cs`）
- 単体テスト作成: `tests/.../CheckItemViewModelFactoryTests.cs`

### Phase 4: CheckItemUIBuilder縮小
- ViewModel構築ロジックを削除（BuildViewModelHierarchy → Factory呼び出しに変更）
- コマンド設定を削除（SetupCommands削除）
- UI要素生成のみに限定（CreateGroupBox, CreateCheckBox維持）
- HandleCheckOnAsync, HandleCheckOffAsync → 呼び出し元へ移動

### Phase 5: Window側でコマンド設定
- MainWindow: SelectCommand設定
- ChecklistWindow: CheckedChangedCommand設定
- ViewCaptureCommand: 両Window共通で設定
- コールバック方式（OnCaptureRequested, OnItemSelected）を廃止
- HandleCheckOnAsync/HandleCheckOffAsync呼び出しをWindow側で実装

### Phase 6: テスト・動作確認
- 単体テスト実行
- 結合テスト
- 手動動作確認
  - MainWindow: チェック項目クリックで資料フィルタリング
  - MainWindow: キャプチャボタン表示（キャプチャあれば常に表示）
  - ChecklistWindow: チェックON/OFF→DB保存
  - ChecklistWindow: キャプチャボタン表示（チェックON かつ キャプチャあり）
  - キャプチャ取得・削除

---

## 7. 対象ファイル

### 新規作成
- `src/DocumentFileManager.UI/Models/CheckItemState.cs`
- `src/DocumentFileManager.UI/Factories/CheckItemViewModelFactory.cs`
- `tests/DocumentFileManager.Tests/Models/CheckItemStateTests.cs`
- `tests/DocumentFileManager.Tests/Factories/CheckItemViewModelFactoryTests.cs`

### 修正
- `src/DocumentFileManager.UI/ViewModels/CheckItemViewModel.cs`
- `src/DocumentFileManager.UI/Helpers/CheckItemUIBuilder.cs`
- `src/DocumentFileManager.UI/Windows/MainWindow.xaml.cs`
- `src/DocumentFileManager.UI/Windows/ChecklistWindow.xaml.cs`
- `src/DocumentFileManager.UI/AppInitializer.cs`

### 維持（変更なし）
- `src/DocumentFileManager.UI/Models/CheckItemTransition.cs`
- `src/DocumentFileManager.UI/Services/ChecklistStateManager.cs`
- `src/DocumentFileManager.UI/Behaviors/CheckBoxBehaviors.cs`

---

## 8. 補足: CheckItemStateとCheckItemTransitionの違い

| 項目 | CheckItemState | CheckItemTransition |
|------|----------------|---------------------|
| 目的 | UI表示状態管理 | DBトランザクション管理 |
| ライフサイクル | ViewModel生存中 | DB操作中のみ |
| 状態更新 | UI操作で即座に反映 | Commit時にDB反映 |
| ロールバック | なし | あり（Rollbackメソッド） |

両クラスは独立して動作し、ItemStateの同期は不要。
CheckItemTransitionはDB操作時のみ生成・使用される。
