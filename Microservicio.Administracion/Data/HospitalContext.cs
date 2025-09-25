using Microsoft.EntityFrameworkCore;

namespace Microservicio.Administracion.Data
{
    public class HospitalContext : DbContext
    {
        public HospitalContext(DbContextOptions<HospitalContext> options) : base(options) { }
        public DbSet<Especialidad> Especialidades => Set<Especialidad>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Especialidad>(e =>
            {
                e.ToTable("Especialidades");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).HasColumnName("id_especialidad");
                e.Property(x => x.Nombre).HasColumnName("nombre").IsRequired().HasMaxLength(100);
            });
        }
    }

    public class Especialidad
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = default!;
    }
}
