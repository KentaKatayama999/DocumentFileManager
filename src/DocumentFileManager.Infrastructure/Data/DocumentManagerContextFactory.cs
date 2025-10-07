using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocumentFileManager.Infrastructure.Data;

/// <summary>
/// EF Core マイグレーション用のデザインタイムファクトリ
/// dotnet ef コマンド実行時に DbContext を生成する
/// </summary>
public class DocumentManagerContextFactory : IDesignTimeDbContextFactory<DocumentManagerContext>
{
    public DocumentManagerContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DocumentManagerContext>();

        // デザインタイム用のダミー接続文字列
        // 実際のアプリケーションではプロジェクトルートの workspace.db を使用
        optionsBuilder.UseSqlite("Data Source=workspace.db");

        return new DocumentManagerContext(optionsBuilder.Options);
    }
}
