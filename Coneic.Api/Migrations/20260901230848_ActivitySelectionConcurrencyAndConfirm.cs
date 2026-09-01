using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class ActivitySelectionConcurrencyAndConfirm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TakenCount",
                table: "SelectableActivities",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "ActivitySelections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsConfirmed",
                table: "ActivitySelections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 101,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 102,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 103,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 104,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 105,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 106,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 107,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 108,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 109,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 110,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 201,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 202,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 203,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 204,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 205,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 206,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 401,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 402,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 403,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 404,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 405,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 406,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 407,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 408,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 409,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 410,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 411,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 412,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 413,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 414,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 415,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 416,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 417,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 418,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 419,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 420,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 421,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 422,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 423,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 424,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 425,
                column: "TakenCount",
                value: 0);

            migrationBuilder.UpdateData(
                table: "SelectableActivities",
                keyColumn: "Id",
                keyValue: 426,
                column: "TakenCount",
                value: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TakenCount",
                table: "SelectableActivities");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "ActivitySelections");

            migrationBuilder.DropColumn(
                name: "IsConfirmed",
                table: "ActivitySelections");
        }
    }
}
