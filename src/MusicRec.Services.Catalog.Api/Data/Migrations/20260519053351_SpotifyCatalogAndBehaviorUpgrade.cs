using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRec.Services.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpotifyCatalogAndBehaviorUpgrade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalUrl",
                table: "Songs",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Songs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SpotifyTrackId",
                table: "Songs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserBehaviorEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SongId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContextType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CompletionRate = table.Column<double>(type: "float", nullable: true),
                    PlaybackPositionSeconds = table.Column<int>(type: "int", nullable: true),
                    PlaybackDurationSeconds = table.Column<int>(type: "int", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBehaviorEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBehaviorEvents_Songs_SongId",
                        column: x => x.SongId,
                        principalTable: "Songs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Songs_SpotifyTrackId",
                table: "Songs",
                column: "SpotifyTrackId",
                unique: true,
                filter: "[SpotifyTrackId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorEvents_EventType_OccurredAtUtc",
                table: "UserBehaviorEvents",
                columns: new[] { "EventType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorEvents_SongId_OccurredAtUtc",
                table: "UserBehaviorEvents",
                columns: new[] { "SongId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBehaviorEvents_UserId_OccurredAtUtc",
                table: "UserBehaviorEvents",
                columns: new[] { "UserId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBehaviorEvents");

            migrationBuilder.DropIndex(
                name: "IX_Songs_SpotifyTrackId",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "ExternalUrl",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "SpotifyTrackId",
                table: "Songs");
        }
    }
}
