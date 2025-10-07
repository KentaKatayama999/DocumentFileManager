using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentFileManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckItems_CheckItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CheckItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    FileType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheckItemDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CheckItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    DocumentId = table.Column<int>(type: "INTEGER", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CaptureFile = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckItemDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckItemDocuments_CheckItems_CheckItemId",
                        column: x => x.CheckItemId,
                        principalTable: "CheckItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CheckItemDocuments_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckItemDocuments_CheckItemId_DocumentId_LinkedAt",
                table: "CheckItemDocuments",
                columns: new[] { "CheckItemId", "DocumentId", "LinkedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CheckItemDocuments_DocumentId",
                table: "CheckItemDocuments",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckItems_ParentId",
                table: "CheckItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CheckItems_Path",
                table: "CheckItems",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileType",
                table: "Documents",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_RelativePath",
                table: "Documents",
                column: "RelativePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckItemDocuments");

            migrationBuilder.DropTable(
                name: "CheckItems");

            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
