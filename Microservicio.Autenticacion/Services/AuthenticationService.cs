using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microservicio.Autenticacion.Data;
using Microservicio.Autenticacion.Models;

namespace Microservicio.Autenticacion.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HospitalDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(HospitalDbContext context, IConfiguration config, ILogger<AuthenticationService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<Usuario?> ValidateUserAsync(string nombreUsuario, string contraseña)
        {
            try
            {
                if (string.IsNullOrEmpty(nombreUsuario) || string.IsNullOrEmpty(contraseña))
                {
                    _logger.LogWarning("Credenciales faltantes en solicitud de login");
                    return null;
                }

                var user = await _context.Usuarios
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e!.TipoEmpleado)
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e!.CentroMedico)
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e!.Especialidad)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && 
                                           u.Empleado != null && 
                                           u.Empleado.Estado == "Activo");

                if (user == null)
                {
                    _logger.LogWarning("Usuario no encontrado o inactivo: {Username}", nombreUsuario);
                    return null;
                }

                if (!BCrypt.Net.BCrypt.Verify(contraseña, user.Contraseña))
                {
                    _logger.LogWarning("Intento de login fallido para usuario: {Username}", nombreUsuario);
                    return null;
                }

                _logger.LogInformation("Login exitoso para usuario: {Username}", nombreUsuario);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante validación de usuario: {Username}", nombreUsuario);
                return null;
            }
        }

        public async Task<Usuario?> GetUserByUsernameAsync(string nombreUsuario)
        {
            try
            {
                return await _context.Usuarios
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e!.TipoEmpleado)
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e!.CentroMedico)
                    .Include(u => u.Empleado)
                        .ThenInclude(e => e!.Especialidad)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && 
                                           u.Empleado != null && 
                                           u.Empleado.Estado == "Activo");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario: {Username}", nombreUsuario);
                return null;
            }
        }

        public async Task<string> GenerateJwtToken(Usuario user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? 
                throw new InvalidOperationException("Jwt:Key no configurada")));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, user.NombreUsuario)
            };

            if (user.Empleado != null)
            {
                claims.Add(new Claim(ClaimTypes.Email, user.Empleado.Email ?? ""));
                claims.Add(new Claim(ClaimTypes.Role, user.Empleado.Rol));
                claims.Add(new Claim("nombre_completo", user.Empleado.Nombre));
                claims.Add(new Claim("id_empleado", user.Empleado.IdEmpleado.ToString()));
                
                if (user.Empleado.CentroMedico != null)
                {
                    claims.Add(new Claim("centro_medico", user.Empleado.CentroMedico.Nombre));
                    claims.Add(new Claim("id_centro_medico", user.Empleado.IdCentroMedico.ToString() ?? ""));
                }
                
                if (user.Empleado.Especialidad != null)
                {
                    claims.Add(new Claim("especialidad", user.Empleado.Especialidad.Nombre));
                }
            }

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}