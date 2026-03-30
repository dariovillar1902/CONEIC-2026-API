using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "EndTime", "Location", "SpeakerId", "StartTime", "Title" },
                values: new object[] { 1, "Bienvenida al XVII CONEIC", new DateTime(2026, 10, 15, 10, 0, 0, 0, DateTimeKind.Unspecified), "Auditorio Principal", null, new DateTime(2026, 10, 15, 9, 0, 0, 0, DateTimeKind.Unspecified), "Apertura" });

            migrationBuilder.InsertData(
                table: "Speakers",
                columns: new[] { "Id", "Bio", "ImageUrl", "LinkedInUrl", "Name", "Title" },
                values: new object[,]
                {
                    { 1, "Experto en diseño sismorresistente.", "https://via.placeholder.com/150", "#", "Ing. Juan Pérez", "Especialista en Estructuras" },
                    { 2, "Investigadora en recursos hídricos.", "https://via.placeholder.com/150", "#", "Dra. María González", "Ingeniería Ambiental" }
                });

            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "EndTime", "Location", "SpeakerId", "StartTime", "Title" },
                values: new object[,]
                {
                    { 2, "Diseño moderno de puentes.", new DateTime(2026, 10, 15, 12, 0, 0, 0, DateTimeKind.Unspecified), "Auditorio Principal", 1, new DateTime(2026, 10, 15, 10, 30, 0, 0, DateTimeKind.Unspecified), "Charla Magistral: Puentes" },
                    { 3, "Introducción a Building Information Modeling.", new DateTime(2026, 10, 15, 16, 0, 0, 0, DateTimeKind.Unspecified), "Sala A", 2, new DateTime(2026, 10, 15, 14, 0, 0, 0, DateTimeKind.Unspecified), "Taller: BIM" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Activities",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Speakers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Speakers",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
