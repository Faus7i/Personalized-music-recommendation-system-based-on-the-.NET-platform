using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRec.Services.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpotifyUriForPlayback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SpotifyUri",
                table: "Songs",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpotifyUri",
                table: "Songs");
        }
    }
}
