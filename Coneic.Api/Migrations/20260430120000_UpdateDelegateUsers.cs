using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDelegateUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing FRBA delegate: fix email and ensure delegationName matches form (no "de")
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "DelegationName" },
                values: new object[] { "delegate@frba.utn.edu.ar", "UTN - Facultad Regional Buenos Aires" });

            // Add delegate for Facultad Regional General Pacheco
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Email", "Password", "Role", "DelegationName" },
                values: new object[] { 6, "delegate@frgp.utn.edu.ar", "demo", "delegate", "UTN - Facultad Regional General Pacheco" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Email", "DelegationName" },
                values: new object[] { "delegate@utn.edu.ar", "UTN - Facultad Regional Buenos Aires" });
        }
    }
}
