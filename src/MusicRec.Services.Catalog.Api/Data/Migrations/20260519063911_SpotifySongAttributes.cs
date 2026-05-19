using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MusicRec.Services.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SpotifySongAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "EnergyLevel",
                table: "Songs",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "MoodTag",
                table: "Songs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TempoTag",
                table: "Songs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnergyLevel",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "MoodTag",
                table: "Songs");

            migrationBuilder.DropColumn(
                name: "TempoTag",
                table: "Songs");
        }
    }
}
