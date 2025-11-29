# バグ修正・仕様変更プラン（修正版）

**作成日**: 2025-11-26
**最終更新**: 2025-11-26（レビュー指摘対応）
**対象**: DocumentFileManager

---

## 概要

以下の5件のバグ修正および仕様変更を実施する。

| # | 種別 | 内容 | 対象ファイル |
|---|------|------|-------------|
| 1 | バグ | カメラアイコン押下時の画像パス解決がMainWindowとChecklistWindowで異なる | `CheckItemUIBuilder.cs`, `AppInitializer.cs` |
| 2 | バグ | ChecklistWindowで既存キャプチャ画像のカメラアイコンが表示されない | Issue #1で解決 |
| 3 | 機能改善 | 「左に配置」ボタン押下時にViewerWindowを右側に移動させる | `ChecklistWindow.xaml.cs` |
| 4 | 機能追加 | ChecklistWindowの「新規作成」機能をAPI化 | 新規: `Services/ChecklistService.cs` |
| 5 | 機能追加 | MainWindowの「資料追加」機能をAPI化 | 新規: `Services/DocumentService.cs` |

---

## 詳細分析

### Issue #1: カメラアイコン押下時のパス解決の不整合

**現象**:
- MainWindowとChecklistWindowでカメラアイコンをクリックしたとき、キャプチャ画像のパス解決方法が異なる

**原因箇所**: `CheckItemUIBuilder.cs:302-310`

```csharp
// 画像確認ボタンクリック
imageButton.Click += (sender, e) =>
{
    if (viewModel.CaptureFilePath != null)
    {
        var absolutePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "",
            "..", "..", "..", "..", "..",
            viewModel.CaptureFilePath);
        absolutePath = Path.GetFullPath(absolutePath);
        // ...
    }
};
```

**根本原因**:
- `CheckItemUIBuilder`のコンストラクタに`documentRootPath`パラメータが**存在しない**
- 現在のコンストラクタ（28-38行目）:
  ```csharp
  public CheckItemUIBuilder(
      ICheckItemRepository repository,
      ICheckItemDocumentRepository checkItemDocumentRepository,
      UISettings settings,
      ILogger<CheckItemUIBuilder> logger)
  ```
- `Assembly.GetExecutingAssembly().Location`から5階層上を計算しているが、この方法は開発環境でのみ動作し、本番環境（publish後）では正しく動作しない

**修正方針**:
1. `CheckItemUIBuilder`のコンストラクタに`string documentRootPath`パラメータを追加
2. プライベートフィールド`private readonly string _documentRootPath;`を追加
3. 画像パス解決時に`Path.Combine(_documentRootPath, viewModel.CaptureFilePath)`を使用
4. DI設定: `AppInitializer.cs`の97行目で登録されている`CheckItemUIBuilder`は自動的に`documentRootPath`を解決できる（65行目で`services.AddSingleton(_ => documentRootPath);`として登録済み）

**修正コード案**:

`CheckItemUIBuilder.cs`コンストラクタ:
```csharp
private readonly string _documentRootPath;

public CheckItemUIBuilder(
    ICheckItemRepository repository,
    ICheckItemDocumentRepository checkItemDocumentRepository,
    UISettings settings,
    ILogger<CheckItemUIBuilder> logger,
    string documentRootPath)  // 追加
{
    _repository = repository;
    _checkItemDocumentRepository = checkItemDocumentRepository;
    _settings = settings;
    _logger = logger;
    _documentRootPath = documentRootPath;  // 追加
}
```

画像パス解決（302-310行目）:
```csharp
imageButton.Click += (sender, e) =>
{
    if (viewModel.CaptureFilePath != null)
    {
        var absolutePath = Path.Combine(_documentRootPath, viewModel.CaptureFilePath);
        absolutePath = Path.GetFullPath(absolutePath);
        // ...
    }
};
```

---

### Issue #2: ChecklistWindowで既存キャプチャ画像のカメラアイコンが表示されない

**原因分析**:
- **Issue #1と同じ根本原因**
- `CheckItemUIBuilder.cs:127-135`でChecklistWindowの場合も`CaptureFilePath`は正しく設定されている
- カメラアイコンのVisibility判定（289行目）は`viewModel.HasCapture`で行われる
- `HasCapture`は`CaptureFilePath != null`で判定されるため、データベースの`CaptureFile`カラムに値が保存されていれば表示される

**確認事項**:
- DBの`CheckItemDocument.CaptureFile`カラムに正しく相対パスが保存されているか検証
- キャプチャ保存時（`ChecklistWindow.cs:719`）に`CaptureFile`が正しく設定されているか確認

