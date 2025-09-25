using Microsoft.AspNetCore.Mvc;
using Grpc.Net.Client;
using Microservicio.Autenticacion;
using ApiGateway.Models;
using System.ComponentModel.DataAnnotations;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] Models.LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new LoginResponse
                    {
                        Success = false,
                        Message = "Datos de entrada inválidos",
                        Token = ""
                    });
                }

                // Obtener la URL del microservicio de autenticación
                var authServiceUrl = _configuration["Microservices:AuthenticationService"] ??
                    throw new InvalidOperationException("URL del servicio de autenticación no configurada");

                _logger.LogInformation("Intentando login para usuario: {Username}", request.Username);

                // Crear cliente gRPC
                using var channel = GrpcChannel.ForAddress(authServiceUrl);
                var client = new AuthService.AuthServiceClient(channel);

                // Realizar la llamada gRPC
                var grpcRequest = new Microservicio.Autenticacion.LoginRequest
                {
                    Username = request.Username,
                    Password = request.Password
                };

                var grpcResponse = await client.LoginAsync(grpcRequest);

                // Evaluar la respuesta
                var success = !string.IsNullOrEmpty(grpcResponse.Token);

                var response = new LoginResponse
                {
                    Success = success,
                    Token = grpcResponse.Token,
                    Message = grpcResponse.Message
                };

                if (success)
                {
                    _logger.LogInformation("Login exitoso para usuario: {Username}", request.Username);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Login fallido para usuario: {Username}. Mensaje: {Message}", 
                        request.Username, grpcResponse.Message);
                    return Unauthorized(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el proceso de login para usuario: {Username}", request.Username);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "Error interno del servidor",
                    Token = ""
                });
            }
        }

        [HttpPost("validate-token")]
        public async Task<ActionResult<ValidateTokenResponse>> ValidateToken([FromBody] Models.ValidateTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ValidateTokenResponse
                    {
                        IsValid = false,
                        Message = "Token requerido",
                        Username = ""
                    });
                }

                // Obtener la URL del microservicio de autenticación
                var authServiceUrl = _configuration["Microservices:AuthenticationService"] ??
                    throw new InvalidOperationException("URL del servicio de autenticación no configurada");

                _logger.LogInformation("Validando token");

                // Crear cliente gRPC
                using var channel = GrpcChannel.ForAddress(authServiceUrl);
                var client = new AuthService.AuthServiceClient(channel);

                // Realizar la llamada gRPC
                var grpcRequest = new Microservicio.Autenticacion.TokenRequest
                {
                    Token = request.Token
                };

                var grpcResponse = await client.ValidateTokenAsync(grpcRequest);

                var response = new ValidateTokenResponse
                {
                    IsValid = grpcResponse.IsValid,
                    Username = grpcResponse.Username,
                    Message = grpcResponse.IsValid ? "Token válido" : "Token inválido"
                };

                if (grpcResponse.IsValid)
                {
                    _logger.LogInformation("Token válido para usuario: {Username}", grpcResponse.Username);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Token inválido");
                    return Unauthorized(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la validación del token");
                return StatusCode(500, new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = "Error interno del servidor",
                    Username = ""
                });
            }
        }

        [HttpGet("user-info")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetUserInfo()
        {
            try
            {
                // Obtener información del token JWT actual
                var username = User.Identity?.Name;
                var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

                return Ok(new
                {
                    username = username,
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false,
                    claims = claims,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo información del usuario");
                return StatusCode(500, new { error = "Error interno del servidor" });
            }
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "OK", service = "ApiGateway - Auth Controller", timestamp = DateTime.UtcNow });
        }
    }
}