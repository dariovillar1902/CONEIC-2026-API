using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Coneic.Api.Models;

namespace Coneic.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<PaymentBatch> PaymentBatches { get; set; }
        public DbSet<Speaker> Speakers { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<ManualComment> ManualComments { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<AttendanceSession> AttendanceSessions { get; set; }
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
        public DbSet<ActivityBlock> ActivityBlocks { get; set; }
        public DbSet<SelectableActivity> SelectableActivities { get; set; }
        public DbSet<ActivitySelection> ActivitySelections { get; set; }

        private static readonly JsonSerializerOptions _json = new();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppSetting>().HasKey(s => s.Key);

            modelBuilder.Entity<AttendanceRecord>()
                .HasIndex(r => new { r.RegistrationId, r.SessionId })
                .IsUnique();

            modelBuilder.Entity<ActivitySelection>()
                .HasIndex(s => new { s.UserEmail, s.BlockId })
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.ManagedFaculties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _json),
                    v => JsonSerializer.Deserialize<List<string>>(v, _json) ?? new List<string>())
                .HasColumnType("TEXT")
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (a, b) => a != null && b != null && a.SequenceEqual(b),
                    c => c.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                    c => c.ToList()));

            modelBuilder.Entity<PaymentBatch>()
                .Property(b => b.Assignments)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, _json),
                    v => JsonSerializer.Deserialize<List<BatchAssignment>>(v, _json) ?? new List<BatchAssignment>())
                .HasColumnType("TEXT")
                .Metadata.SetValueComparer(new ValueComparer<List<BatchAssignment>>(
                    (a, b) => a != null && b != null && a.Count == b.Count,
                    c => c.Count,
                    c => c.ToList()));

            modelBuilder.Entity<Speaker>().HasData(
                new Speaker { Id = 1, Name = "Ing. Roberto Fernández", Title = "Especialista en Estructuras y Sismología",    Bio = "Especialista en diseño sísmico y estructuras de gran escala con más de 20 años de trayectoria en obras de infraestructura.",                                                                     ImageUrl = "https://randomuser.me/api/portraits/men/32.jpg",   LinkedInUrl = "#" },
                new Speaker { Id = 2, Name = "Dra. Laura Gómez",       Title = "Ingeniería Hidráulica y Ambiental",           Bio = "Investigadora en hidráulica e ingeniería ambiental. Autora de numerosas publicaciones sobre gestión de recursos hídricos en la cuenca del Plata.",                                    ImageUrl = "https://randomuser.me/api/portraits/women/44.jpg", LinkedInUrl = "#" },
                new Speaker { Id = 3, Name = "Ing. Diego Torres",       Title = "BIM y Tecnología en la Construcción",         Bio = "Referente nacional en implementación de BIM en proyectos de infraestructura vial y edilicia. Docente en UTN y UBA.",                                                                   ImageUrl = "https://randomuser.me/api/portraits/men/55.jpg",   LinkedInUrl = "#" },
                new Speaker { Id = 4, Name = "Ing. Valeria Ríos",       Title = "Nuevos Materiales y Nanotecnología",          Bio = "Investigadora del CONICET en nuevos materiales constructivos: hormigones especiales, geosintéticos y nanotecnología aplicada.",                                                       ImageUrl = "https://randomuser.me/api/portraits/women/61.jpg", LinkedInUrl = "#" }
            );

            // ── MARTES 4 ago ──────────────────────────────────────────────────
            modelBuilder.Entity<Activity>().HasData(
                new Activity { Id = 1,  Title = "Acreditación y Bienvenida",                    Description = "Registro de asistentes y entrega de kits de bienvenida.",                                                                                      StartTime = new DateTime(2026,8,4, 9, 0,0), EndTime = new DateTime(2026,8,4,10, 0,0), Location = "Hall Principal — UTN FRBA",       SpeakerId = null },
                new Activity { Id = 2,  Title = "Charla Inaugural: La Ingeniería Civil del Futuro", Description = "Conferencia magistral de apertura con visión integral sobre los desafíos actuales y futuros de la ingeniería civil argentina.",             StartTime = new DateTime(2026,8,4,10, 0,0), EndTime = new DateTime(2026,8,4,13, 0,0), Location = "Auditorio Principal",              SpeakerId = 1 },
                new Activity { Id = 3,  Title = "Almuerzo",                                     Description = "",                                                                                                                                               StartTime = new DateTime(2026,8,4,13, 0,0), EndTime = new DateTime(2026,8,4,14,30,0), Location = "Patio del Campus",                SpeakerId = null },
                new Activity { Id = 4,  Title = "Taller: BIM aplicado a Infraestructura",       Description = "Aplicación de BIM en proyectos de infraestructura vial y edilicia: flujos de trabajo, software y casos reales.",                               StartTime = new DateTime(2026,8,4,14,30,0), EndTime = new DateTime(2026,8,4,17, 0,0), Location = "Laboratorio de Informática",      SpeakerId = 3 },
                new Activity { Id = 5,  Title = "Taller: Diseño Sismorresistente",              Description = "Criterios de diseño sísmico, normativa CIRSOC y análisis de estructuras ante solicitaciones dinámicas.",                                        StartTime = new DateTime(2026,8,4,17, 0,0), EndTime = new DateTime(2026,8,4,18,30,0), Location = "Sala de Estructuras",              SpeakerId = 1 },
                new Activity { Id = 6,  Title = "Cena de Bienvenida",                           Description = "Cena de inicio del congreso con networking y presentación de delegaciones de todo el país.",                                                    StartTime = new DateTime(2026,8,4,20, 0,0), EndTime = new DateTime(2026,8,4,22,30,0), Location = "Salón de Eventos — Sede Central", SpeakerId = null },
            // ── MIÉRCOLES 5 ago ───────────────────────────────────────────────
                new Activity { Id = 7,  Title = "Acreditación",                                 Description = "",                                                                                                                                               StartTime = new DateTime(2026,8,5, 9, 0,0), EndTime = new DateTime(2026,8,5, 9,30,0), Location = "Hall Principal",                  SpeakerId = null },
                new Activity { Id = 8,  Title = "Charla Magistral: Infraestructura Hídrica",    Description = "Panorama actual de la infraestructura hídrica en Argentina: cuencas, embalses y gestión del agua en un contexto de cambio climático.",          StartTime = new DateTime(2026,8,5, 9,30,0), EndTime = new DateTime(2026,8,5,12, 0,0), Location = "Auditorio Principal",              SpeakerId = 2 },
                new Activity { Id = 9,  Title = "Panel: Obras Emblemáticas de Buenos Aires",    Description = "Panel multidisciplinario sobre las obras más emblemáticas de Buenos Aires: subterráneos, autopistas, Puerto Madero y obras hidráulicas.",        StartTime = new DateTime(2026,8,5,12, 0,0), EndTime = new DateTime(2026,8,5,13,30,0), Location = "Auditorio Principal",              SpeakerId = null },
                new Activity { Id = 10, Title = "Almuerzo",                                     Description = "",                                                                                                                                               StartTime = new DateTime(2026,8,5,13,30,0), EndTime = new DateTime(2026,8,5,15, 0,0), Location = "Patio del Campus",                SpeakerId = null },
                new Activity { Id = 11, Title = "Visita Técnica: Puerto de Buenos Aires",       Description = "Recorrido técnico guiado por las instalaciones y obras en curso del Puerto de Buenos Aires.",                                                   StartTime = new DateTime(2026,8,5,15, 0,0), EndTime = new DateTime(2026,8,5,19, 0,0), Location = "Puerto Madero — Dique 4",          SpeakerId = null },
                new Activity { Id = 12, Title = "Peña Federal",                                 Description = "Noche de integración federal con música, gastronomía regional y actividades culturales.",                                                        StartTime = new DateTime(2026,8,5,20,30,0), EndTime = new DateTime(2026,8,5,23, 0,0), Location = "Salón de Eventos — Sede Central", SpeakerId = null },
            // ── JUEVES 6 ago ──────────────────────────────────────────────────
                new Activity { Id = 13, Title = "Acreditación",                                 Description = "",                                                                                                                                               StartTime = new DateTime(2026,8,6, 9, 0,0), EndTime = new DateTime(2026,8,6, 9,30,0), Location = "Hall Principal",                  SpeakerId = null },
                new Activity { Id = 14, Title = "Taller: Nuevos Materiales Constructivos",      Description = "Exploración de hormigones especiales, geosintéticos y nanotecnología aplicados a la construcción moderna.",                                     StartTime = new DateTime(2026,8,6, 9,30,0), EndTime = new DateTime(2026,8,6,12,30,0), Location = "Laboratorio de Materiales",       SpeakerId = 4 },
                new Activity { Id = 15, Title = "Almuerzo",                                     Description = "",                                                                                                                                               StartTime = new DateTime(2026,8,6,12,30,0), EndTime = new DateTime(2026,8,6,14, 0,0), Location = "Patio del Campus",                SpeakerId = null },
                new Activity { Id = 16, Title = "Actividad Solidaria",                          Description = "Intervención constructiva voluntaria en una comunidad local de la zona sur de Buenos Aires.",                                                    StartTime = new DateTime(2026,8,6,14, 0,0), EndTime = new DateTime(2026,8,6,17, 0,0), Location = "Comunidad La Paloma — Zona Sur",  SpeakerId = null },
                new Activity { Id = 17, Title = "Asamblea ANEIC",                               Description = "Reunión de representantes universitarios para tratar agenda de ANEIC, elección de sede 2027 y resoluciones.",                                   StartTime = new DateTime(2026,8,6,17, 0,0), EndTime = new DateTime(2026,8,6,19, 0,0), Location = "Auditorio Principal",              SpeakerId = null },
                new Activity { Id = 18, Title = "Noche Temática: Folklore Porteño",             Description = "Noche temática con folklore porteño, tango y gastronomía bonaerense.",                                                                          StartTime = new DateTime(2026,8,6,20,30,0), EndTime = new DateTime(2026,8,6,23, 0,0), Location = "Salón de Eventos — Sede Central", SpeakerId = null },
            // ── VIERNES 7 ago ─────────────────────────────────────────────────
                new Activity { Id = 19, Title = "Acreditación",                                 Description = "",                                                                                                                                               StartTime = new DateTime(2026,8,7, 9, 0,0), EndTime = new DateTime(2026,8,7, 9,30,0), Location = "Hall Principal",                  SpeakerId = null },
                new Activity { Id = 20, Title = "Charla de Cierre: Sostenibilidad en la Ingeniería", Description = "Reflexión sobre sostenibilidad, cambio climático y el rol del ingeniero civil en la transición energética.",                               StartTime = new DateTime(2026,8,7, 9,30,0), EndTime = new DateTime(2026,8,7,12, 0,0), Location = "Auditorio Principal",              SpeakerId = 2 },
                new Activity { Id = 21, Title = "Almuerzo",                                     Description = "",                                                                                                                                               StartTime = new DateTime(2026,8,7,12, 0,0), EndTime = new DateTime(2026,8,7,13,30,0), Location = "Patio del Campus",                SpeakerId = null },
                new Activity { Id = 22, Title = "Actividad Recreativa y Sorteos",               Description = "Actividades recreativas, concursos y sorteos de premios especiales para los asistentes.",                                                       StartTime = new DateTime(2026,8,7,14, 0,0), EndTime = new DateTime(2026,8,7,16, 0,0), Location = "Patio del Campus",                SpeakerId = null },
                new Activity { Id = 23, Title = "Acto de Clausura",                             Description = "Ceremonia oficial de cierre del XVIII CONEIC: entrega de diplomas, reconocimientos y palabras de despedida.",                                   StartTime = new DateTime(2026,8,7,16, 0,0), EndTime = new DateTime(2026,8,7,18, 0,0), Location = "Auditorio Principal",              SpeakerId = null }
            );

            // ══════════════════════════════════════════════════════════════════
            // ELECCIÓN DE ACTIVIDADES — datos de demo/prueba
            // Reconstruidos a partir de "elección de actividades.pdf" (Carol,
            // 1/9/2026). Capacidades = placeholder (30/40) hasta que Acad/GyP
            // confirmen números reales. Los bloques de Talleres/Charlas son una
            // agrupación provisoria: los horarios reales todavía se están
            // definiendo (ver chat 1/9/2026). NO representa la versión final.
            // ══════════════════════════════════════════════════════════════════
            modelBuilder.Entity<ActivityBlock>().HasData(
                new ActivityBlock { Id = 1, Category = "VisitaTecnica", Name = "Visita Técnica", MaxSelections = 1, IsActive = true,
                    Note = "Elegí una visita técnica. Cupos y horarios definitivos a confirmar." },
                // Talleres/Charlas: contenido sembrado pero oculto por ahora — se
                // reactiva más adelante cuando estén definidos los bloques horarios.
                new ActivityBlock { Id = 2, Category = "TallerCharla", Name = "Talleres y Charlas Simultáneas", MaxSelections = 1, IsActive = false,
                    Note = "Agrupación provisoria de demo — los bloques horarios reales todavía se están definiendo con Académica y GyP." }
            );

            modelBuilder.Entity<SelectableActivity>().HasData(
                // ── Talleres (1.01–1.10) ─────────────────────────────────────
                new SelectableActivity { Id = 101, BlockId = 2, Code = "1.01", Capacity = 40, Title = "¿Y ahora qué?", Speaker = "Ing. Axel Colantuono, CRIBA",
                    Description = "El objetivo del taller es acompañar a estudiantes de la carrera con la duda de qué sucede una vez recibido, cómo posicionarse en el mercado laboral y qué posibilidades hay hoy en día para ingenieros civiles." },
                new SelectableActivity { Id = 102, BlockId = 2, Code = "1.02", Capacity = 40, Title = "Más allá del chat: fundamentos y aplicaciones prácticas de la IA", Speaker = "Dr. Felipe Ruiz Bruzzone",
                    Description = "Introducción accesible a la IA conversacional, con foco en Claude: qué es un modelo de lenguaje, por qué puede alucinar y cómo usarla de forma más eficiente y responsable en la formación y el ejercicio profesional." },
                new SelectableActivity { Id = 103, BlockId = 2, Code = "1.03", Capacity = 40, Title = "Derrumbes. Casos.", Speaker = "Ing. Claudio Silvio Risetto",
                    Description = "Identificar causas probables de derrumbes. Prevenciones." },
                new SelectableActivity { Id = 104, BlockId = 2, Code = "1.04", Capacity = 40, Title = "Cómo la tecnología está cambiando la forma de diagnosticar estructuras de hormigón", Speaker = "Ing. Julio Cesar Tomás, ITAC Laboratorio",
                    Description = "Presentación de casos reales de diagnóstico estructural mediante tecnologías avanzadas. Cómo identificar patologías, comprender el comportamiento del hormigón y definir estrategias técnicas de intervención eficiente." },
                new SelectableActivity { Id = 105, BlockId = 2, Code = "1.05", Capacity = 40, Title = "Ingeniería de detalle y coordinación de instalaciones: diseñar en la computadora para construir sin errores en la obra", Speaker = "Ing. María Anahí Zoratto Macia",
                    Description = "Cómo realizar la ingeniería de detalle en instalaciones MEP para obtener modelos digitales listos para fabricación. Detección temprana de interferencias y tolerancias de montaje para asegurar que estructura e instalaciones convivan en armonía." },
                new SelectableActivity { Id = 106, BlockId = 2, Code = "1.06", Capacity = 40, Title = "Del plano 2D al modelo 3D: cómo la ingeniería MEP digital transforma el diseño de instalaciones", Speaker = "Ing. María Anahí Zoratto Macia",
                    Description = "Introducción al flujo de trabajo del ingeniero MEP dentro del entorno BIM. Transición de esquemas tradicionales 2D a modelos 3D interactivos para coordinar conductos, tuberías y cables antes de ir a obra, evitando errores millonarios." },
                new SelectableActivity { Id = 107, BlockId = 2, Code = "1.07", Capacity = 40, Title = "De la distribución real de tensiones al modelo simplificado: impacto en la práctica del Ingeniero Civil", Speaker = "Ing. Bryan Alejandro Castañón, ANEIC Guatemala",
                    Description = "Criterio técnico en hormigón armado: análisis de cómo la simplificación del Bloque Equivalente de Whitney rige las ecuaciones clave de ACI 318 y CIRSOC 201." },
                new SelectableActivity { Id = 108, BlockId = 2, Code = "1.08", Capacity = 40, Title = "Sistema de complejos Hidroeléctricos COMAHUE - Río Limay: taller de práctica de roles en sectores de interés público, privado y sociedad", Speaker = "Ing. Gerardo Burdisso",
                    Description = "Simulación operativa en software de presas hidroeléctricas con juego de roles para la toma de decisiones en generación energética, control de crecidas y gestión ambiental." },
                new SelectableActivity { Id = 109, BlockId = 2, Code = "1.09", Capacity = 40, Title = "La obra que no contamina: taller de gestión ambiental para ingenieros civiles", Speaker = "Lic. Romina Favilla",
                    Description = "Práctica en equipo sobre un caso simulado de obra civil en tiempo real: identificación de impactos ambientales, aplicación de jerarquías de mitigación y resolución de imprevistos en obra." },
                new SelectableActivity { Id = 110, BlockId = 2, Code = "1.10", Capacity = 40, Title = "¿Aguanta o no Aguanta? Taller de Geotecnia en Acción", Speaker = "Ing. Gustavo Daniel Mosquera",
                    Description = "Un edificio, un puente, una presa: obras distintas que se sostienen —o se caen— por lo mismo. En este taller no vas a escuchar la teoría, la vas a construir con tus manos y en equipo. Diseñá tu estructura sobre un terreno real, cargala hasta el límite y descubrí en vivo cómo el agua cambia las reglas del juego." },

                // ── Charlas simultáneas (2.01–2.06) ──────────────────────────
                new SelectableActivity { Id = 201, BlockId = 2, Code = "2.01", Capacity = 40, Title = "El ecosistema BIM en la Ingeniería Civil: de los fundamentos teóricos a la tecnología aplicada", Speaker = "Ing. Martín Magallanes",
                    Description = "Tecnología aplicada, fundamentos y entorno de la metodología BIM, y su integración con la gestión eficiente en obra." },
                new SelectableActivity { Id = 202, BlockId = 2, Code = "2.02", Capacity = 40, Title = "Rutas y Fauna Silvestre, es tiempo de pensar a todas las vidas", Speaker = "Lic. Nicolás Lodeiro Ocampo",
                    Description = "Integración del conocimiento técnico y el derecho a la vida silvestre en la infraestructura: impacto de los atropellamientos de fauna y soluciones de ingeniería aplicada." },
                new SelectableActivity { Id = 203, BlockId = 2, Code = "2.03", Capacity = 40, Title = "Aprender a ApreHender: el diferencial humano frente al avance tecnológico", Speaker = "Ing. Joaquín N. Perrig",
                    Description = "Por qué el criterio y la conciencia son el verdadero valor diferencial frente a la automatización técnica, y herramientas para liderar tu propio desarrollo en la próxima década." },
                new SelectableActivity { Id = 204, BlockId = 2, Code = "2.04", Capacity = 40, Title = "Impresión 3D de hormigón en Argentina: desafíos y aprendizajes en proyectos reales", Speaker = "Ing. Rocío Gentico, Techint",
                    Description = "Desafíos técnicos y operativos superados en proyectos reales, implementación de la tecnología a escala nacional y lecciones aprendidas." },
                new SelectableActivity { Id = 205, BlockId = 2, Code = "2.05", Capacity = 40, Title = "Demolición, Excavación y Reciclaje en Obras de Gran Escala", Speaker = "Ing. Maximiliano Mauriño, Grupo Mitre",
                    Description = "Gestión sustentable, técnicas avanzadas de demolición según escala de obra y economía circular con residuos de construcción y demolición (RCD)." },
                new SelectableActivity { Id = 206, BlockId = 2, Code = "2.06", Capacity = 40, Title = "Terremoto 2026 en Venezuela: ingeniería forense y lecciones aprendidas", Speaker = "Ing. Gustavo Delgado",
                    Description = "Catalogación de fallas, evolución constructiva e ingeniería forense: análisis exhaustivo de daños estructurales, geomorfológicos y socioeconómicos tras el evento sísmico." },

                // ── Visitas técnicas (4.01–4.26) ─────────────────────────────
                new SelectableActivity { Id = 401, BlockId = 1, Code = "4.01", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-01.jpg", Title = "Complejo Nuclear Atucha", Description = "Pionera de la Energía Nuclear en América Latina." },
                new SelectableActivity { Id = 402, BlockId = 1, Code = "4.02", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-02.jpg", Title = "Puerto Buenos Aires", Description = "Ingeniería portuaria y logística multimodal a gran escala." },
                new SelectableActivity { Id = 403, BlockId = 1, Code = "4.03", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-03.jpg", Title = "Complejo Ambiental Norte III", Description = "Ingeniería ambiental y gestión de residuos a escala metropolitana." },
                new SelectableActivity { Id = 404, BlockId = 1, Code = "4.04", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-04.jpg", Title = "Planta Potabilizadora Gral. Belgrano", Description = "1.950.000 m³/día de agua tratada para la región metropolitana." },
                new SelectableActivity { Id = 405, BlockId = 1, Code = "4.05", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-05.jpg", Title = "Planta Depuradora Sudoeste", Description = "237.000 m³/día de líquidos tratados. Sirve a gran parte del partido de La Matanza." },
                new SelectableActivity { Id = 406, BlockId = 1, Code = "4.06", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-06.jpg", Title = "Planta Depuradora Lanús", Description = "23.328 m³/día de efluentes tratados. Saneamiento directo para 90.000 habitantes." },
                new SelectableActivity { Id = 407, BlockId = 1, Code = "4.07", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-07.jpg", Title = "Planta Depuradora El Jagüel", Description = "48.000 m³/día de efluentes tratados. Operativa para Ezeiza y parte de Esteban Echeverría." },
                new SelectableActivity { Id = 408, BlockId = 1, Code = "4.08", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-08.jpg", Title = "Planta Depuradora Fiorito", Description = "77.760 m³/día de efluentes tratados. Beneficia a 270.000 habitantes." },
                new SelectableActivity { Id = 409, BlockId = 1, Code = "4.09", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-09.jpg", Title = "Planta de Hormigón Sola", Description = "Elementos de hormigón premoldeado. Tecnología y estandarización a gran escala para la industria de la construcción." },
                new SelectableActivity { Id = 410, BlockId = 1, Code = "4.10", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-10.jpg", Title = "Planta Depuradora Hurlingham", Description = "30.240 m³/día de efluentes tratados. Sirve a Hurlingham, Ituzaingó, Morón y Tres de Febrero." },
                new SelectableActivity { Id = 411, BlockId = 1, Code = "4.11", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-11.jpg", Title = "Fenomix", Description = "Producción y logística de hormigón elaborado. El detrás de la dosificación y distribución para obras de gran porte." },
                new SelectableActivity { Id = 412, BlockId = 1, Code = "4.12", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-12.jpg", Title = "Guardería Náutica Neptuno", Description = "Infraestructura costera y portuaria. Soluciones de ingeniería para el almacenamiento y movimiento de embarcaciones." },
                new SelectableActivity { Id = 413, BlockId = 1, Code = "4.13", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-13.jpg", Title = "Madero Harbour", Description = "Un proyecto de GNV Group para transformar la vida urbana en Puerto Madero." },
                new SelectableActivity { Id = 414, BlockId = 1, Code = "4.14", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-14.jpg", Title = "Impresora 3D de Concreto", Description = "Primera en su tipo adquirida por una empresa constructora en Argentina." },
                new SelectableActivity { Id = 415, BlockId = 1, Code = "4.15", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-15.jpg", Title = "Proyecto Udaondo", Description = "Una obra de usos mixtos que integra residencias de lujo, un hotel cinco estrellas y 7.600 m² de amenidades de primer nivel." },
                new SelectableActivity { Id = 416, BlockId = 1, Code = "4.16", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-16.jpg", Title = "Quartier Bajo Belgrano", Description = "Un proyecto inmobiliario en una de las zonas con mayor proyección de Buenos Aires, que integra el diseño urbano y entorno natural." },
                new SelectableActivity { Id = 417, BlockId = 1, Code = "4.17", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-17.jpg", Title = "Autopista Parque Dellepiane", Description = "Será la primera autopista parque de la Ciudad, diseñada para optimizar la movilidad y el espacio público." },
                new SelectableActivity { Id = 418, BlockId = 1, Code = "4.18", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-18.jpg", Title = "MilAires", Description = "Complejo residencial que combina departamentos con grandes espacios verdes y servicios premium propios de un barrio cerrado dentro de la ciudad." },
                new SelectableActivity { Id = 419, BlockId = 1, Code = "4.19", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-19.jpg", Title = "Puente Labruna", Description = "Ampliación y renovación del puente que conecta el Parque de la Innovación y la Ciudad Universitaria." },
                new SelectableActivity { Id = 420, BlockId = 1, Code = "4.20", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-20.jpg", Title = "Anillo Pampa", Description = "Nueva conexión entre la Ciudad y el Río de la Plata que mejorará la movilidad e integrará el entorno." },
                new SelectableActivity { Id = 421, BlockId = 1, Code = "4.21", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-21.jpg", Title = "Ñlet Loreto", Description = "Nueva torre de 17 pisos y 4 subsuelos en Belgrano: departamentos de hasta 555 m² con pileta propia, spa, gimnasio y pileta en la terraza." },
                new SelectableActivity { Id = 422, BlockId = 1, Code = "4.22", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-22.jpg", Title = "Estadio Más Monumental", Description = "Megaobra histórica del Más Monumental: techado integral, tribuna 360° y ampliación de capacidad a 101.000 espectadores con cerca de 100 columnas perimetrales." },
                new SelectableActivity { Id = 423, BlockId = 1, Code = "4.23", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-23.jpg", Title = "Centro Cultural San Martín", Description = "Modernización integral y preservación patrimonial: un complejo emblemático de 42.000 m² compuesto por una torre de 12 pisos, cuerpo bajo y 6 subsuelos." },
                new SelectableActivity { Id = 424, BlockId = 1, Code = "4.24", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-24.jpg", Title = "Escuela Indira Gandhi", Description = "Nueva infraestructura educativa de 2.900 m² en el Barrio 31: diseño bioclimático, sistemas constructivos industrializados y espacios de alta funcionalidad pedagógica." },
                new SelectableActivity { Id = 425, BlockId = 1, Code = "4.25", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-25.jpg", Title = "Complejo Dorrego Plaza", Description = "Obra residencial de 13.677 m² (2, 3 y 4 ambientes). Trabajos actuales: estructura en 5° piso, inicio de albañilería y grúa torre con trepado." },
                new SelectableActivity { Id = 426, BlockId = 1, Code = "4.26", Capacity = 30, ImageUrl = "/assets/visitas/visita-4-26.jpg", Title = "JN4016", Description = "70.000 m² cubiertos. Desarrollo de usos mixtos (hotel de lujo, viviendas, oficinas y plaza pública) con un 35% de factor de ocupación y 65% de espacio público." }
            );

        }
    }
}
