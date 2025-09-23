using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microservicio.Autenticacion.Models
{
    public class TipoEmpleado
    {
        [Key]
        [Column("id_tipo")]
        public int IdTipo { get; set; }

        [Required]
        [StringLength(50)]
        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        // Relación con Empleados
        public ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();
    }
}