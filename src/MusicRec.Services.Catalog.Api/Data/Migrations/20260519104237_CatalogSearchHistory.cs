using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRec.Services.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatalogSearchHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserSearchHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Keyword = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedKeyword = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SearchType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    SearchedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSearchHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSearchHistories_UserId_NormalizedKeyword",
                table: "UserSearchHistories",
                columns: new[] { "UserId", "NormalizedKeyword" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSearchHistories_UserId_SearchedAtUtc",
                table: "UserSearchHistories",
                columns: new[] { "UserId", "SearchedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSearchHistories");
        }
    }
}
