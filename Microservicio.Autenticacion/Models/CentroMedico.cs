using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microservicio.Autenticacion.Models
{
    public class CentroMedico
    {
        [Key]
        [Column("id_centro_medico")]
        public int IdCentroMedico { get; set; }

        [Required]
        [StringLength(100)]
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        [Column("ciudad")]
        public string? Ciudad { get; set; }

        [StringLength(200)]
        [Column("direccion")]
        public string? Direccion { get; set; }

        // Relación con Empleados
        public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    }
}