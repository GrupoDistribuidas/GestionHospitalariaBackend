using Microservicio.Autenticacion.Services;
using Microservicio.Autenticacion.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

// Configurar Entity Framework con MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<HospitalDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Registrar servicios de autenticación
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Habilitar reflexión gRPC para herramientas como Postman
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddGrpcReflection();
}

var app = builder.Build();

// IMPORTANTE: NO crear tablas automáticamente porque ya existen
// Solo verificar conexión a la base de datos existente
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
        try
        {
            // Solo verificar que podemos conectarnos
            await context.Database.CanConnectAsync();
            app.Logger.LogInformation("Conexión a base de datos existente verificada exitosamente");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Error al conectar con la base de datos existente");
        }
    }
}

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGrpcService<AuthGrpcService>();

// Habilitar reflexión gRPC en desarrollo
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

app.MapGet("/", () => "Microservicio de Autenticación - Hospital Central. Comuníquese con los puntos finales de gRPC a través de un cliente gRPC.");

app.Run();
