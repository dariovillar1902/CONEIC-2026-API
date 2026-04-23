using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class MockScheduleData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Remove old placeholder activities ──────────────────────────────
            migrationBuilder.DeleteData(table: "Activities", keyColumn: "Id", keyValue: 1);
            migrationBuilder.DeleteData(table: "Activities", keyColumn: "Id", keyValue: 2);
            migrationBuilder.DeleteData(table: "Activities", keyColumn: "Id", keyValue: 3);

            // ── Update existing speakers ───────────────────────────────────────
            migrationBuilder.UpdateData(
                table: "Speakers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Bio", "ImageUrl", "LinkedInUrl", "Name", "Title" },
                values: new object[] {
                    "Especialista en diseño sísmico y estructuras de gran escala con más de 20 años de trayectoria en obras de infraestructura.",
                    "https://randomuser.me/api/portraits/men/32.jpg",
                    "#",
                    "Ing. Roberto Fernández",
                    "Especialista en Estructuras y Sismología"
                });

            migrationBuilder.UpdateData(
                table: "Speakers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Bio", "ImageUrl", "LinkedInUrl", "Name", "Title" },
                values: new object[] {
                    "Investigadora en hidráulica e ingeniería ambiental. Autora de numerosas publicaciones sobre gestión de recursos hídricos en la cuenca del Plata.",
                    "https://randomuser.me/api/portraits/women/44.jpg",
                    "#",
                    "Dra. Laura Gómez",
                    "Ingeniería Hidráulica y Ambiental"
                });

            // ── Add new speakers ───────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Speakers",
                columns: new[] { "Id", "Bio", "ImageUrl", "LinkedInUrl", "Name", "Title" },
                values: new object[,]
                {
                    {
                        3,
                        "Referente nacional en implementación de BIM en proyectos de infraestructura vial y edilicia. Docente en UTN y UBA.",
                        "https://randomuser.me/api/portraits/men/55.jpg",
                        "#",
                        "Ing. Diego Torres",
                        "BIM y Tecnología en la Construcción"
                    },
                    {
                        4,
                        "Investigadora del CONICET en nuevos materiales constructivos: hormigones especiales, geosintéticos y nanotecnología aplicada.",
                        "https://randomuser.me/api/portraits/women/61.jpg",
                        "#",
                        "Ing. Valeria Ríos",
                        "Nuevos Materiales y Nanotecnología"
                    }
                });

            // ── MARTES 4 de agosto 2026 ────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "EndTime", "Location", "SpeakerId", "StartTime", "Title" },
                values: new object[,]
                {
                    { 1,  "Registro de asistentes y entrega de kits de bienvenida.",
                          new DateTime(2026, 8, 4, 10, 0, 0), "Hall Principal — UTN FRBA", null,
                          new DateTime(2026, 8, 4,  9, 0, 0), "Acreditación y Bienvenida" },

                    { 2,  "Conferencia magistral de apertura con visión integral sobre los desafíos actuales y futuros de la ingeniería civil argentina.",
                          new DateTime(2026, 8, 4, 13, 0, 0), "Auditorio Principal", 1,
                          new DateTime(2026, 8, 4, 10, 0, 0), "Charla Inaugural: La Ingeniería Civil del Futuro" },

                    { 3,  "",
                          new DateTime(2026, 8, 4, 14, 30, 0), "Patio del Campus", null,
                          new DateTime(2026, 8, 4, 13, 0, 0), "Almuerzo" },

                    { 4,  "Aplicación de BIM en proyectos de infraestructura vial y edilicia: flujos de trabajo, software y casos reales.",
                          new DateTime(2026, 8, 4, 17, 0, 0), "Laboratorio de Informática", 3,
                          new DateTime(2026, 8, 4, 14, 30, 0), "Taller: BIM aplicado a Infraestructura" },

                    { 5,  "Criterios de diseño sísmico, normativa CIRSOC y análisis de estructuras ante solicitaciones dinámicas.",
                          new DateTime(2026, 8, 4, 18, 30, 0), "Sala de Estructuras", 1,
                          new DateTime(2026, 8, 4, 17, 0, 0), "Taller: Diseño Sismorresistente" },

                    { 6,  "Cena de inicio del congreso con networking y presentación de delegaciones de todo el país.",
                          new DateTime(2026, 8, 4, 22, 30, 0), "Salón de Eventos — Sede Central", null,
                          new DateTime(2026, 8, 4, 20, 0, 0), "Cena de Bienvenida" },
                });

            // ── MIÉRCOLES 5 de agosto 2026 ────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "EndTime", "Location", "SpeakerId", "StartTime", "Title" },
                values: new object[,]
                {
                    { 7,  "",
                          new DateTime(2026, 8, 5,  9, 30, 0), "Hall Principal", null,
                          new DateTime(2026, 8, 5,  9, 0, 0), "Acreditación" },

                    { 8,  "Panorama actual de la infraestructura hídrica en Argentina: cuencas, embalses y gestión del agua en un contexto de cambio climático.",
                          new DateTime(2026, 8, 5, 12, 0, 0), "Auditorio Principal", 2,
                          new DateTime(2026, 8, 5,  9, 30, 0), "Charla Magistral: Infraestructura Hídrica" },

                    { 9,  "Panel multidisciplinario sobre las obras más emblemáticas de Buenos Aires: subterráneos, autopistas, Puerto Madero y obras hidráulicas.",
                          new DateTime(2026, 8, 5, 13, 30, 0), "Auditorio Principal", null,
                          new DateTime(2026, 8, 5, 12, 0, 0), "Panel: Obras Emblemáticas de Buenos Aires" },

                    { 10, "",
                          new DateTime(2026, 8, 5, 15, 0, 0), "Patio del Campus", null,
                          new DateTime(2026, 8, 5, 13, 30, 0), "Almuerzo" },

                    { 11, "Recorrido técnico guiado por las instalaciones y obras en curso del Puerto de Buenos Aires.",
                          new DateTime(2026, 8, 5, 19, 0, 0), "Puerto Madero — Dique 4", null,
                          new DateTime(2026, 8, 5, 15, 0, 0), "Visita Técnica: Puerto de Buenos Aires" },

                    { 12, "Noche de integración federal con música, gastronomía regional y actividades culturales.",
                          new DateTime(2026, 8, 5, 23, 0, 0), "Salón de Eventos — Sede Central", null,
                          new DateTime(2026, 8, 5, 20, 30, 0), "Peña Federal" },
                });

            // ── JUEVES 6 de agosto 2026 ───────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "EndTime", "Location", "SpeakerId", "StartTime", "Title" },
                values: new object[,]
                {
                    { 13, "",
                          new DateTime(2026, 8, 6,  9, 30, 0), "Hall Principal", null,
                          new DateTime(2026, 8, 6,  9, 0, 0), "Acreditación" },

                    { 14, "Exploración de hormigones especiales, geosintéticos y nanotecnología aplicados a la construcción moderna.",
                          new DateTime(2026, 8, 6, 12, 30, 0), "Laboratorio de Materiales", 4,
                          new DateTime(2026, 8, 6,  9, 30, 0), "Taller: Nuevos Materiales Constructivos" },

                    { 15, "",
                          new DateTime(2026, 8, 6, 14, 0, 0), "Patio del Campus", null,
                          new DateTime(2026, 8, 6, 12, 30, 0), "Almuerzo" },

                    { 16, "Intervención constructiva voluntaria en una comunidad local de la zona sur de Buenos Aires.",
                          new DateTime(2026, 8, 6, 17, 0, 0), "Comunidad La Paloma — Zona Sur", null,
                          new DateTime(2026, 8, 6, 14, 0, 0), "Actividad Solidaria" },

                    { 17, "Reunión de representantes universitarios para tratar agenda de ANEIC, elección de sede 2027 y resoluciones.",
                          new DateTime(2026, 8, 6, 19, 0, 0), "Auditorio Principal", null,
                          new DateTime(2026, 8, 6, 17, 0, 0), "Asamblea ANEIC" },

                    { 18, "Noche temática con folklore porteño, tango y gastronomía bonaerense.",
                          new DateTime(2026, 8, 6, 23, 0, 0), "Salón de Eventos — Sede Central", null,
                          new DateTime(2026, 8, 6, 20, 30, 0), "Noche Temática: Folklore Porteño" },
                });

            // ── VIERNES 7 de agosto 2026 ──────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "EndTime", "Location", "SpeakerId", "StartTime", "Title" },
                values: new object[,]
                {
                    { 19, "",
                          new DateTime(2026, 8, 7,  9, 30, 0), "Hall Principal", null,
                          new DateTime(2026, 8, 7,  9, 0, 0), "Acreditación" },

                    { 20, "Reflexión sobre sostenibilidad, cambio climático y el rol del ingeniero civil en la transición energética.",
                          new DateTime(2026, 8, 7, 12, 0, 0), "Auditorio Principal", 2,
                          new DateTime(2026, 8, 7,  9, 30, 0), "Charla de Cierre: Sostenibilidad en la Ingeniería" },

                    { 21, "",
                          new DateTime(2026, 8, 7, 13, 30, 0), "Patio del Campus", null,
                          new DateTime(2026, 8, 7, 12, 0, 0), "Almuerzo" },

                    { 22, "Actividades recreativas, concursos y sorteos de premios especiales para los asistentes.",
                          new DateTime(2026, 8, 7, 16, 0, 0), "Patio del Campus", null,
                          new DateTime(2026, 8, 7, 14, 0, 0), "Actividad Recreativa y Sorteos" },

                    { 23, "Ceremonia oficial de cierre del XVIII CONEIC: entrega de diplomas, reconocimientos y palabras de despedida.",
                          new DateTime(2026, 8, 7, 18, 0, 0), "Auditorio Principal", null,
                          new DateTime(2026, 8, 7, 16, 0, 0), "Acto de Clausura" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove mock activities
            for (int i = 1; i <= 23; i++)
                migrationBuilder.DeleteData(table: "Activities", keyColumn: "Id", keyValue: i);

            // Remove speakers 3 and 4
            migrationBuilder.DeleteData(table: "Speakers", keyColumn: "Id", keyValue: 3);
            migrationBuilder.DeleteData(table: "Speakers", keyColumn: "Id", keyValue: 4);

            // Restore original speakers 1 and 2
            migrationBuilder.UpdateData(
                table: "Speakers", keyColumn: "Id", keyValue: 1,
                columns: new[] { "Bio", "ImageUrl", "LinkedInUrl", "Name", "Title" },
                values: new object[] { "Experto en diseño sismorresistente.", "https://via.placeholder.com/150", "#", "Ing. Juan Pérez", "Especialista en Estructuras" });

            migrationBuilder.UpdateData(
                table: "Speakers", keyColumn: "Id", keyValue: 2,
                columns: new[] { "Bio", "ImageUrl", "LinkedInUrl", "Name", "Title" },
                values: new object[] { "Investigadora en recursos hídricos.", "https://via.placeholder.com/150", "#", "Dra. María González", "Ingeniería Ambiental" });

            // Restore original 3 activities
            migrationBuilder.InsertData(
                table: "Activities",
                columns: new[] { "Id", "Description", "EndTime", "Location", "SpeakerId", "StartTime", "Title" },
                values: new object[,]
                {
                    { 1, "Bienvenida al XVII CONEIC", new DateTime(2026, 10, 15, 10, 0, 0), "Auditorio Principal", null, new DateTime(2026, 10, 15, 9, 0, 0), "Apertura" },
                    { 2, "Diseño moderno de puentes.", new DateTime(2026, 10, 15, 12, 0, 0), "Auditorio Principal", 1,    new DateTime(2026, 10, 15, 10, 30, 0), "Charla Magistral: Puentes" },
                    { 3, "Introducción a Building Information Modeling.", new DateTime(2026, 10, 15, 16, 0, 0), "Sala A", 2, new DateTime(2026, 10, 15, 14, 0, 0), "Taller: BIM" },
                });
        }
    }
}
