using DocumentFileManager.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentFileManager.Infrastructure.Data;

/// <summary>
/// 資料保存アプリのデータベースコンテキスト
/// SQLiteデータベース（workspace.db）を使用してプロジェクト単位でデータを管理
/// </summary>
public class DocumentManagerContext : DbContext
{
    /// <summary>チェックリスト項目</summary>
    public DbSet<CheckItem> CheckItems { get; set; } = null!;

    /// <summary>資料ファイル</summary>
    public DbSet<Document> Documents { get; set; } = null!;

    /// <summary>チェック項目と資料の紐づけ</summary>
    public DbSet<CheckItemDocument> CheckItemDocuments { get; set; } = null!;

    public DocumentManagerContext(DbContextOptions<DocumentManagerContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CheckItem の設定
        modelBuilder.Entity<CheckItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Path)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Label)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>(); // Enum を int で保存

            // 自己参照リレーションシップ（親子構造）
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict); // 親削除時は子を削除しない

            // インデックス
            entity.HasIndex(e => e.Path)
                .IsUnique();

            entity.HasIndex(e => e.ParentId);
        });

        // Document の設定
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.RelativePath)
                .IsRequired()
                .HasMaxLength(1000);

            entity.Property(e => e.FileType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.AddedAt)
                .IsRequired();

            // インデックス
            entity.HasIndex(e => e.RelativePath)
                .IsUnique();

            entity.HasIndex(e => e.FileType);
        });

        // CheckItemDocument の設定（中間テーブル）
        modelBuilder.Entity<CheckItemDocument>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.LinkedAt)
                .IsRequired();

            entity.Property(e => e.CaptureFile)
                .HasMaxLength(1000);

            // CheckItem との多対多リレーションシップ
            entity.HasOne(e => e.CheckItem)
                .WithMany(e => e.LinkedDocuments)
                .HasForeignKey(e => e.CheckItemId)
                .OnDelete(DeleteBehavior.Cascade); // チェック項目削除時は紐づけも削除

            // Document との多対多リレーションシップ
            entity.HasOne(e => e.Document)
                .WithMany(e => e.LinkedCheckItems)
                .HasForeignKey(e => e.DocumentId)
                .OnDelete(DeleteBehavior.Cascade); // 資料削除時は紐づけも削除

            // 複合インデックス（同一項目-資料ペアの重複を防ぐ）
            entity.HasIndex(e => new { e.CheckItemId, e.DocumentId, e.LinkedAt })
                .IsUnique();
        });
    }
}
