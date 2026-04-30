using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class ForceFixDelegateEmailsV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL so this is idempotent regardless of prior migration state.
            // Fix FRBA delegate: update email and ensure DelegationName has no "de"
            migrationBuilder.Sql(
                @"UPDATE Users
                  SET Email = 'delegate@frba.utn.edu.ar',
                      DelegationName = 'UTN - Facultad Regional Buenos Aires'
                  WHERE Id = 2;");

            // Insert FRGP delegate if not already present
            migrationBuilder.Sql(
                @"INSERT INTO Users (Id, Email, Password, Role, DelegationName)
                  SELECT 6, 'delegate@frgp.utn.edu.ar', 'demo', 'delegate', 'UTN - Facultad Regional General Pacheco'
                  WHERE NOT EXISTS (SELECT 1 FROM Users WHERE Id = 6);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE Users
                  SET Email = 'delegate@utn.edu.ar',
                      DelegationName = 'UTN - Facultad Regional de Buenos Aires'
                  WHERE Id = 2;");

            migrationBuilder.Sql("DELETE FROM Users WHERE Id = 6;");
        }
    }
}
