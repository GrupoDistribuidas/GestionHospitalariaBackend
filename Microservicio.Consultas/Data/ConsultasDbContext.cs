using Microsoft.EntityFrameworkCore;
using Microservicio.Consultas.Models;

namespace Microservicio.Consultas.Data
{
    public class ConsultasDbContext : DbContext
    {
        public ConsultasDbContext(DbContextOptions<ConsultasDbContext> options)
            : base(options)
        {
        }

        public DbSet<ConsultaMedica> ConsultasMedicas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ConsultaMedica>(entity =>
            {
                entity.ToTable("Consulta_medica");
                entity.HasKey(e => e.IdConsultaMedica);
                entity.Property(e => e.IdConsultaMedica).HasColumnName("id_consulta_medica");
                entity.Property(e => e.Fecha).HasColumnName("fecha").IsRequired();
                entity.Property(e => e.Hora).HasColumnName("hora").IsRequired();
                entity.Property(e => e.Motivo).HasColumnName("motivo").IsRequired();
                entity.Property(e => e.Diagnostico).HasColumnName("diagnostico");
                entity.Property(e => e.Tratamiento).HasColumnName("tratamiento");
                entity.Property(e => e.IdPaciente).HasColumnName("id_paciente").IsRequired();
                entity.Property(e => e.IdMedico).HasColumnName("id_medico");
            });
        }
    }
}

