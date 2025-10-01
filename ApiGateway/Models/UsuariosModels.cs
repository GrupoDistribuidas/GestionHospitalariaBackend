using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models
{
    // Modelos de respuesta
    public class UsuarioModel
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int IdEmpleado { get; set; }
        public EmpleadoModel? Empleado { get; set; }
    }

    public class EmpleadoModel
    {
        public int IdEmpleado { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
    }

    // Modelos de request para crear/actualizar usuarios
    public class CrearUsuarioRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 255 caracteres")]
        public string Contraseña { get; set; } = string.Empty;

        [StringLength(30, ErrorMessage = "El rol no puede exceder 30 caracteres")]
        public string? Rol { get; set; }

        public int? IdEmpleado { get; set; }
    }

    public class ActualizarUsuarioRequest
    {
        [Required(ErrorMessage = "El ID del usuario es requerido")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
        public string NombreUsuario { get; set; } = string.Empty;

        [StringLength(255, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 255 caracteres")]
        public string? Contraseña { get; set; }

        [StringLength(30, ErrorMessage = "El rol no puede exceder 30 caracteres")]
        public string? Rol { get; set; }

        public int? IdEmpleado { get; set; }
    }

    // Modelos de respuesta estándar
    public class UsuarioResponse : BaseResponse
    {
        public UsuarioModel? Usuario { get; set; }
    }

    public class UsuariosListResponse : BaseResponse
    {
        public List<UsuarioModel> Usuarios { get; set; } = new List<UsuarioModel>();
    }

    public class EliminarUsuarioResponse : BaseResponse
    {
    }

    public class BaseResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}