**修正方針**:
- Issue #1を修正すれば、パス解決の問題は解決
- DBデータの検証を実施し、必要に応じてデータ修正

---

### Issue #3: 「左に配置」ボタンでViewerWindowも移動させる

**現象**:
- ChecklistWindowの「左に配置」ボタンを押すとChecklistWindowが左に移動する
- ViewerWindowは左側のまま残り、重なってしまう

**原因**:
- `DockLeftButton_Click`（256-273行目）でChecklistWindowの位置のみ変更している
- ViewerWindowの位置変更処理がない

**技術的課題**:
- `ChecklistWindow.xaml.cs`に`SetWindowPos` Win32 APIのインポートが**存在しない**
- ChecklistWindowは`IntPtr _documentWindowHandle`（ウィンドウハンドル）のみを保持
- ViewerWindowインスタンスへの参照は持っていないため、Win32 APIを直接使用する必要がある

**修正方針**:
1. `ChecklistWindow.xaml.cs`に`SetWindowPos` Win32 APIをインポート追加
2. `DockLeftButton_Click`でViewerWindowを右側に移動
3. `DockRightButton_Click`でViewerWindowを左側に移動

**修正コード案**:

Win32 APIインポート追加（クラス先頭部分に追加）:
```csharp
[DllImport("user32.dll", SetLastError = true)]
private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

[DllImport("user32.dll")]
private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

private const int SW_RESTORE = 9;
```

`DockLeftButton_Click`修正:
```csharp
private void DockLeftButton_Click(object sender, RoutedEventArgs e)
{
    _isDockingRight = false;
    _isAdjustingPosition = true;

    try
    {
        var workArea = SystemParameters.WorkArea;

        // ChecklistWindowを左に配置
        Left = workArea.Left;
        Top = workArea.Top;
        Height = workArea.Height;

        // ViewerWindow（資料ウィンドウ）を右に配置
        if (_documentWindowHandle != IntPtr.Zero)
        {
            ShowWindow(_documentWindowHandle, SW_RESTORE); // 最大化を解除
            int viewerX = (int)(workArea.Left + ActualWidth);
            int viewerY = (int)workArea.Top;
            int viewerWidth = (int)(workArea.Width - ActualWidth);
            int viewerHeight = (int)workArea.Height;

            SetWindowPos(_documentWindowHandle, IntPtr.Zero, viewerX, viewerY, viewerWidth, viewerHeight, 0);
            _logger.LogDebug("ViewerWindowを右端に配置: X={X}, Y={Y}, Width={Width}, Height={Height}",
                viewerX, viewerY, viewerWidth, viewerHeight);
        }

        _logger.LogDebug("ウィンドウを左端に配置: Left={Left}, Top={Top}", Left, Top);
    }
    finally
    {
        _isAdjustingPosition = false;
    }
}
```

`DockRightButton_Click`修正:
```csharp
private void DockRightButton_Click(object sender, RoutedEventArgs e)
{
    _isDockingRight = true;
    _isAdjustingPosition = true;

    try
    {
        var workArea = SystemParameters.WorkArea;

        // ChecklistWindowを右に配置
        Left = workArea.Right - ActualWidth;
        Top = workArea.Top;
        Height = workArea.Height;

        // ViewerWindow（資料ウィンドウ）を左に配置
        if (_documentWindowHandle != IntPtr.Zero)
        {
            ShowWindow(_documentWindowHandle, SW_RESTORE); // 最大化を解除
            int viewerX = (int)workArea.Left;
            int viewerY = (int)workArea.Top;
            int viewerWidth = (int)(workArea.Width - ActualWidth);
            int viewerHeight = (int)workArea.Height;

            SetWindowPos(_documentWindowHandle, IntPtr.Zero, viewerX, viewerY, viewerWidth, viewerHeight, 0);
            _logger.LogDebug("ViewerWindowを左端に配置: X={X}, Y={Y}, Width={Width}, Height={Height}",
                viewerX, viewerY, viewerWidth, viewerHeight);
        }

        _logger.LogDebug("ウィンドウを右端に配置: Left={Left}, Top={Top}", Left, Top);
    }
    finally
    {
        _isAdjustingPosition = false;
    }
}
```

---

### Issue #4: 「新規作成」機能のAPI化

**現象**:
- ChecklistWindowの「新規作成」ボタンは現在UI操作でのみ実行可能
- 外部からAPI経由で新規チェックリストを作成したい

