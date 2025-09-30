using Microservicio.Autenticacion.Models;

namespace Microservicio.Autenticacion.Services
{
    public interface IAuthenticationService
    {
        Task<Usuario?> ValidateUserAsync(string nombreUsuario, string contraseña);
        Task<Usuario?> GetUserByUsernameAsync(string nombreUsuario);
        Task<string> GenerateJwtToken(Usuario user);
        Task<bool> SendPasswordByEmailAsync(string nombreUsuario);
    }
}