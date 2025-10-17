using DocumentFileManager.Entities;
using Xunit;
using System.IO;

namespace DocumentFileManager.Tests.Entities;

/// <summary>
/// Documentエンティティの単体テスト
/// </summary>
public class DocumentTests
{
    [Fact]
    public void GetAbsolutePath_相対パスから絶対パスを生成()
    {
        // Arrange
        var document = new Document
        {
            RelativePath = Path.Combine("test-files", "sample.pdf")
        };
        var projectRoot = @"C:\Projects\MyApp";

        // Act
        var absolutePath = document.GetAbsolutePath(projectRoot);

        // Assert
        var expected = Path.Combine(@"C:\Projects\MyApp", "test-files", "sample.pdf");
        Assert.Equal(expected, absolutePath);
    }

    [Fact]
    public void GetAbsolutePath_空のプロジェクトルート_相対パスのみ返す()
    {
        // Arrange
        var document = new Document
        {
            RelativePath = "test-files/sample.pdf"
        };
        var projectRoot = "";

        // Act
        var absolutePath = document.GetAbsolutePath(projectRoot);

        // Assert
        Assert.Equal("test-files/sample.pdf", absolutePath);
    }

    [Fact]
    public void Exists_ファイルが存在しない_Falseを返す()
    {
        // Arrange
        var document = new Document
        {
            RelativePath = "nonexistent-file.pdf"
        };
        var projectRoot = Path.GetTempPath();

        // Act
        var exists = document.Exists(projectRoot);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void Exists_ファイルが存在する_Trueを返す()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var fileName = Path.GetFileName(tempFile);
        var projectRoot = Path.GetDirectoryName(tempFile)!;

        var document = new Document
        {
            RelativePath = fileName
        };

        try
        {
            // Act
            var exists = document.Exists(projectRoot);

            // Assert
            Assert.True(exists);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void デフォルト値の確認()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        Assert.Equal(0, document.Id);
        Assert.Equal(string.Empty, document.FileName);
        Assert.Equal(string.Empty, document.RelativePath);
        Assert.Equal(string.Empty, document.FileType);
        Assert.NotEqual(default, document.AddedAt);
        Assert.Empty(document.LinkedCheckItems);
    }

    [Fact]
    public void LinkedCheckItems_初期状態は空のコレクション()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        Assert.Empty(document.LinkedCheckItems);
    }

    [Fact]
    public void LinkedCheckItems_紐づけを追加できる()
    {
        // Arrange
        var document = new Document
        {
            Id = 1,
            FileName = "test.pdf"
        };

        var checkItem = new CheckItem
        {
            Id = 1,
            Label = "確認項目"
        };

        var link = new CheckItemDocument
        {
            DocumentId = document.Id,
            CheckItemId = checkItem.Id,
            Document = document,
            CheckItem = checkItem
        };

        // Act
        document.LinkedCheckItems.Add(link);

        // Assert
        Assert.Single(document.LinkedCheckItems);
        Assert.Contains(link, document.LinkedCheckItems);
    }

    [Fact]
    public void AddedAt_デフォルトでUTC時刻が設定される()
    {
        // Arrange & Act
        var beforeCreation = DateTime.UtcNow.AddSeconds(-1);
        var document = new Document();
        var afterCreation = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.InRange(document.AddedAt, beforeCreation, afterCreation);
        Assert.Equal(DateTimeKind.Utc, document.AddedAt.Kind);
    }
}
