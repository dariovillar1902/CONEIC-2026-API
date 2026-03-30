using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "Registrations",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPending",
                table: "Registrations",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "ParticipatedInJoreic",
                table: "Registrations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Registrations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "AmountPending",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "ParticipatedInJoreic",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Registrations");
        }
    }
}
