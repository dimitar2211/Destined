using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Destined.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAllowCommentsToTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowComments",
                table: "Tickets",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowComments",
                table: "Tickets");
        }
    }
}
