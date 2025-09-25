using System.Net;
using System.Text.Json;

namespace ApiGateway.Middleware
{
    public class JwtAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtAuthenticationMiddleware> _logger;

        public JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en autenticación JWT");
                await HandleAuthenticationExceptionAsync(context, ex);
            }

            // Manejar respuestas 401 (Unauthorized) de manera personalizada
            if (context.Response.StatusCode == 401 && !context.Response.HasStarted)
            {
                await HandleUnauthorizedAsync(context);
            }
        }

        private static async Task HandleAuthenticationExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "Error de autenticación",
                message = "Token JWT inválido o expirado",
                details = exception.Message,
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }

        private static async Task HandleUnauthorizedAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "No autorizado",
                message = "Se requiere un token JWT válido para acceder a este recurso",
                hint = "Incluye el header: Authorization: Bearer <tu-token-jwt>",
                timestamp = DateTime.UtcNow
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(jsonResponse);
        }
    }
}