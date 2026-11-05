using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineBusTicketReservationSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationReasonAndBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "Cancellations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "Cancellations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "Cancellations");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "Cancellations");
        }
    }
}
