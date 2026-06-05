using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRec.Services.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlaylistVisibilityAndManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Playlists",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Playlists");
        }
    }
}
