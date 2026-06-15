using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.AddColumn<string>(
                name: "Filial",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagedFaculties",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InterestedInMaccaferri",
                table: "Registrations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Observations",
                table: "Registrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DelegationName",
                table: "PaymentBatches",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "Assignments",
                table: "PaymentBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DelegateEmail",
                table: "PaymentBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsValidated",
                table: "PaymentBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NeedsInvoice",
                table: "PaymentBatches",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedAt",
                table: "PaymentBatches",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Filial",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ManagedFaculties",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InterestedInMaccaferri",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Observations",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Assignments",
                table: "PaymentBatches");

            migrationBuilder.DropColumn(
                name: "DelegateEmail",
                table: "PaymentBatches");

            migrationBuilder.DropColumn(
                name: "IsValidated",
                table: "PaymentBatches");

            migrationBuilder.DropColumn(
                name: "NeedsInvoice",
                table: "PaymentBatches");

            migrationBuilder.DropColumn(
                name: "ValidatedAt",
                table: "PaymentBatches");

            migrationBuilder.AlterColumn<string>(
                name: "DelegationName",
                table: "PaymentBatches",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "DelegationName", "Email", "Password", "Role" },
                values: new object[,]
                {
                    { 1, null, "dvillar@frba.utn.edu.ar", "admin", "admin" },
                    { 2, "UTN - Facultad Regional Buenos Aires", "delegate@frba.utn.edu.ar", "demo", "delegate" },
                    { 3, null, "test@visitor.com", "demo", "assistant" },
                    { 4, null, "spizzamus@frba.utn.edu.ar", "admin", "admin" },
                    { 5, null, "cpoggi@frba.utn.edu.ar", "admin", "admin" },
                    { 6, "UTN - Facultad Regional General Pacheco", "delegate@frgp.utn.edu.ar", "demo", "delegate" }
                });
        }
    }
}
