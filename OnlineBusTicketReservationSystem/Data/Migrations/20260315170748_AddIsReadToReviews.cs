using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineBusTicketReservationSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsReadToReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "Reviews",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "Reviews");
        }
    }
}
