using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <summary>
    /// Sede y horario de cada Taller / Charla Simultánea / Solidaria, según la
    /// pestaña "Elección Definitiva TCHS - Cupos" (2026-10-07). Se muestran en
    /// la elección y en "Mi Cronograma". Columnas nullable: las Solidarias
    /// todavía no tienen hora confirmada.
    /// </summary>
    public partial class AddSelectableActivityVenueAndTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "Venue", table: "SelectableActivities", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "StartTime", table: "SelectableActivities", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "EndTime", table: "SelectableActivities", type: "TEXT", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Venue", table: "SelectableActivities");
            migrationBuilder.DropColumn(name: "StartTime", table: "SelectableActivities");
            migrationBuilder.DropColumn(name: "EndTime", table: "SelectableActivities");
        }
    }
}
