using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Microservicio.Autenticacion.Models
{
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [StringLength(50)]
        [Column("nombre_usuario")]
        public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    [Column("contrasena")]
    public string Contraseña { get; set; } = string.Empty;

        [StringLength(50)]
        [Column("rol")]
        public string? Rol { get; set; }

        [Column("id_empleado")]
        public int? IdEmpleado { get; set; }

        // Relaci�n con Empleado
        [ForeignKey("IdEmpleado")]
        public Empleado? Empleado { get; set; }
    }
}