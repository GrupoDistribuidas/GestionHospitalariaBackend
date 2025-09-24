using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microservicio.Autenticacion.Models
{
    public class Empleado
    {
        [Key]
        [Column("id_empleado")]
        public int IdEmpleado { get; set; }

        [Column("id_centro_medico")]
        public int? IdCentroMedico { get; set; }

        [Column("id_tipo")]
        public int? IdTipo { get; set; }

        [Column("id_especialidad")]
        public int? IdEspecialidad { get; set; }

        [Required]
        [StringLength(100)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(20)]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [StringLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [Column("salario")]
        public decimal? Salario { get; set; }

        [StringLength(100)]
        [Column("horario")]
        public string? Horario { get; set; }

        [Required]
        [StringLength(20)]
        [Column("estado")]
        public string Estado { get; set; } = "Activo";

        // Propiedades calculadas para compatibilidad
        [NotMapped]
        public bool Activo => Estado == "Activo";

        [NotMapped]
        public string Rol => TipoEmpleado?.Tipo ?? "empleado";

        // Relaciones
        [ForeignKey("IdCentroMedico")]
        public CentroMedico? CentroMedico { get; set; }

        [ForeignKey("IdTipo")]
        public TipoEmpleado? TipoEmpleado { get; set; }

        [ForeignKey("IdEspecialidad")]
        public Especialidad? Especialidad { get; set; }

        // Relación con Usuario
        public Usuario? Usuario { get; set; }
    }
}