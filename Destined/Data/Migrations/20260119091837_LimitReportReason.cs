using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Destined.Data.Migrations
{
    /// <inheritdoc />
    public partial class LimitReportReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Truncate existing data to avoid data loss error
            migrationBuilder.Sql("UPDATE TicketReports SET Reason = LEFT(Reason, 100) WHERE LEN(Reason) > 100");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "TicketReports",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "TicketReports",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
