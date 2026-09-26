using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <summary>
    /// Pedido por seguros (auditorio y visitas) — el formulario de
    /// inscripción original no pedía fecha de nacimiento. Se agrega como
    /// columna nullable; se completa después vía un paso obligatorio antes
    /// de la Elección de Actividades (decisión del equipo, 2026-09-26).
    /// </summary>
    public partial class AddRegistrationBirthDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<System.DateTime>(
                name: "BirthDate",
                table: "Registrations",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "BirthDate", table: "Registrations");
        }
    }
}
