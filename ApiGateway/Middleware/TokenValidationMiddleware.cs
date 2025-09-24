using Grpc.Net.Client;
using Microservicio.Autenticacion;
using System.Text.Json;

namespace ApiGateway.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Rutas que no requieren autenticación
            var publicPaths = new[]
            {
                "/api/auth/login",
                "/api/auth/health",
                "/health",
                "/swagger",
                "/api/auth/validate-token" // Para permitir validación externa
            };

            var path = context.Request.Path.Value?.ToLowerInvariant();
            
            // Si es una ruta pública, continuar sin validación
            if (publicPaths.Any(publicPath => path?.StartsWith(publicPath.ToLowerInvariant()) == true))
            {
                await _next(context);
                return;
            }

            // Obtener el token del header Authorization
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                await HandleUnauthorized(context, "Token de acceso requerido");
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                // Validar el token con el microservicio de autenticación
                var isValid = await ValidateTokenWithAuthService(token);

                if (!isValid)
                {
                    await HandleUnauthorized(context, "Token inválido o expirado");
                    return;
                }

                // Si el token es válido, continuar con la siguiente middleware
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la validación del token");
                await HandleUnauthorized(context, "Error interno durante la validación del token");
            }
        }

        private async Task<bool> ValidateTokenWithAuthService(string token)
        {
            try
            {
                var authServiceUrl = _configuration["Microservices:AuthenticationService"] ??
                    throw new InvalidOperationException("URL del servicio de autenticación no configurada");

                using var channel = GrpcChannel.ForAddress(authServiceUrl);
                var client = new AuthService.AuthServiceClient(channel);

                var request = new TokenRequest { Token = token };
                var response = await client.ValidateTokenAsync(request);

                return response.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar token con el servicio de autenticación");
                return false;
            }
        }

        private async Task HandleUnauthorized(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new { error = "Unauthorized", message = message };
            var jsonResponse = JsonSerializer.Serialize(response);

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    // Extension method para registrar el middleware
    public static class TokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenValidationMiddleware>();
        }
    }
}