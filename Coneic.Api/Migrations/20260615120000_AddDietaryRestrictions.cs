using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    public partial class AddDietaryRestrictions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DietaryRestrictions",
                table: "Registrations",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DietaryRestrictions",
                table: "Registrations");
        }
    }
}
