using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineBusTicketReservationSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPNRToBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PNR",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PNR",
                table: "Bookings");
        }
    }
}
