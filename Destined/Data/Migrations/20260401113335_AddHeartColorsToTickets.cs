using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Destined.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHeartColorsToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeartColor",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LikeCountColor",
                table: "Tickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeartColor",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "LikeCountColor",
                table: "Tickets");
        }
    }
}
