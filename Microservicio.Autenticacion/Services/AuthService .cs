namespace Microservicio.Autenticacion.Services
{
    using BCrypt.Net;
    using Grpc.Core;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microservicio.Autenticacion;

    public class AuthGrpcService : AuthService.AuthServiceBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthGrpcService> _logger;
        private readonly IConfiguration _config;

        public AuthGrpcService(IAuthenticationService authService, ILogger<AuthGrpcService> logger, IConfiguration config)
        {
            _authService = authService;
            _logger = logger;
            _config = config;
        }

        public override async Task<LoginReply> Login(LoginRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Intento de login para usuario: '{Username}'", request.Username);

                if (string.IsNullOrEmpty(request.Username))
                {
                    _logger.LogWarning("Username requerido");
                    return new LoginReply
                    {
                        Message = "Username requerido",
                        Token = ""
                    };
                }

                if (string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("Password requerido");
                    return new LoginReply
                    {
                        Message = "Password requerido",
                        Token = ""
                    };
                }

                // Validar credenciales contra la base de datos
                var user = await _authService.ValidateUserAsync(request.Username, request.Password);

                if (user == null)
                {
                    _logger.LogWarning("Login fallido para usuario: '{Username}'", request.Username);
                    return new LoginReply
                    {
                        Message = "Credenciales inválidas",
                        Token = ""
                    };
                }

                // Generar token JWT
                var token = await _authService.GenerateJwtToken(user);

                _logger.LogInformation("Login exitoso para usuario: '{Username}'", request.Username);
                
                return new LoginReply
                {
                    Message = "Login exitoso",
                    Token = token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de login para usuario: '{Username}'", request.Username ?? "NULL");
                return new LoginReply
                {
                    Message = "Error interno del servidor",
                    Token = ""
                };
            }
        }

        public override async Task<TokenReply> ValidateToken(TokenRequest request, ServerCallContext context)
        {
            var handler = new JwtSecurityTokenHandler();
            try
            {
                _logger.LogInformation("Validando token");

                var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ??
                    throw new InvalidOperationException("Jwt:Key no configurada"));

                var validations = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _config["Jwt:Issuer"] ?? "Microservicio.Autenticacion",
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero // No tolerancia para tiempo de expiración
                };

                var principal = handler.ValidateToken(request.Token, validations, out var validatedToken);

                // Obtener información del usuario del token
                var username = principal.Identity?.Name;

                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogWarning("Token válido pero sin username");
                    return new TokenReply
                    {
                        IsValid = false,
                        Username = ""
                    };
                }

                // Verificar que el usuario siga activo en la base de datos
                var user = await _authService.GetUserByUsernameAsync(username);
                if (user == null || user.Empleado == null || !user.Empleado.Activo)
                {
                    _logger.LogWarning("Usuario en token ya no está activo: {Username}", username);
                    return new TokenReply
                    {
                        IsValid = false,
                        Username = ""
                    };
                }

                _logger.LogInformation("Token validado exitosamente para usuario: {Username}", username);
                return new TokenReply
                {
                    IsValid = true,
                    Username = username
                };
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token expirado");
                return new TokenReply
                {
                    IsValid = false,
                    Username = ""
                };
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("Token inválido: {Message}", ex.Message);
                return new TokenReply
                {
                    IsValid = false,
                    Username = ""
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la validación del token");
                return new TokenReply
                {
                    IsValid = false,
                    Username = ""
                };
            }
        }
    }
}
