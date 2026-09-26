using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Coneic.Api.Migrations
{
    /// <summary>
    /// Reconstruye por completo el catálogo de Talleres, Charlas Simultáneas y
    /// Solidarias siguiendo la "Guía de Elección de Actividades Académicas v1"
    /// (PDF) y los cupos de "Actividades Académicas PyD.xlsx" — decisión tomada
    /// en la reunión del 2026-09-24: se descarta todo lo cargado antes (podía
    /// tener códigos duplicados/desactualizados) y se recarga desde cero.
    ///
    /// Agrega también SelectableActivities.Family: enlaza cada Taller
    /// (1.01–1.17) con las Charlas Simultáneas (2.01–2.08) que habilita, según
    /// las 3 "familias" del PDF. Las Solidarias (3.01–3.12) no tienen familia
    /// — se eligen de forma independiente.
    /// </summary>
    public partial class ReloadTalleresSimultaneasSolidarias : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Family",
                table: "SelectableActivities",
                type: "INTEGER",
                nullable: true);

            // ── Borrar selecciones de prueba existentes en bloques 2/3/4 ────────
            // Solo hay 6, todas de cuentas internas (web@ / prensa@) — sin
            // asistentes reales todavía. Se borran antes que las actividades
            // para no dejar ActivityId apuntando a filas que van a desaparecer.
            migrationBuilder.Sql("DELETE FROM ActivitySelections WHERE BlockId IN (2, 3, 4);");

            // ── Borrar el catálogo viejo de Talleres/Simultáneas/Solidarias ─────
            migrationBuilder.Sql("DELETE FROM SelectableActivities WHERE BlockId IN (2, 3, 4);");

            // ── TALLERES (bloque 2) — códigos 1.01 a 1.17 ───────────────────────
            migrationBuilder.InsertData(
                table: "SelectableActivities",
                columns: new[] { "Id", "BlockId", "Family", "Code", "Capacity", "Title", "Speaker", "Description" },
                values: new object[,]
                {
                    { 500, 2, 1, "1.01", 30,
                      "Pedir no es debilidad. De la autoexigencia a la excelencia: cómo dejar de cargar con todo y aprender a pedir",
                      "Ezequiel Gauna",
                      "¿Te pasa que preferís hacerlo vos antes que explicárselo a otro, porque total lo vas a tener que rehacer? Este taller trabaja una distinción concreta: exigencia no es lo mismo que excelencia. La exigencia pone el foco en el resultado y en lo que falta; la excelencia, en el proceso y en cómo elijo llegar. Desde ahí vamos a la herramienta: el pedido efectivo — qué pido, para qué, a quién, con qué condiciones de satisfacción y en qué plazo. Una hora de práctica en grupo, para salir con un pedido pendiente identificado y formulado." },
                    { 501, 2, 1, "1.02", 40,
                      "¿Cuál es tu límite de fluencia? Cómo reconocer y gestionar las tensiones internas",
                      "Lic. Camila Romero y Lic. Candela Lotero",
                      "Traslada una pregunta de la Ingeniería Civil al comportamiento humano: ¿cuánto podemos sostener antes de que la presión empiece a afectar nuestra forma de pensar, sentir y actuar? Desde la psicología, se aborda cómo el estrés académico y laboral impacta en nuestro rendimiento y nuestras decisiones. A través de ejemplos y un taller práctico, los participantes podrán analizar diferentes formas de responder frente a situaciones de presión y estrategias de afrontamiento aplicables a las demandas académicas y laborales." },
                    { 502, 2, 1, "1.03", 30,
                      "Derrumbes. Casos",
                      "Ing. Claudio Silvio Risetto",
                      "Identificar causas probables de derrumbes. Prevenciones." },
                    { 503, 2, 1, "1.04", 30,
                      "¿Todos vemos lo mismo? Modelos mentales y empatía para potenciar tu vida profesional",
                      "Maria Soledad Corbiere",
                      "Taller práctico para descubrir cómo nuestros modelos mentales influyen en nuestras decisiones y relaciones, y cómo la empatía puede mejorar nuestra forma de trabajar con otros." },
                    { 504, 2, 1, "1.05", 40,
                      "El rol de la matriculación profesional e Incumbencias",
                      "Ing. Federico Augusto Marti",
                      "Exposición sobre la importancia de la matriculación en los Colegios Profesionales, roles y funciones, y su articulación con la Federación Argentina de la Ingeniería Civil. Se analizará el contexto pasado, actual y futuro de las Incumbencias profesionales de diversas ramas del campo laboral de la Ingeniería Civil." },
                    { 505, 2, 1, "1.06", 50,
                      "Ingeniería de detalle y coordinación de instalaciones. Diseñar en la computadora para construir sin errores en la obra",
                      "Ing. María Anahí Zoratto Macias",
                      "En proyectos de gran escala, un conducto de aire acondicionado que choca contra una viga o una tubería mal ubicada puede detener toda la obra. Cómo se realiza la ingeniería de detalle en instalaciones MEP para lograr que el modelo digital sea una copia exacta de lo que se montará en el terreno. A través de casos reales: cómo detectar interferencias a tiempo, respetar las tolerancias de montaje y asegurar que estructura e instalaciones convivan en armonía." },

                    { 506, 2, 2, "1.07", 30,
                      "Sistema de complejos Hidroeléctricos COMAHUE - Río Limay. Taller de práctica de roles en sectores de interés público, privado y sociedad",
                      "Ing. Gerardo Burdisso",
                      "Simulación operativa de las presas hidroeléctricas del río Limay (Alicurá, Piedra del Águila, Pichi Picún Leufú, El Chocón y Arroyito), enfocada en su función energética, de riego y control de crecidas. Mediante un juego de roles, los participantes representan a los diversos sectores de la cuenca —concesionarios, provincias, ORSEP, AIC, CAMMESA y sociedad civil— para debatir intereses cruzados, mientras un grupo opera en tiempo real un software de simulación del complejo." },
                    { 507, 2, 2, "1.08", 48,
                      "Más allá del chat: fundamentos y aplicaciones prácticas de la IA",
                      "Dr. Felipe Ruiz Bruzzone",
                      "Introducción accesible a los conceptos fundamentales de la IA conversacional (con foco en Claude, de Anthropic): qué es un modelo de lenguaje, por qué puede generar respuestas erróneas o alucinar, y qué rol cumple el contexto en sus respuestas. El taller se centra luego en mostrar posibilidades concretas de uso cotidiano, con criterios prácticos para incorporar la IA de forma más eficiente y responsable en la formación académica y el ejercicio profesional." },
                    { 508, 2, 2, "1.09", 30,
                      "Del aula a la vida profesional: El camino real detrás de un cálculo estructural",
                      "Ing. Santiago Vazquez e Ing. Marino Rodriguez Azul",
                      "En la universidad aprendemos a analizar estructuras, determinar solicitaciones, dimensionar elementos y verificar diseños. En esta charla recorreremos un proyecto real de principio a fin, desde la recepción de la arquitectura y el estudio de suelos hasta la documentación necesaria para llevar la estructura a obra — mostrando cómo piensa, decide y trabaja un ingeniero civil frente a un proyecto real." },
                    { 509, 2, 2, "1.10", 35,
                      "Resistencia Efectiva del Hormigón y Ensayo de Testigos",
                      "Ing. Daniel Bascoy",
                      "Determinación de la resistencia efectiva de un lote de hormigón a través del ensayo de testigos. Su interpretación desde el punto de vista reglamentario. Análisis de aplicación en casos reales, con conclusiones y derivaciones prácticas." },
                    { 510, 2, 2, "1.11", 37,
                      "De la distribución real de tensiones al modelo simplificado: impacto en la práctica del Ingeniero Civil",
                      "Ing. Bryan Alejandro Castañón Martinez",
                      "El criterio técnico detrás del diseño en concreto armado: cómo la simplificación geométrica del Bloque Equivalente de Whitney impacta de forma directa las cuatro ecuaciones fundamentales exigidas por normativas internacionales (ACI 318 y CIRSOC 201)." },
                    { 511, 2, 2, "1.12", 40,
                      "El rol de la matriculación profesional e Incumbencias",
                      "Ing. Federico Augusto Marti",
                      "Exposición sobre la importancia de la matriculación en los Colegios Profesionales, roles y funciones, y su articulación con la Federación Argentina de la Ingeniería Civil. Se analizará el contexto pasado, actual y futuro de las Incumbencias profesionales de diversas ramas del campo laboral de la Ingeniería Civil." },
                    { 512, 2, 2, "1.13", 50,
                      "Del plano 2D al modelo 3D: Cómo la ingeniería MEP digital transforma el diseño de instalaciones en la obra",
                      "Ing. María Anahí Zoratto Macias",
                      "Las instalaciones mecánicas, eléctricas y de plomería (MEP) son una de las partes más complejas de cualquier proyecto de ingeniería civil. Introducción al flujo de trabajo real de un ingeniero MEP dentro de un entorno BIM: cómo pasamos de los esquemas tradicionales en 2D a modelos tridimensionales interactivos, y por qué coordinar tuberías, conductos y cables en una computadora antes de ir a la obra evita errores millonarios en la construcción." },

                    { 513, 2, 3, "1.14", 100,
                      "De la distribución real de tensiones al modelo simplificado: impacto en la práctica del Ingeniero Civil",
                      "Ing. Bryan Alejandro Castañón Martinez",
                      "El criterio técnico detrás del diseño en concreto armado: cómo la simplificación geométrica del Bloque Equivalente de Whitney impacta de forma directa las cuatro ecuaciones fundamentales exigidas por normativas internacionales (ACI 318 y CIRSOC 201)." },
                    { 514, 2, 3, "1.15", 38,
                      "¿Aguanta o no Aguanta? Taller de Geotecnia en Acción",
                      "Ing. Gustavo Daniel Mosquera",
                      "Un edificio, un puente, un túnel, una excavación, una presa: obras que parecen no tener nada en común, hasta que descubrís que todas se sostienen —o se caen— por lo mismo. Trabajando en equipo, vas a diseñar tu obra sobre un terreno real, cargarla hasta el límite y enfrentar el momento en que el agua cambia las reglas del juego, viviendo desde adentro la interacción suelo-estructura. Sin fórmulas complicadas y con mucha tierra en las manos." },
                    { 515, 2, 3, "1.16", 86,
                      "Cómo la tecnología está cambiando la forma de diagnosticar estructuras de hormigón",
                      "Ing. Julio Cesar Tomás",
                      "Casos reales de diagnóstico estructural, mostrando cómo estas tecnologías se aplican en la práctica profesional para identificar problemas, comprender el comportamiento de las estructuras y definir estrategias de intervención." },
                    { 516, 2, 3, "1.17", 44,
                      "¿Cómo considerar las cargas horizontales en edificios altos?",
                      "Ing. Anibal Guillermo Tolosa",
                      "El taller busca que los participantes puedan identificar las posibilidades que existen en los edificios altos para tomar cargas horizontales, especialmente de viento, con una metodología sencilla pero rigurosa para entender el fenómeno e identificar el viaje de cargas asociado." },
                });

            // ── CHARLAS SIMULTÁNEAS (bloque 3) — códigos 2.01 a 2.08 ────────────
            migrationBuilder.InsertData(
                table: "SelectableActivities",
                columns: new[] { "Id", "BlockId", "Family", "Code", "Capacity", "Title", "Speaker", "Description" },
                values: new object[,]
                {
                    { 550, 3, 1, "2.01", 140,
                      "Aprender a ApreHender",
                      "Ing. Joaquín N. Perrig",
                      "Una conferencia pensada para que el auditorio no se lleve respuestas, sino una pregunta mejor: ¿qué tipo de profesional quiero ser? En una década donde la tecnología avanza sobre lo técnico, el criterio humano y la conciencia serán el diferencial decisivo." },
                    { 551, 3, 1, "2.02", 40,
                      "Impresión 3D de hormigón en Argentina",
                      "Ing. Rocio Gentico, Techint",
                      "Implementación de la tecnología de impresión 3D de hormigón en Argentina, sus desafíos y aprendizajes obtenidos en proyectos reales de infraestructura." },
                    { 552, 3, 1, "2.03", 40,
                      "El ecosistema BIM en la Ingeniería Civil: De los fundamentos teóricos a la tecnología aplicada",
                      "Ing. Martin Magallanes",
                      "Introducción conceptual y tecnológica a la metodología BIM y su impacto en la ingeniería moderna. Principios básicos de este entorno de trabajo y, mediante casos de uso y herramientas de software específicas, cómo la digitalización optimiza el ciclo de vida de los proyectos." },

                    { 553, 3, 2, "2.04", 220,
                      "Aprender a ApreHender",
                      "Ing. Joaquín N. Perrig",
                      "Una conferencia pensada para que el auditorio no se lleve respuestas, sino una pregunta mejor: ¿qué tipo de profesional quiero ser? En una década donde la tecnología avanza sobre lo técnico, el criterio humano y la conciencia serán el diferencial decisivo." },
                    { 554, 3, 2, "2.05", 50,
                      "Impresión 3D de hormigón en Argentina",
                      "Ing. Rocio Gentico, Techint",
                      "Implementación de la tecnología de impresión 3D de hormigón en Argentina, sus desafíos y aprendizajes obtenidos en proyectos reales de infraestructura." },

                    { 555, 3, 3, "2.06", 40,
                      "Ingenieros digitales y emprendedores",
                      "Ing. Maria de los Angeles Sager",
                      "El mundo cada vez nos exige ser más creativos, y menos rígidos. El rol del ingeniero no es ajeno a la innovación y a las nuevas formas de trabajar — preguntas para disparar la formación de ingenieros emprendedores, alineados con el desarrollo tecnológico y auténticos en su forma de trabajar." },
                    { 556, 3, 3, "2.07", 46,
                      "Demolición, excavación y reciclaje en obras de gran escala",
                      "Ing. Maximiliano Mauriño, Grupo Mitre",
                      "Grupo Mitre —empresa familiar con 40 años de trayectoria y primera empresa de demolición B Certificada del mundo— presenta su enfoque de gestión técnica y sostenible en obras de demolición, movimiento de suelos, infraestructura y reciclaje: técnicas de demolición, tecnología aplicada y revalorización de Residuos de Construcción y Demolición (RCD)." },
                    { 557, 3, 3, "2.08", 182,
                      "Rutas y fauna silvestre, es tiempo de pensar a todas las vidas",
                      "Nicolas Ocampo",
                      "El conocimiento científico y técnico actual, los grandes avances tecnológicos y la creciente conciencia respecto del derecho a la vida deben acompañarse de soluciones creativas en el asfalto para disminuir drásticamente los atropellamientos y muertes de fauna silvestre que ocurren diariamente." },
                });

            // ── SOLIDARIAS (bloque 4) — códigos 3.01 a 3.12, sin familia ────────
            migrationBuilder.InsertData(
                table: "SelectableActivities",
                columns: new[] { "Id", "BlockId", "Family", "Code", "Capacity", "Title", "Speaker", "Description" },
                values: new object[,]
                {
                    { 600, 4, null, "3.01", 100,
                      "Responsabilidad social: del compromiso a la acción",
                      "Lic. Nutricionista Belen Castiñeira — Fundación Marolio — UTN BA - Campus",
                      "Fundación Marolio compartirá su experiencia y trabajo territorial para acercar a los estudiantes al concepto de responsabilidad social y mostrar cómo, desde la profesión y el conocimiento, también se puede generar impacto y transformar realidades." },
                    { 601, 4, null, "3.02", 80,
                      "Fundación Techo + Unión Civil / Subcomisiones de Incumbencias y Web y Multimedia (ANEIC)",
                      "UTN BA - Campus",
                      "Conocé Techo (Adriana Marsan, Fundación Techo) — qué hace la organización y por qué trabaja en barrios populares. · El que firma pierde (Unión Civil + Subcomisiones: de Incumbencias, y Web y Multimedia, ANEIC) — incumbencias profesionales del Ingeniero Civil: qué podés firmar y cuándo." },
                    { 602, 4, null, "3.03", 100,
                      "Ingeniería Sin Fronteras",
                      "Adriana Bertola y Nico Delgado — UTN BA - Campus",
                      "Su trabajo, proyectos pasados y en curso, programa de codiseño con comunidades y su nuevo programa de voluntariado CHE. Incluye un caso práctico del proyecto solidario actual en La Boca — con participación de las Subcomisiones de Convenios y Patrocinios, y Finanzas (ANEIC)." },
                    { 603, 4, null, "3.04", 74,
                      "Club de Leones (Lions International)",
                      "Olga Margot Roglia, Alamo Urbina Mayari, María de los Ángeles Hirz — UTN BA - Campus",
                      "Qué es ser León — historia del Leonismo y su acción social. · Lions International visita las aulas universitarias — discapacidad e inclusión. · Cuidado del medio ambiente — concientización ambiental y otras causas del leonismo (diabetes, visión, hambre, cáncer infantil, ayuda humanitaria, desastres naturales, juventud)." },
                    { 604, 4, null, "3.05", 89,
                      "Colectando Sol + Atalaya Sur",
                      "Ing. Leandro Magri — UTN BA - Campus",
                      "Energía solar y proyectos sociales (Colectando Sol) — aprovechamiento integral de la energía solar desde el modelo de triple impacto. · Proyecto termotanques solares en la Villa 20 (Atalaya Sur, con FIUBA y Jóvenes por el Clima) — energía limpia y accesible para barrios populares." },
                    { 605, 4, null, "3.06", 77,
                      "Energía eólica + Rotaract",
                      "Damián Alejandro Planes Jaluf, Giuliana Mestre y Royner Cañizales — UTN BA - Campus",
                      "Energía eólica: proyectos educativos y sociales — proyectos reales de energía eólica en comunidades aisladas. · Construyendo comunidad (Rotaract Belgrano) — servicio comunitario y proyectos sostenibles." },
                    { 606, 4, null, "3.07", 40,
                      "Módulo Sanitario — Taller de instalaciones",
                      "Equipo de Módulo Sanitario — UTN BA - Campus",
                      "Experiencia práctica de armado de kits eléctricos y termofusión de caños para preparar insumos usados en la construcción de baños para familias en emergencia sanitaria." },
                    { 607, 4, null, "3.08", 75,
                      "Otromodo — Estufa Social Isleña",
                      "Guillermo Horacio Elizalde y Adrián Rodrigo Mancuso — UTN BA - Campus",
                      "Bioconstrucción aplicada a la vivienda social: principios, componentes y datos técnicos de la Estufa Social Isleña. Construyen una estufa en vivo durante la actividad." },
                    { 608, 4, null, "3.09", 20,
                      "Rotaract — Hogar Maradona",
                      "Darío Medina",
                      "Jornada de puesta en valor y relevamiento del edificio Hogar Maradona: pintura, reparaciones generales y relevamiento técnico integral. Requiere ropa larga y cómoda. Sede: Hogar Maradona, Av. Córdoba 6500, Colegiales." },
                    { 609, 4, null, "3.10", 26,
                      "Atalaya Sur — Jornada solidaria",
                      "Atalaya Sur",
                      "Reparación de paredes con enduido y yeso, pintura de fachada, armado de mobiliario y reacondicionamiento de biblioteca. Requiere ropa larga y cómoda. Punto de encuentro a confirmar." },
                    { 610, 4, null, "3.11", 75,
                      "Hospital Posadas — Puesta en valor",
                      "Hospital Posadas",
                      "Demarcación vial en accesos y reacondicionamiento de tándems de asientos en salas de espera. Requiere ropa apta, guantes, barbijo, protector ocular y calzado cerrado. Punto de encuentro a confirmar." },
                    { 611, 4, null, "3.12", 26,
                      "Ingeniería Sin Fronteras — Jornada Solidaria",
                      "Ingeniería Sin Fronteras",
                      "Trabajo colaborativo en el Centro Deportivo Libertad Eterna: nivelación con fajas, carpetas cementicias y tabiquería en seco. Requiere ropa larga y cómoda, casco y calzado de seguridad. Punto de encuentro a confirmar." },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM SelectableActivities WHERE Id BETWEEN 500 AND 611;");
            migrationBuilder.DropColumn(name: "Family", table: "SelectableActivities");
        }
    }
}
