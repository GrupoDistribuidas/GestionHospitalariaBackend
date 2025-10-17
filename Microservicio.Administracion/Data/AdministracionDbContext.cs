using Microsoft.EntityFrameworkCore;
using Microservicio.Administracion.Models;

namespace Microservicio.Administracion.Data
{
    public class AdministracionDbContext : DbContext
    {
        public AdministracionDbContext(DbContextOptions<AdministracionDbContext> options)
            : base(options)
        {
        }

        public DbSet<CentroMedico> CentrosMedicos { get; set; }
        public DbSet<TipoEmpleado> TiposEmpleados { get; set; }
        public DbSet<Especialidad> Especialidades { get; set; }
        public DbSet<Empleado> Empleados { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CentroMedico>(entity =>
            {
                entity.ToTable("Centros_Medicos");
                entity.HasKey(e => e.IdCentroMedico);
                entity.Property(e => e.IdCentroMedico).HasColumnName("id_centro_medico");
                entity.Property(e => e.Nombre).HasColumnName("nombre").IsRequired();
                entity.Property(e => e.Ciudad).HasColumnName("ciudad");
                entity.Property(e => e.Direccion).HasColumnName("direccion");
            });

            modelBuilder.Entity<TipoEmpleado>(entity =>
            {
                entity.ToTable("Tipos_Empleados");
                entity.HasKey(e => e.IdTipo);
                entity.Property(e => e.IdTipo).HasColumnName("id_tipo");
                entity.Property(e => e.Tipo).HasColumnName("tipo").IsRequired();
            });

            modelBuilder.Entity<Especialidad>(entity =>
            {
                entity.ToTable("Especialidades");
                entity.HasKey(e => e.IdEspecialidad);
                entity.Property(e => e.IdEspecialidad).HasColumnName("id_especialidad");
                entity.Property(e => e.Nombre).HasColumnName("nombre").IsRequired();
            });

            modelBuilder.Entity<Empleado>(entity =>
            {
                entity.ToTable("Empleados");
                entity.HasKey(e => e.IdEmpleado);
                entity.Property(e => e.IdEmpleado).HasColumnName("id_empleado");
                entity.Property(e => e.IdCentroMedico).HasColumnName("id_centro_medico");
                entity.Property(e => e.IdTipo).HasColumnName("id_tipo");
                entity.Property(e => e.IdEspecialidad).HasColumnName("id_especialidad");
                entity.Property(e => e.Nombre).HasColumnName("nombre").IsRequired();
                entity.Property(e => e.Telefono).HasColumnName("telefono");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Salario).HasColumnName("salario");
                entity.Property(e => e.Horario).HasColumnName("horario");
                entity.Property(e => e.Estado).HasColumnName("estado").HasDefaultValue("Activo");

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

            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.ToTable("Usuarios");
                entity.HasKey(u => u.IdUsuario);
                entity.Property(u => u.IdUsuario).HasColumnName("id_usuario");
                entity.Property(u => u.NombreUsuario).HasColumnName("nombre_usuario").IsRequired().HasMaxLength(50);
                entity.Property(u => u.Contraseña).HasColumnName("contrasena").IsRequired().HasMaxLength(255);
                entity.Property(u => u.Rol).HasColumnName("rol").IsRequired().HasMaxLength(20).HasDefaultValue("Usuario");
                entity.Property(u => u.IdEmpleado).HasColumnName("id_empleado");

                entity.HasIndex(u => u.NombreUsuario).IsUnique();

                entity.HasOne(u => u.Empleado)
                    .WithOne()
                    .HasForeignKey<Usuario>(u => u.IdEmpleado)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
