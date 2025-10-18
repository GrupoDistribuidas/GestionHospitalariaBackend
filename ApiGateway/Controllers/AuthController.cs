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

                // Intentar la llamada gRPC con reintentos para manejar condiciones de arranque/transitorias
                Microservicio.Autenticacion.LoginReply? grpcResponse = null;
                int maxAttemptsLogin = 3;
                for (int attempt = 1; attempt <= maxAttemptsLogin; attempt++)
                {
                    try
                    {
                        grpcResponse = await client.LoginAsync(grpcRequest);
                        break;
                    }
                    catch (Grpc.Core.RpcException rpcEx) when (rpcEx.StatusCode == Grpc.Core.StatusCode.Unavailable || rpcEx.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
                    {
                        _logger.LogWarning(rpcEx, "Intento {Attempt} fallido (RpcException) al llamar a AuthService.LoginAsync", attempt);
                    }
                    catch (System.Net.Sockets.SocketException sockEx)
                    {
                        _logger.LogWarning(sockEx, "Intento {Attempt} fallido (SocketException) al llamar a AuthService.LoginAsync", attempt);
                    }

                    if (attempt < maxAttemptsLogin)
                        await Task.Delay(1000 * attempt); // backoff simple: 1s, 2s, ...
                }

                if (grpcResponse == null)
                {
                    throw new InvalidOperationException($"No se pudo conectar al servicio de autenticación en {authServiceUrl} después de {maxAttemptsLogin} intentos.");
                }

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

                // Reintentos para la validación de token
                Microservicio.Autenticacion.TokenReply? grpcResponse = null;
                int maxAttemptsToken = 3;
                for (int attempt = 1; attempt <= maxAttemptsToken; attempt++)
                {
                    try
                    {
                        grpcResponse = await client.ValidateTokenAsync(grpcRequest);
                        break;
                    }
                    catch (Grpc.Core.RpcException rpcEx) when (rpcEx.StatusCode == Grpc.Core.StatusCode.Unavailable || rpcEx.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
                    {
                        _logger.LogWarning(rpcEx, "Intento {Attempt} fallido (RpcException) al llamar a AuthService.ValidateTokenAsync", attempt);
                    }
                    catch (System.Net.Sockets.SocketException sockEx)
                    {
                        _logger.LogWarning(sockEx, "Intento {Attempt} fallido (SocketException) al llamar a AuthService.ValidateTokenAsync", attempt);
                    }

                    if (attempt < maxAttemptsToken)
                        await Task.Delay(1000 * attempt);
                }

                if (grpcResponse == null)
                {
                    throw new InvalidOperationException($"No se pudo conectar al servicio de autenticación en {authServiceUrl} después de {maxAttemptsToken} intentos.");
                }

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

        [HttpPost("forgot-password")]
        public async Task<ActionResult<Models.PasswordRecoveryResponse>> ForgotPassword([FromBody] Models.PasswordRecoveryRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new Models.PasswordRecoveryResponse
                    {
                        Success = false,
                        Message = "Datos de entrada inválidos"
                    });
                }

                // Obtener la URL del microservicio de autenticación
                var authServiceUrl = _configuration["Microservices:AuthenticationService"] ??
                    throw new InvalidOperationException("URL del servicio de autenticación no configurada");

                _logger.LogInformation("Solicitud de recuperación de contraseña para usuario: {Username}", request.Username);

                // Crear cliente gRPC
                using var channel = GrpcChannel.ForAddress(authServiceUrl);
                var client = new AuthService.AuthServiceClient(channel);

                // Realizar la llamada gRPC
                var grpcRequest = new Microservicio.Autenticacion.PasswordRecoveryRequest
                {
                    Username = request.Username
                };

                // Reintentos para el envío de recuperación de contraseña
                Microservicio.Autenticacion.PasswordRecoveryReply? grpcResponse = null;
                int maxAttemptsPwd = 3;
                for (int attempt = 1; attempt <= maxAttemptsPwd; attempt++)
                {
                    try
                    {
                        grpcResponse = await client.SendPasswordByEmailAsync(grpcRequest);
                        break;
                    }
                    catch (Grpc.Core.RpcException rpcEx) when (rpcEx.StatusCode == Grpc.Core.StatusCode.Unavailable || rpcEx.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded)
                    {
                        _logger.LogWarning(rpcEx, "Intento {Attempt} fallido (RpcException) al llamar a AuthService.SendPasswordByEmailAsync", attempt);
                    }
                    catch (System.Net.Sockets.SocketException sockEx)
                    {
                        _logger.LogWarning(sockEx, "Intento {Attempt} fallido (SocketException) al llamar a AuthService.SendPasswordByEmailAsync", attempt);
                    }

                    if (attempt < maxAttemptsPwd)
                        await Task.Delay(1000 * attempt);
                }

                if (grpcResponse == null)
                {
                    throw new InvalidOperationException($"No se pudo conectar al servicio de autenticación en {authServiceUrl} después de {maxAttemptsPwd} intentos.");
                }

                var response = new Models.PasswordRecoveryResponse
                {
                    Success = grpcResponse.Success,
                    Message = grpcResponse.Message
                };

                if (grpcResponse.Success)
                {
                    _logger.LogInformation("Recuperación de contraseña exitosa para usuario: {Username}", request.Username);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("Error en recuperación de contraseña para usuario: {Username}. Mensaje: {Message}", 
                        request.Username, grpcResponse.Message);
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado durante recuperación de contraseña para usuario: {Username}", request.Username);
                return StatusCode(500, new Models.PasswordRecoveryResponse
                {
                    Success = false,
                    Message = "Error interno del servidor. Intenta nuevamente más tarde."
                });
            }
        }
    }
}