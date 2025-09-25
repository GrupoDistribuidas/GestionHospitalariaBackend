using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Models
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ValidateTokenRequest
    {
        [Required(ErrorMessage = "El token es requerido")]
        public string Token { get; set; } = string.Empty;
    }

    public class ValidateTokenResponse
    {
        public bool IsValid { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}