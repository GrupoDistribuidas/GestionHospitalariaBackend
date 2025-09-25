using Microsoft.EntityFrameworkCore;
using Microservicio.Autenticacion.Models;

namespace Microservicio.Autenticacion.Data
{
    public class HospitalDbContext : DbContext
    {
        public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<CentroMedico> CentrosMedicos { get; set; }
        public DbSet<TipoEmpleado> TiposEmpleados { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la tabla Centros_Medicos
            modelBuilder.Entity<CentroMedico>(entity =>
            {
                entity.ToTable("Centros_Medicos");
                entity.HasKey(e => e.IdCentroMedico);
            });

            // Configuración de la tabla Tipos_Empleados
            modelBuilder.Entity<TipoEmpleado>(entity =>
            {
                entity.ToTable("Tipos_Empleados");
                entity.HasKey(e => e.IdTipo);
            });

            // Configuración de la tabla Especialidades
            modelBuilder.Entity<Especialidad>(entity =>
            {
                entity.ToTable("Especialidades");
                entity.HasKey(e => e.IdEspecialidad);
            });

            // Configuración de la tabla Empleados
            modelBuilder.Entity<Empleado>(entity =>
            {
                entity.ToTable("Empleados");
                entity.HasKey(e => e.IdEmpleado);
                
                entity.HasIndex(e => e.Email);
                
                // Configurar relaciones
                entity.HasOne(e => e.CentroMedico)
                    .WithMany(c => c.Empleados)
                    .HasForeignKey(e => e.IdCentroMedico)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.TipoEmpleado)
                    .WithMany(t => t.Empleados)
                    .HasForeignKey(e => e.IdTipo)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Especialidad)
                    .WithMany(es => es.Empleados)
                    .HasForeignKey(e => e.IdEspecialidad)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de la tabla Usuarios
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(e => e.IdUsuario);
                
                entity.HasIndex(e => e.NombreUsuario).IsUnique();

                // Configurar relación con Empleado
                entity.HasOne(u => u.Empleado)
                    .WithOne(e => e.Usuario)
                    .HasForeignKey<Usuario>(u => u.IdEmpleado)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}