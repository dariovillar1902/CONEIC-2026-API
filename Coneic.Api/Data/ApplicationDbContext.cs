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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Speaker>().HasData(
                new Speaker { Id = 1, Name = "Ing. Juan Pérez", Title = "Especialista en Estructuras", Bio = "Experto en diseño sismorresistente.", ImageUrl = "https://via.placeholder.com/150", LinkedInUrl = "#" },
                new Speaker { Id = 2, Name = "Dra. María González", Title = "Ingeniería Ambiental", Bio = "Investigadora en recursos hídricos.", ImageUrl = "https://via.placeholder.com/150", LinkedInUrl = "#" }
            );

            modelBuilder.Entity<Activity>().HasData(
                new Activity { Id = 1, Title = "Apertura", Description = "Bienvenida al XVII CONEIC", StartTime = DateTime.Parse("2026-10-15T09:00:00"), EndTime = DateTime.Parse("2026-10-15T10:00:00"), Location = "Auditorio Principal", SpeakerId = null },
                new Activity { Id = 2, Title = "Charla Magistral: Puentes", Description = "Diseño moderno de puentes.", StartTime = DateTime.Parse("2026-10-15T10:30:00"), EndTime = DateTime.Parse("2026-10-15T12:00:00"), Location = "Auditorio Principal", SpeakerId = 1 },
                new Activity { Id = 3, Title = "Taller: BIM", Description = "Introducción a Building Information Modeling.", StartTime = DateTime.Parse("2026-10-15T14:00:00"), EndTime = DateTime.Parse("2026-10-15T16:00:00"), Location = "Sala A", SpeakerId = 2 }
            );

            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Email = "dvillar@frba.utn.edu.ar", Password = "admin", Role = "admin" },
                new User { Id = 2, Email = "delegate@utn.edu.ar", Password = "demo", Role = "delegate", DelegationName = "UTN - Facultad Regional Buenos Aires" },
                new User { Id = 3, Email = "test@visitor.com", Password = "demo", Role = "assistant" }
            );
        }
    }
}
