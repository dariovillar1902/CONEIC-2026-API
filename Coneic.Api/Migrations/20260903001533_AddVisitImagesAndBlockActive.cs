using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitImagesAndBlockActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "SelectableActivities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ActivityBlocks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "ActivityBlocks",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "ActivityBlocks",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsActive",
                value: false);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 101,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 102,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 103,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 104,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 105,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 106,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 107,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 108,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 109,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 110,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 201,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 202,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 203,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 204,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 205,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 206,
                column: "ImageUrl",
                value: null);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 401,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-01.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 402,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-02.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 403,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-03.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 404,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-04.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 405,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-05.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 406,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-06.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 407,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-07.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 408,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-08.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 409,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-09.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 410,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-10.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 411,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-11.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 412,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-12.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 413,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-13.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 414,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-14.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 415,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-15.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 416,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-16.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 417,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-17.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 418,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-18.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 419,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-19.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 420,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-20.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 421,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-21.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 422,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-22.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 423,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-23.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 424,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-24.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 425,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-25.jpg");

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 426,
                column: "ImageUrl",
                value: "/assets/visitas/visita-4-26.jpg");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "SelectableActivities");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ActivityBlocks");
        }
    }
}
