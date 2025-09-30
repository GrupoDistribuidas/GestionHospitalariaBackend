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
        private readonly IEmailService _emailService;

        public AuthenticationService(HospitalDbContext context, IConfiguration config, ILogger<AuthenticationService> logger, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _emailService = emailService;
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

        public async Task<bool> SendPasswordByEmailAsync(string nombreUsuario)
        {
            try
            {
                if (string.IsNullOrEmpty(nombreUsuario))
                {
                    _logger.LogWarning("Nombre de usuario vacío en solicitud de recuperación de contraseña");
                    return false;
                }

                var user = await _context.Usuarios
                    .Include(u => u.Empleado)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario && 
                                           u.Empleado != null && 
                                           u.Empleado.Estado == "Activo");

                if (user == null || user.Empleado == null)
                {
                    _logger.LogWarning("Usuario no encontrado o inactivo para recuperación de contraseña: {Username}", nombreUsuario);
                    return false;
                }

                if (string.IsNullOrEmpty(user.Empleado.Email))
                {
                    _logger.LogWarning("Usuario no tiene email configurado para recuperación de contraseña: {Username}", nombreUsuario);
                    return false;
                }

                // Enviar el correo con la contraseña actual
                var emailSent = await _emailService.SendPasswordByEmailAsync(
                    user.Empleado.Email,
                    user.Empleado.Nombre,
                    user.NombreUsuario,
                    user.Contraseña
                );

                if (emailSent)
                {
                    _logger.LogInformation("Correo de recuperación de contraseña enviado exitosamente para usuario: {Username}", nombreUsuario);
                }
                else
                {
                    _logger.LogError("Error al enviar correo de recuperación de contraseña para usuario: {Username}", nombreUsuario);
                }

                return emailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al procesar solicitud de recuperación de contraseña para usuario: {Username}", nombreUsuario);
                return false;
            }
        }
    }
}