**対象箇所**: `ChecklistWindow.xaml.cs:422-540` (`CreateNewChecklistButton_Click`)

**既存アーキテクチャとの整合性**:
- 既存サービス配置: `src/DocumentFileManager.UI/Services/`
- 既存サービス例: `DataIntegrityService`, `ScreenCaptureService`, `SettingsPersistence`
- DI登録: `AppInitializer.cs:90-94`

**修正方針**:
1. `src/DocumentFileManager.UI/Services/IChecklistService.cs` インターフェース作成
2. `src/DocumentFileManager.UI/Services/ChecklistService.cs` 実装クラス作成
3. 新規チェックリスト作成ロジックをサービスに抽出
4. `AppInitializer.cs`にDI登録追加（`AddScoped`）
5. ChecklistWindow、MainWindowからサービスを呼び出すように変更

**新規ファイル: `IChecklistService.cs`**
```csharp
namespace DocumentFileManager.UI.Services;

public interface IChecklistService
{
    /// <summary>
    /// 新規チェックリストを作成
    /// </summary>
    /// <param name="checklistName">チェックリスト名</param>
    /// <returns>作成されたファイル名</returns>
    Task<ChecklistCreationResult> CreateNewChecklistAsync(string checklistName);
}

public class ChecklistCreationResult
{
    public bool Success { get; set; }
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
}
```

**新規ファイル: `ChecklistService.cs`**
```csharp
namespace DocumentFileManager.UI.Services;

public class ChecklistService : IChecklistService
{
    private readonly ICheckItemRepository _checkItemRepository;
    private readonly Infrastructure.Services.ChecklistSaver _checklistSaver;
    private readonly PathSettings _pathSettings;
    private readonly ILogger<ChecklistService> _logger;
    private readonly string _documentRootPath;

    public ChecklistService(
        ICheckItemRepository checkItemRepository,
        Infrastructure.Services.ChecklistSaver checklistSaver,
        PathSettings pathSettings,
        ILogger<ChecklistService> logger,
        string documentRootPath)
    {
        _checkItemRepository = checkItemRepository;
        _checklistSaver = checklistSaver;
        _pathSettings = pathSettings;
        _logger = logger;
        _documentRootPath = documentRootPath;
    }

    public async Task<ChecklistCreationResult> CreateNewChecklistAsync(string checklistName)
    {
        // ChecklistWindow.CreateNewChecklistButton_Clickのロジックを移植
        // パストラバーサル対策: Path.GetInvalidFileNameChars()でサニタイズ
        // 絶対パス検証: _documentRootPath配下であることを確認
    }
}
```

**DI登録追加（`AppInitializer.cs:94`の後に追加）**:
```csharp
services.AddScoped<IChecklistService, ChecklistService>();
```

---

### Issue #5: 「資料追加」機能のAPI化

**現象**:
- MainWindowの「資料追加」ボタンは現在UI操作でのみ実行可能
- 外部からAPI経由で資料を追加したい

**対象箇所**:
- `MainWindow.xaml.cs:423-466` (`AddDocumentButton_Click`)
- `MainWindow.xaml.cs:471-551` (`RegisterDocumentAsync`)

**修正方針**:
1. `src/DocumentFileManager.UI/Services/IDocumentService.cs` インターフェース作成
2. `src/DocumentFileManager.UI/Services/DocumentService.cs` 実装クラス作成
3. 資料登録ロジックをサービスに抽出
4. `AppInitializer.cs`にDI登録追加（`AddScoped`）
5. MainWindowからサービスを呼び出すように変更

**新規ファイル: `IDocumentService.cs`**
```csharp
namespace DocumentFileManager.UI.Services;

public interface IDocumentService
{
    /// <summary>
    /// 資料を登録
    /// </summary>
    /// <param name="filePath">登録するファイルのパス</param>
    /// <returns>登録結果</returns>
    Task<DocumentRegistrationResult> RegisterDocumentAsync(string filePath);

    /// <summary>
    /// 複数の資料を一括登録
    /// </summary>
    /// <param name="filePaths">登録するファイルのパスリスト</param>
    /// <returns>登録結果リスト</returns>
    Task<List<DocumentRegistrationResult>> RegisterDocumentsAsync(IEnumerable<string> filePaths);
}

public class DocumentRegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Document? Document { get; set; }
}
```

