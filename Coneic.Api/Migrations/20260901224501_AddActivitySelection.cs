using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Coneic.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddActivitySelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    MaxSelections = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityBlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivitySelections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserEmail = table.Column<string>(type: "TEXT", nullable: false),
                    BlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityId = table.Column<int>(type: "INTEGER", nullable: false),
                    SelectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySelections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SelectableActivities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BlockId = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Speaker = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SelectableActivities", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ActivityBlocks",
                columns: new[] { "Id", "Category", "MaxSelections", "Name", "Note" },
                values: new object[,]
                {
                    { 1, "VisitaTecnica", 1, "Visita Técnica", "Elegí una visita técnica. Cupos y horarios definitivos a confirmar." },
                    { 2, "TallerCharla", 1, "Talleres y Charlas Simultáneas", "Agrupación provisoria de demo — los bloques horarios reales todavía se están definiendo con Académica y GyP." }
                });

            migrationBuilder.InsertData(
                table: "SelectableActivities",
                columns: new[] { "Id", "BlockId", "Capacity", "Code", "Description", "Speaker", "Title" },
                values: new object[,]
                {
                    { 101, 2, 40, "1.01", "El objetivo del taller es acompañar a estudiantes de la carrera con la duda de qué sucede una vez recibido, cómo posicionarse en el mercado laboral y qué posibilidades hay hoy en día para ingenieros civiles.", "Ing. Axel Colantuono, CRIBA", "¿Y ahora qué?" },
                    { 102, 2, 40, "1.02", "Introducción accesible a la IA conversacional, con foco en Claude: qué es un modelo de lenguaje, por qué puede alucinar y cómo usarla de forma más eficiente y responsable en la formación y el ejercicio profesional.", "Dr. Felipe Ruiz Bruzzone", "Más allá del chat: fundamentos y aplicaciones prácticas de la IA" },
                    { 103, 2, 40, "1.03", "Identificar causas probables de derrumbes. Prevenciones.", "Ing. Claudio Silvio Risetto", "Derrumbes. Casos." },
                    { 104, 2, 40, "1.04", "Presentación de casos reales de diagnóstico estructural mediante tecnologías avanzadas. Cómo identificar patologías, comprender el comportamiento del hormigón y definir estrategias técnicas de intervención eficiente.", "Ing. Julio Cesar Tomás, ITAC Laboratorio", "Cómo la tecnología está cambiando la forma de diagnosticar estructuras de hormigón" },
                    { 105, 2, 40, "1.05", "Cómo realizar la ingeniería de detalle en instalaciones MEP para obtener modelos digitales listos para fabricación. Detección temprana de interferencias y tolerancias de montaje para asegurar que estructura e instalaciones convivan en armonía.", "Ing. María Anahí Zoratto Macia", "Ingeniería de detalle y coordinación de instalaciones: diseñar en la computadora para construir sin errores en la obra" },
                    { 106, 2, 40, "1.06", "Introducción al flujo de trabajo del ingeniero MEP dentro del entorno BIM. Transición de esquemas tradicionales 2D a modelos 3D interactivos para coordinar conductos, tuberías y cables antes de ir a obra, evitando errores millonarios.", "Ing. María Anahí Zoratto Macia", "Del plano 2D al modelo 3D: cómo la ingeniería MEP digital transforma el diseño de instalaciones" },
                    { 107, 2, 40, "1.07", "Criterio técnico en hormigón armado: análisis de cómo la simplificación del Bloque Equivalente de Whitney rige las ecuaciones clave de ACI 318 y CIRSOC 201.", "Ing. Bryan Alejandro Castañón, ANEIC Guatemala", "De la distribución real de tensiones al modelo simplificado: impacto en la práctica del Ingeniero Civil" },
                    { 108, 2, 40, "1.08", "Simulación operativa en software de presas hidroeléctricas con juego de roles para la toma de decisiones en generación energética, control de crecidas y gestión ambiental.", "Ing. Gerardo Burdisso", "Sistema de complejos Hidroeléctricos COMAHUE - Río Limay: taller de práctica de roles en sectores de interés público, privado y sociedad" },
                    { 109, 2, 40, "1.09", "Práctica en equipo sobre un caso simulado de obra civil en tiempo real: identificación de impactos ambientales, aplicación de jerarquías de mitigación y resolución de imprevistos en obra.", "Lic. Romina Favilla", "La obra que no contamina: taller de gestión ambiental para ingenieros civiles" },
                    { 110, 2, 40, "1.10", "Un edificio, un puente, una presa: obras distintas que se sostienen —o se caen— por lo mismo. En este taller no vas a escuchar la teoría, la vas a construir con tus manos y en equipo. Diseñá tu estructura sobre un terreno real, cargala hasta el límite y descubrí en vivo cómo el agua cambia las reglas del juego.", "Ing. Gustavo Daniel Mosquera", "¿Aguanta o no Aguanta? Taller de Geotecnia en Acción" },
                    { 201, 2, 40, "2.01", "Tecnología aplicada, fundamentos y entorno de la metodología BIM, y su integración con la gestión eficiente en obra.", "Ing. Martín Magallanes", "El ecosistema BIM en la Ingeniería Civil: de los fundamentos teóricos a la tecnología aplicada" },
                    { 202, 2, 40, "2.02", "Integración del conocimiento técnico y el derecho a la vida silvestre en la infraestructura: impacto de los atropellamientos de fauna y soluciones de ingeniería aplicada.", "Lic. Nicolás Lodeiro Ocampo", "Rutas y Fauna Silvestre, es tiempo de pensar a todas las vidas" },
                    { 203, 2, 40, "2.03", "Por qué el criterio y la conciencia son el verdadero valor diferencial frente a la automatización técnica, y herramientas para liderar tu propio desarrollo en la próxima década.", "Ing. Joaquín N. Perrig", "Aprender a ApreHender: el diferencial humano frente al avance tecnológico" },
                    { 204, 2, 40, "2.04", "Desafíos técnicos y operativos superados en proyectos reales, implementación de la tecnología a escala nacional y lecciones aprendidas.", "Ing. Rocío Gentico, Techint", "Impresión 3D de hormigón en Argentina: desafíos y aprendizajes en proyectos reales" },
                    { 205, 2, 40, "2.05", "Gestión sustentable, técnicas avanzadas de demolición según escala de obra y economía circular con residuos de construcción y demolición (RCD).", "Ing. Maximiliano Mauriño, Grupo Mitre", "Demolición, Excavación y Reciclaje en Obras de Gran Escala" },
                    { 206, 2, 40, "2.06", "Catalogación de fallas, evolución constructiva e ingeniería forense: análisis exhaustivo de daños estructurales, geomorfológicos y socioeconómicos tras el evento sísmico.", "Ing. Gustavo Delgado", "Terremoto 2026 en Venezuela: ingeniería forense y lecciones aprendidas" },
                    { 401, 1, 30, "4.01", "Pionera de la Energía Nuclear en América Latina.", null, "Complejo Nuclear Atucha" },
                    { 402, 1, 30, "4.02", "Ingeniería portuaria y logística multimodal a gran escala.", null, "Puerto Buenos Aires" },
                    { 403, 1, 30, "4.03", "Ingeniería ambiental y gestión de residuos a escala metropolitana.", null, "Complejo Ambiental Norte III" },
                    { 404, 1, 30, "4.04", "1.950.000 m³/día de agua tratada para la región metropolitana.", null, "Planta Potabilizadora Gral. Belgrano" },
                    { 405, 1, 30, "4.05", "237.000 m³/día de líquidos tratados. Sirve a gran parte del partido de La Matanza.", null, "Planta Depuradora Sudoeste" },
                    { 406, 1, 30, "4.06", "23.328 m³/día de efluentes tratados. Saneamiento directo para 90.000 habitantes.", null, "Planta Depuradora Lanús" },
                    { 407, 1, 30, "4.07", "48.000 m³/día de efluentes tratados. Operativa para Ezeiza y parte de Esteban Echeverría.", null, "Planta Depuradora El Jagüel" },
                    { 408, 1, 30, "4.08", "77.760 m³/día de efluentes tratados. Beneficia a 270.000 habitantes.", null, "Planta Depuradora Fiorito" },
                    { 409, 1, 30, "4.09", "Elementos de hormigón premoldeado. Tecnología y estandarización a gran escala para la industria de la construcción.", null, "Planta de Hormigón Sola" },
                    { 410, 1, 30, "4.10", "30.240 m³/día de efluentes tratados. Sirve a Hurlingham, Ituzaingó, Morón y Tres de Febrero.", null, "Planta Depuradora Hurlingham" },
                    { 411, 1, 30, "4.11", "Producción y logística de hormigón elaborado. El detrás de la dosificación y distribución para obras de gran porte.", null, "Fenomix" },
                    { 412, 1, 30, "4.12", "Infraestructura costera y portuaria. Soluciones de ingeniería para el almacenamiento y movimiento de embarcaciones.", null, "Guardería Náutica Neptuno" },
                    { 413, 1, 30, "4.13", "Un proyecto de GNV Group para transformar la vida urbana en Puerto Madero.", null, "Madero Harbour" },
                    { 414, 1, 30, "4.14", "Primera en su tipo adquirida por una empresa constructora en Argentina.", null, "Impresora 3D de Concreto" },
                    { 415, 1, 30, "4.15", "Una obra de usos mixtos que integra residencias de lujo, un hotel cinco estrellas y 7.600 m² de amenidades de primer nivel.", null, "Proyecto Udaondo" },
                    { 416, 1, 30, "4.16", "Un proyecto inmobiliario en una de las zonas con mayor proyección de Buenos Aires, que integra el diseño urbano y entorno natural.", null, "Quartier Bajo Belgrano" },
                    { 417, 1, 30, "4.17", "Será la primera autopista parque de la Ciudad, diseñada para optimizar la movilidad y el espacio público.", null, "Autopista Parque Dellepiane" },
                    { 418, 1, 30, "4.18", "Complejo residencial que combina departamentos con grandes espacios verdes y servicios premium propios de un barrio cerrado dentro de la ciudad.", null, "MilAires" },
                    { 419, 1, 30, "4.19", "Ampliación y renovación del puente que conecta el Parque de la Innovación y la Ciudad Universitaria.", null, "Puente Labruna" },
                    { 420, 1, 30, "4.20", "Nueva conexión entre la Ciudad y el Río de la Plata que mejorará la movilidad e integrará el entorno.", null, "Anillo Pampa" },
                    { 421, 1, 30, "4.21", "Nueva torre de 17 pisos y 4 subsuelos en Belgrano: departamentos de hasta 555 m² con pileta propia, spa, gimnasio y pileta en la terraza.", null, "Ñlet Loreto" },
                    { 422, 1, 30, "4.22", "Megaobra histórica del Más Monumental: techado integral, tribuna 360° y ampliación de capacidad a 101.000 espectadores con cerca de 100 columnas perimetrales.", null, "Estadio Más Monumental" },
                    { 423, 1, 30, "4.23", "Modernización integral y preservación patrimonial: un complejo emblemático de 42.000 m² compuesto por una torre de 12 pisos, cuerpo bajo y 6 subsuelos.", null, "Centro Cultural San Martín" },
                    { 424, 1, 30, "4.24", "Nueva infraestructura educativa de 2.900 m² en el Barrio 31: diseño bioclimático, sistemas constructivos industrializados y espacios de alta funcionalidad pedagógica.", null, "Escuela Indira Gandhi" },
                    { 425, 1, 30, "4.25", "Obra residencial de 13.677 m² (2, 3 y 4 ambientes). Trabajos actuales: estructura en 5° piso, inicio de albañilería y grúa torre con trepado.", null, "Complejo Dorrego Plaza" },
                    { 426, 1, 30, "4.26", "70.000 m² cubiertos. Desarrollo de usos mixtos (hotel de lujo, viviendas, oficinas y plaza pública) con un 35% de factor de ocupación y 65% de espacio público.", null, "JN4016" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySelections_UserEmail_BlockId",
                table: "ActivitySelections",
                columns: new[] { "UserEmail", "BlockId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityBlocks");

            migrationBuilder.DropTable(
                name: "ActivitySelections");

            migrationBuilder.DropTable(
                name: "SelectableActivities");
        }
    }
}
