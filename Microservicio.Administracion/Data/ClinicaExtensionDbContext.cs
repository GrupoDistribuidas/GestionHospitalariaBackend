using Microsoft.EntityFrameworkCore;
using Microservicio.ClinicaExtension.Models;

namespace Microservicio.ClinicaExtension.Data
{
    public class ClinicaExtensionDbContext : DbContext
    {
        public ClinicaExtensionDbContext(DbContextOptions<ClinicaExtensionDbContext> options)
            : base(options)
        {
        }

        public DbSet<Paciente> Pacientes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Paciente>(entity =>
            {
                entity.ToTable("Pacientes");
                entity.HasKey(e => e.IdPaciente);
                entity.Property(e => e.IdPaciente).HasColumnName("id_paciente");
                entity.Property(e => e.Nombre).HasColumnName("nombre").IsRequired();
                entity.Property(e => e.Cedula).HasColumnName("cedula").IsRequired();
                entity.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento").IsRequired();
                entity.Property(e => e.Telefono).HasColumnName("telefono");
                entity.Property(e => e.Direccion).HasColumnName("direccion");
            });
        }
    }
}
