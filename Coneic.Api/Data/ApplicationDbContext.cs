using Microsoft.EntityFrameworkCore;
using Coneic.Api.Models;

namespace Coneic.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Speaker> Speakers { get; set; }
        public DbSet<Activity> Activities { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Registration> Registrations { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<PaymentBatch> PaymentBatches { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
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

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Email = "dvillar@frba.utn.edu.ar", Password = "admin", Role = "admin" },
                new User { Id = 2, Email = "delegate@utn.edu.ar", Password = "demo", Role = "delegate", DelegationName = "UTN - Facultad Regional Buenos Aires" },
                new User { Id = 3, Email = "test@visitor.com", Password = "demo", Role = "assistant" },
                new User { Id = 4, Email = "spizzamus@frba.utn.edu.ar", Password = "admin", Role = "admin" },
                new User { Id = 5, Email = "cpoggi@frba.utn.edu.ar", Password = "admin", Role = "admin" }
            );
        }
    }
}