**新規ファイル: `DocumentService.cs`**
```csharp
namespace DocumentFileManager.UI.Services;

public class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentService> _logger;
    private readonly string _documentRootPath;

    public DocumentService(
        IDocumentRepository documentRepository,
        ILogger<DocumentService> logger,
        string documentRootPath)
    {
        _documentRepository = documentRepository;
        _logger = logger;
        _documentRootPath = documentRootPath;
    }

    public async Task<DocumentRegistrationResult> RegisterDocumentAsync(string filePath)
    {
        // MainWindow.RegisterDocumentAsyncのロジックを移植
        // パストラバーサル対策を実施
    }

    public async Task<List<DocumentRegistrationResult>> RegisterDocumentsAsync(IEnumerable<string> filePaths)
    {
        var results = new List<DocumentRegistrationResult>();
        foreach (var filePath in filePaths)
        {
            results.Add(await RegisterDocumentAsync(filePath));
        }
        return results;
    }
}
```

**DI登録追加（`AppInitializer.cs:94`の後に追加）**:
```csharp
services.AddScoped<IDocumentService, DocumentService>();
```

---

## 実装順序

以下の順序で実装を進める：

### Phase 1: バグ修正（優先度高）
1. **Issue #1**: CheckItemUIBuilderにdocumentRootPath追加
   - コンストラクタ変更
   - パス解決ロジック修正
   - DIは自動解決（変更不要）
2. **Issue #2**: Issue #1で解決。DBデータ検証のみ実施

### Phase 2: UI改善
3. **Issue #3**: 左/右配置ボタンでViewerWindowも連動
   - Win32 APIインポート追加
   - DockLeftButton_Click修正
   - DockRightButton_Click修正

### Phase 3: API化
4. **Issue #5**: DocumentService作成（資料追加API）
   - IDocumentService.cs作成
   - DocumentService.cs作成
   - DI登録
   - MainWindow修正
5. **Issue #4**: ChecklistService作成（新規作成API）
   - IChecklistService.cs作成
   - ChecklistService.cs作成
   - DI登録
   - ChecklistWindow、MainWindow修正

---

## 影響範囲

| ファイル | 変更内容 |
|---------|---------|
| `CheckItemUIBuilder.cs` | コンストラクタにdocumentRootPath追加、パス解決修正 |
| `ChecklistWindow.xaml.cs` | Win32 API追加、配置ボタン修正、サービス呼び出し |
| `MainWindow.xaml.cs` | サービス呼び出し |
| `AppInitializer.cs:94` | 新規サービスのDI登録追加 |
| 新規: `Services/IDocumentService.cs` | 資料登録インターフェース |
| 新規: `Services/DocumentService.cs` | 資料登録サービス実装 |
| 新規: `Services/IChecklistService.cs` | チェックリスト管理インターフェース |
| 新規: `Services/ChecklistService.cs` | チェックリスト管理サービス実装 |

**呼び出し元への影響**:
- `CheckItemUIBuilder`のコンストラクタ変更は、DIで自動解決されるため既存コードへの影響なし
- サービス化により、UIロジックの重複（ChecklistWindow/MainWindow）が解消される

---

## セキュリティ考慮事項

- **パストラバーサル対策**:
  - `Path.GetInvalidFileNameChars()`でのサニタイズ（既存実装を継承）
  - 絶対パス検証: `Path.GetFullPath`後に`_documentRootPath`配下であることを確認
  - サービス層で入力検証を実施

---

## テスト項目

### Issue #1
- [ ] MainWindowでカメラアイコンをクリックして画像が表示される
- [ ] ChecklistWindowでカメラアイコンをクリックして画像が表示される
- [ ] 両方で同じ画像ファイルが表示される
- [ ] ログ出力で絶対パスが一致することを確認

### Issue #2
- [ ] 既存のキャプチャ画像があるチェック項目でChecklistWindowを開く
- [ ] カメラアイコンが表示される
- [ ] DBの`CheckItemDocument.CaptureFile`カラムに値が存在することを確認

### Issue #3
- [ ] 「左に配置」ボタンでChecklistWindowが左に移動する
- [ ] 同時にViewerWindowが右に移動する
- [ ] 「右に配置」ボタンで逆の動作をする
- [ ] 移動後の座標がログ出力で確認できる

### Issue #4
- [ ] `IChecklistService.CreateNewChecklistAsync`で新規チェックリストが作成できる
- [ ] UIボタンからも引き続き作成できる
- [ ] 不正なファイル名（パストラバーサル）がブロックされる

### Issue #5
- [ ] `IDocumentService.RegisterDocumentAsync`で資料が追加できる
- [ ] `IDocumentService.RegisterDocumentsAsync`で複数資料が追加できる
- [ ] UIボタンからも引き続き追加できる
- [ ] 不正なファイルパスがブロックされる
