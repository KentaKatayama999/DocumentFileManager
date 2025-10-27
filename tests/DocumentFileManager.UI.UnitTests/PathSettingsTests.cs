using System;
using System.IO;
using DocumentFileManager.UI;
using DocumentFileManager.UI.Configuration;
using Xunit;

namespace DocumentFileManager.UI.UnitTests;

public class PathSettingsTests : IDisposable
{
    private readonly string _tempRoot;

    public PathSettingsTests()
    {
        _tempRoot = Directory.CreateTempSubdirectory("DocFM_PathSettingsTest").FullName;
    }

    [Fact]
    public void ToAbsolutePath_WithRelativePath_ReturnsCombinedPath()
    {
        var settings = new PathSettings();
        var expected = Path.Combine(_tempRoot, "documents");

        var actual = settings.ToAbsolutePath(_tempRoot, "documents");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToAbsolutePath_WithAbsolutePath_ReturnsSamePath()
    {
        var settings = new PathSettings();
        var absolute = Path.Combine(_tempRoot, "captures");

        var actual = settings.ToAbsolutePath(_tempRoot, absolute);

        Assert.Equal(absolute, actual);
    }

    [Fact]
    public void CreateHost_EnsuresDefaultDirectoriesExist()
    {
        var pathSettings = new PathSettings();

        using var host = AppInitializer.CreateHost(_tempRoot, pathSettings);

        Assert.True(Directory.Exists(Path.Combine(_tempRoot, pathSettings.LogsFolder)), "logs フォルダが作成されていません。");
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, pathSettings.ConfigDirectory)), "config フォルダが作成されていません。");
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, pathSettings.DocumentsDirectory)), "documents フォルダが作成されていません。");
        Assert.True(Directory.Exists(Path.Combine(_tempRoot, pathSettings.CapturesDirectory)), "captures フォルダが作成されていません。");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // テスト実行環境によっては削除に失敗しても問題ないため握りつぶす。
        }
    }
}
