# 実装プランと実装コードの差異レポート

**作成日**: 2025年10月17日
**対象**: キャプチャ画像紐づけ機能

## 概要
実装プラン（`docs/implementation-plan-capture-integration.md`）と実際の実装コードを比較し、差異を特定しました。

---

## 差異の詳細

### 1. UpdateCaptureFileAsync のパラメータ型（Phase 3）

#### プランの記載
```csharp
Task UpdateCaptureFileAsync(int checkItemDocumentId, string captureFilePath);
```

#### 実装コード
```csharp
Task UpdateCaptureFileAsync(int checkItemDocumentId, string? captureFilePath);
```

**差異内容**: パラメータが `string` → `string?` (nullable) に変更

**理由**: 削除機能でDBのCaptureFileフィールドをnullに更新する必要があるため

**評価**: ✅ **実装の方が正しい**（機能要件を満たす）

**対応**: プランを実装に合わせて修正

---

### 2. 画像確認ボタンの絵文字（Phase 6）

#### プランの記載
- UI変更イメージ（行157-161）:
  ```
  ☑ 平面図 [🖼️]
  ```
- Phase 6（行79）: 「ボタンデザイン: カメラ絵文字（📷）」

**差異内容**: プラン内で2つの異なる絵文字が記載されている
- 「🖼️」（フレーム付き画像） - UI変更イメージに記載
- 「📷」（カメラ） - Phase 6の実装詳細に記載

#### 実装コード
```csharp
Content = "📷",  // CheckItemUIBuilder.cs:258
```

**理由**: ユーザーフィードバックで「📷」（カメラ）に決定
- 初回実装: 白黒絵文字
- 途中試行: カラフルなアイコン（ベクターグラフィック）
- 最終決定: 「📷」（カメラ）絵文字、薄い赤背景

**評価**: ✅ **実装が正しい**（ユーザー承認済み）

**対応**: プランの UI変更イメージを「📷」に統一

---

### 3. CaptureImageViewerWindow の拡大縮小機能（Phase 4）

#### プランの記載（行43-51）
```markdown
- [x] CaptureImageViewerWindow.xaml を作成
  - ファイル: `src/DocumentFileManager.UI/CaptureImageViewerWindow.xaml`
  - 内容: 画像表示、拡大縮小、削除ボタン
```

#### 実装コード
- ScrollViewer で基本的なスクロール対応
- Stretch="Uniform" で画像をウィンドウに合わせて表示
- **ズーム機能（マウスホイールでの拡大縮小等）は未実装**

**差異内容**: プランには「拡大縮小」機能が記載されているが、実装は基本的な表示のみ

**評価**: ⚠️ **機能不足の可能性**
- 基本機能としては問題なし（ユーザー承認済み）
- 高度なズーム機能は将来的な改善点

**対応**: プランの記述を「画像表示（スクロール対応）、削除ボタン」に修正

---

### 4. ボタンデザインの詳細（Phase 6）

#### プランの記載（行79）
```markdown
- ボタンデザイン: カメラ絵文字（📷）、薄い赤背景（RGB: 255, 220, 220）
```

#### 実装コード（CheckItemUIBuilder.cs:255-272）
```csharp
var imageButton = new Button
{
    Content = "📷",
    Width = 24,
    Height = 20,
    Margin = new Thickness(5, 0, 0, 0),
    Visibility = viewModel.HasCapture ? Visibility.Visible : Visibility.Collapsed,
    Tag = viewModel,
    FontSize = 11,
    Background = new SolidColorBrush(Color.FromRgb(255, 220, 220)), // 薄い赤
    BorderBrush = new SolidColorBrush(Color.FromRgb(200, 160, 160)), // 薄い赤茶
    BorderThickness = new Thickness(1),
    Cursor = System.Windows.Input.Cursors.Hand,
    Padding = new Thickness(1),
    VerticalContentAlignment = VerticalAlignment.Center,
    HorizontalContentAlignment = HorizontalAlignment.Center
};
```

**差異内容**: プランには基本情報のみ、実装には詳細なスタイル設定が含まれる

**評価**: ✅ **実装の方が詳細**（ユーザーフィードバックを反映）
- サイズ: 24×20（チェックボックスの高さに合わせた）
- フォントサイズ: 11
- カーソル: Hand
- 枠線色: RGB(200, 160, 160)

**対応**: プランにボタンの詳細仕様を追加

---

### 5. StackPanel の Tag プロパティ（Phase 6）

#### プランの記載（行76-77）
```markdown
- 画像確認ボタンの実装:
  - Tag プロパティに ViewModel を保持
```

#### 実装コード（CheckItemUIBuilder.cs:312-318）
```csharp
var stackPanel = new StackPanel
{
    Orientation = Orientation.Horizontal,
    Tag = new { CheckBox = checkBox, ImageButton = imageButton, ViewModel = viewModel }
};
stackPanel.Children.Add(checkBox);
stackPanel.Children.Add(imageButton);
```

**差異内容**:
- プラン: ViewModel のみを保持
- 実装: CheckBox、ImageButton、ViewModel を含む匿名型を保持

**理由**: ChecklistWindow.PerformCaptureForCheckItem から ImageButton を特定するため

**評価**: ✅ **実装の方が柔軟**

**対応**: プランの記述を明確化

---

## 修正プラン

### 必須修正（プランを実装に合わせる）

1. **Phase 3 の記述修正**
   - `Task UpdateCaptureFileAsync(int checkItemDocumentId, string? captureFilePath);` に変更
   - nullable を明示

2. **UI変更イメージの修正**
   - 「🖼️」→「📷」に統一

3. **Phase 4 の記述修正**
   - 「画像表示、拡大縮小、削除ボタン」→「画像表示（スクロール対応）、削除ボタン」に変更

4. **Phase 6 のボタン仕様追加**
   - サイズ、フォント、カーソル等の詳細を追記

5. **Phase 6 の Tag プロパティ説明を明確化**
   - 匿名型の使用を明記

### 将来的な改善点（オプション）

1. **CaptureImageViewerWindow のズーム機能追加**
   - マウスホイールでの拡大縮小
   - ピンチジェスチャー対応（タッチデバイス）
   - 拡大率表示

2. **キャプチャ画像の一括削除機能**
   - CheckItemDocument 削除時に物理ファイルも削除
   - 孤立した画像ファイルのクリーンアップ

---

## 総評

**実装品質**: ✅ 高品質
- プランとの差異はすべて**実装の方が優れている**か、**ユーザーフィードバックを反映したもの**
- 基本機能は完全に実装済み
- エラーハンドリング、ロギング、コメントも適切

**プラン更新の必要性**: ⚠️ 中程度
- 実装の詳細がプランに反映されていない箇所がある
- ドキュメントの正確性向上のため、プラン修正を推奨

**次のアクション**:
1. 実装プランを実装コードに合わせて修正
2. ユーザーフィードバックの履歴をプランに追記（オプション）
3. 将来的な改善点を別ドキュメントに記録（オプション）
