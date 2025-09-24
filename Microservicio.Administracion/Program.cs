using Microservicio.Administracion.Data;
using Microservicio.Administracion.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework con MySQL/MariaDB
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AdministracionDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Agregar gRPC
builder.Services.AddGrpc();

var app = builder.Build();

// Verificar conexión a la base de datos existente
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AdministracionDbContext>();
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
app.MapGrpcService<MedicosService>();
app.MapGet("/", () => "Microservicio de Administración - Hospital Central. Comuníquese con los puntos finales de gRPC a través de un cliente gRPC.");

app.Run();
