using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentFileManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCheckedToCheckItemDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 既存レコードはチェック済みとして扱うため、デフォルト値をtrueに設定
            migrationBuilder.AddColumn<bool>(
                name: "IsChecked",
                table: "CheckItemDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsChecked",
                table: "CheckItemDocuments");
        }
    }
}
