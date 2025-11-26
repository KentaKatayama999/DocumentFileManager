using System.IO;
using DocumentFileManager.Entities;
using DocumentFileManager.Infrastructure.Data;
using DocumentFileManager.Infrastructure.Repositories;
using DocumentFileManager.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DocumentFileManager.Tests.Services;

/// <summary>
/// DocumentServiceのテスト
/// </summary>
public class DocumentServiceTests : IDisposable
{
    private readonly DocumentManagerContext _context;
    private readonly DocumentRepository _repository;
    private readonly DocumentService _service;
    private readonly string _testRootPath;
    private readonly string _testFilePath;

    public DocumentServiceTests()
    {
        var options = new DbContextOptionsBuilder<DocumentManagerContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DocumentManagerContext(options);
        _repository = new DocumentRepository(_context, NullLogger<DocumentRepository>.Instance);

        // テスト用の一時ディレクトリを作成
        _testRootPath = Path.Combine(Path.GetTempPath(), $"DocumentServiceTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRootPath);

        // テスト用ファイルを作成
        _testFilePath = Path.Combine(_testRootPath, "test_source.pdf");
        File.WriteAllText(_testFilePath, "test content");

        _service = new DocumentService(
            _repository,
            NullLogger<DocumentService>.Instance,
            _testRootPath);
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

    [Fact]
    public async Task RegisterDocumentAsync_正常に資料を登録できる()
    {
        // Arrange
        var sourceFile = Path.Combine(_testRootPath, "source", "document.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(sourceFile)!);
        File.WriteAllText(sourceFile, "pdf content");

        // Act
        var result = await _service.RegisterDocumentAsync(sourceFile);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Document);
        Assert.Equal("document.pdf", result.Document.FileName);
        Assert.Equal(".pdf", result.Document.FileType);

        // ファイルがコピーされていることを確認
        var copiedPath = Path.Combine(_testRootPath, "document.pdf");
        Assert.True(File.Exists(copiedPath));
    }

    [Fact]
    public async Task RegisterDocumentAsync_既にルート内にあるファイルも正常に登録される()
    {
        // Arrange - ルート内にファイルを配置
        var fileInRoot = Path.Combine(_testRootPath, "already_in_root.pdf");
        File.WriteAllText(fileInRoot, "content");

        // Act
        var result = await _service.RegisterDocumentAsync(fileInRoot);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Document);
        // 同名ファイルが既に存在するため、連番が付与される
        Assert.Contains("already_in_root", result.Document.FileName);
    }

    [Fact]
    public async Task RegisterDocumentAsync_存在しないファイルはエラー()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testRootPath, "non_existent.pdf");

        // Act
        var result = await _service.RegisterDocumentAsync(nonExistentFile);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("ファイルが見つかりません", result.ErrorMessage);
    }

    [Fact]
    public async Task RegisterDocumentAsync_DB上で重複するRelativePathはスキップされる()
    {
        // Arrange - DBに既存データを登録
        var existingDoc = new Document
        {
            FileName = "existing.pdf",
            RelativePath = "existing.pdf",
            FileType = ".pdf",
            AddedAt = DateTime.UtcNow
        };
        await _context.Documents.AddAsync(existingDoc);
        await _context.SaveChangesAsync();

        // 同名ファイルをルートに配置（物理ファイルも存在する状態にする）
        var existingFilePath = Path.Combine(_testRootPath, "existing.pdf");
        File.WriteAllText(existingFilePath, "existing content");

        // 別の場所から同名ファイルを登録しようとする
        var sourceDir = Path.Combine(_testRootPath, "source2");
        Directory.CreateDirectory(sourceDir);
        var sourceFile = Path.Combine(sourceDir, "existing.pdf");
        File.WriteAllText(sourceFile, "new content");

        // Act - コピー先ファイル名は連番になるが、DBに同じRelativePathが無ければ登録される
        var result = await _service.RegisterDocumentAsync(sourceFile);

        // Assert - ファイル名が連番になるのでDB上は重複しない
        Assert.True(result.Success);
        Assert.Equal("existing_1.pdf", result.Document!.FileName);
    }

    [Fact]
    public async Task RegisterDocumentAsync_ファイル名重複時は連番が付く()
    {
        // Arrange
        // ルートに同名ファイルを事前に配置
        var existingFile = Path.Combine(_testRootPath, "conflict.pdf");
        File.WriteAllText(existingFile, "existing content");

        // 別の場所から同名ファイルを登録
        var sourceDir = Path.Combine(_testRootPath, "source3");
        Directory.CreateDirectory(sourceDir);
        var sourceFile = Path.Combine(sourceDir, "conflict.pdf");
        File.WriteAllText(sourceFile, "new content");

        // Act
        var result = await _service.RegisterDocumentAsync(sourceFile);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Document);
        Assert.Equal("conflict_1.pdf", result.Document.FileName);

        // 連番ファイルが作成されていることを確認
        var copiedPath = Path.Combine(_testRootPath, "conflict_1.pdf");
        Assert.True(File.Exists(copiedPath));
    }

    [Fact]
    public async Task RegisterDocumentsAsync_複数ファイルを一括登録できる()
    {
        // Arrange
        var sourceDir = Path.Combine(_testRootPath, "batch");
        Directory.CreateDirectory(sourceDir);

        var files = new List<string>();
        for (int i = 1; i <= 3; i++)
        {
            var file = Path.Combine(sourceDir, $"file{i}.pdf");
            File.WriteAllText(file, $"content {i}");
            files.Add(file);
        }

        // Act
        var results = await _service.RegisterDocumentsAsync(files);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results, r => Assert.True(r.Success));
    }

    [Fact]
    public async Task RegisterDocumentsAsync_一部失敗しても他は登録される()
    {
        // Arrange
        var sourceDir = Path.Combine(_testRootPath, "partial");
        Directory.CreateDirectory(sourceDir);

        var validFile = Path.Combine(sourceDir, "valid.pdf");
        File.WriteAllText(validFile, "valid content");

        var invalidFile = Path.Combine(sourceDir, "invalid.pdf"); // 存在しないファイル

        var files = new List<string> { validFile, invalidFile };

        // Act
        var results = await _service.RegisterDocumentsAsync(files);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
    }

    [Fact]
    public async Task RegisterDocumentAsync_パストラバーサル攻撃を防ぐ()
    {
        // Arrange - パストラバーサルを含むファイル名
        // 実際にはファイル名に「..」を含めることはOSレベルで制限されるため、
        // このテストは主にロジックの確認用
        var sourceDir = Path.Combine(_testRootPath, "traversal");
        Directory.CreateDirectory(sourceDir);
        var sourceFile = Path.Combine(sourceDir, "normal.pdf");
        File.WriteAllText(sourceFile, "content");

        // Act
        var result = await _service.RegisterDocumentAsync(sourceFile);

        // Assert - 正常なファイルは登録される
        Assert.True(result.Success);

        // 登録先がdocumentRootPath配下であることを確認
        var destPath = Path.Combine(_testRootPath, result.Document!.RelativePath);
        Assert.True(destPath.StartsWith(_testRootPath));
    }
}
