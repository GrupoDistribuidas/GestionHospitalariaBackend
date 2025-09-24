using Microservicio.Consultas.Data;
using Microservicio.Consultas.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework con MySQL/MariaDB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ConsultasDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Agregar gRPC
builder.Services.AddGrpc();

var app = builder.Build();

// Verificar conexión a la base de datos existente
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ConsultasDbContext>();
    try
    {
        context.Database.CanConnect();
        app.Logger.LogInformation("Conexión a base de datos existente verificada exitosamente");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al conectar con la base de datos existente");
    }
}

// Endpoints gRPC
app.MapGrpcService<ConsultasService>();
app.MapGet("/", () => "Microservicio de Consultas - Hospital Central. Comuníquese con los puntos finales de gRPC a través de un cliente gRPC.");

app.Run();
