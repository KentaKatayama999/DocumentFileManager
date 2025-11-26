using System.IO;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Configuration;
using DocumentFileManager.UI.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocumentFileManager.Tests.Helpers;

/// <summary>
/// CheckItemUIBuilderのテスト（Issue #1: パス解決の統一）
/// </summary>
public class CheckItemUIBuilderTests : IDisposable
{
    private readonly DocumentManagerContext _context;
    private readonly CheckItemRepository _checkItemRepository;
    private readonly CheckItemDocumentRepository _checkItemDocumentRepository;
    private readonly UISettings _uiSettings;
    private readonly string _testRootPath;

    public CheckItemUIBuilderTests()
    {
        var options = new DbContextOptionsBuilder<DocumentManagerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DocumentManagerContext(options);
        _checkItemRepository = new CheckItemRepository(_context, NullLogger<CheckItemRepository>.Instance);
        _checkItemDocumentRepository = new CheckItemDocumentRepository(_context);
        _uiSettings = new UISettings();

        // テスト用の一時ディレクトリを作成
        _testRootPath = Path.Combine(Path.GetTempPath(), $"CheckItemUIBuilderTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRootPath);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();

        // テスト用ディレクトリを削除
        if (Directory.Exists(_testRootPath))
        {
            Directory.Delete(_testRootPath, recursive: true);
        }
    }

    private CheckItemUIBuilder CreateBuilder(string documentRootPath)
    {
        return new CheckItemUIBuilder(
            _checkItemRepository,
            _checkItemDocumentRepository,
            _uiSettings,
            NullLogger<CheckItemUIBuilder>.Instance,
            documentRootPath);
    }

    [Fact]
    public void Constructor_documentRootPathが正しく設定される()
    {
        // Arrange & Act
        var builder = CreateBuilder(_testRootPath);

        // Assert
        Assert.Equal(_testRootPath, builder.DocumentRootPath);
    }

    [Fact]
    public void ResolveCaptureFilePath_相対パスから絶対パスを正しく解決する()
    {
        // Arrange
        var builder = CreateBuilder(_testRootPath);
        var relativePath = "captures/document_1/capture_001.png";

        // Act
        var absolutePath = builder.ResolveCaptureFilePath(relativePath);

        // Assert
        var expected = Path.GetFullPath(Path.Combine(_testRootPath, relativePath));
        Assert.Equal(expected, absolutePath);
    }

    [Fact]
    public void ResolveCaptureFilePath_異なるdocumentRootPathで異なる結果を返す()
    {
        // Arrange
        var rootPath1 = Path.Combine(_testRootPath, "ProjectA");
        var rootPath2 = Path.Combine(_testRootPath, "ProjectB");
        Directory.CreateDirectory(rootPath1);
        Directory.CreateDirectory(rootPath2);

        var builder1 = CreateBuilder(rootPath1);
        var builder2 = CreateBuilder(rootPath2);
        var relativePath = "captures/capture_001.png";

        // Act
        var absolutePath1 = builder1.ResolveCaptureFilePath(relativePath);
        var absolutePath2 = builder2.ResolveCaptureFilePath(relativePath);

        // Assert
        Assert.NotEqual(absolutePath1, absolutePath2);
        Assert.StartsWith(rootPath1, absolutePath1);
        Assert.StartsWith(rootPath2, absolutePath2);
    }

    [Fact]
    public void ResolveCaptureFilePath_ファイル名のみでも正しく解決する()
    {
        // Arrange
        var builder = CreateBuilder(_testRootPath);
        var fileName = "capture_001.png";

        // Act
        var absolutePath = builder.ResolveCaptureFilePath(fileName);

        // Assert
        var expected = Path.GetFullPath(Path.Combine(_testRootPath, fileName));
        Assert.Equal(expected, absolutePath);
    }

    [Fact]
    public void ResolveCaptureFilePath_nullを渡すと例外をスローする()
    {
        // Arrange
        var builder = CreateBuilder(_testRootPath);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ResolveCaptureFilePath(null!));
    }

    [Fact]
    public void ResolveCaptureFilePath_空文字を渡すと例外をスローする()
    {
        // Arrange
        var builder = CreateBuilder(_testRootPath);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.ResolveCaptureFilePath(""));
    }

    [Fact]
    public void ResolveCaptureFilePath_日本語パスを正しく解決する()
    {
        // Arrange
        var japaneseRootPath = Path.Combine(_testRootPath, "プロジェクトA");
        Directory.CreateDirectory(japaneseRootPath);
        var builder = CreateBuilder(japaneseRootPath);
        var relativePath = "キャプチャ/画像_001.png";

        // Act
        var absolutePath = builder.ResolveCaptureFilePath(relativePath);

        // Assert
        var expected = Path.GetFullPath(Path.Combine(japaneseRootPath, relativePath));
        Assert.Equal(expected, absolutePath);
    }

    [Fact]
    public void ResolveCaptureFilePath_MainWindowとChecklistWindowで同じ結果を返す()
    {
        // Arrange - 同じdocumentRootPathを使用する2つのビルダー
        // （実際のアプリではMainWindowとChecklistWindowは同じdocumentRootPathを共有）
        var sharedRootPath = Path.Combine(_testRootPath, "SharedProject");
        Directory.CreateDirectory(sharedRootPath);

        var builderForMainWindow = CreateBuilder(sharedRootPath);
        var builderForChecklistWindow = CreateBuilder(sharedRootPath);
        var relativePath = "captures/document_1/capture_001.png";

        // Act
        var pathFromMainWindow = builderForMainWindow.ResolveCaptureFilePath(relativePath);
        var pathFromChecklistWindow = builderForChecklistWindow.ResolveCaptureFilePath(relativePath);

        // Assert - 同じdocumentRootPathなら同じ結果になる（Issue #1の修正確認）
        Assert.Equal(pathFromMainWindow, pathFromChecklistWindow);
    }

    [Fact]
    public void ResolveCaptureFilePath_パスセパレータを正規化する()
    {
        // Arrange
        var builder = CreateBuilder(_testRootPath);
        // Unix形式のパスセパレータを使用
        var relativePath = "captures/document_1/capture_001.png";

        // Act
        var absolutePath = builder.ResolveCaptureFilePath(relativePath);

        // Assert - Path.GetFullPathで正規化されるため、プラットフォーム固有のセパレータになる
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), absolutePath);
    }
}
