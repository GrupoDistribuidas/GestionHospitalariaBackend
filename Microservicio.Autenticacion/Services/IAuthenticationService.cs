using Microservicio.Autenticacion.Models;

namespace Microservicio.Autenticacion.Services
{
    public interface IAuthenticationService
    {
        Task<Usuario?> ValidateUserAsync(string nombreUsuario, string contrase�a);
        Task<Usuario?> GetUserByUsernameAsync(string nombreUsuario);
        Task<string> GenerateJwtToken(Usuario user);
    }
}