using Microservicio.Administracion.Data;
using Microservicio.Administracion.Services;
using Microservicio.ClinicaExtension.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configurar Entity Framework para Hospital Central (medicos)
var connectionStringCentral = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AdministracionDbContext>(options =>
    options.UseMySql(connectionStringCentral, ServerVersion.AutoDetect(connectionStringCentral)));

// Configurar Entity Framework para Clinica Extension (pacientes)
var connectionStringClinica = builder.Configuration.GetConnectionString("ClinicaExtension")
    ?? throw new InvalidOperationException("Connection string 'ClinicaExtension' not found.");

builder.Services.AddDbContext<ClinicaExtensionDbContext>(options =>
    options.UseMySql(connectionStringClinica, ServerVersion.AutoDetect(connectionStringClinica)));

// Agregar gRPC
builder.Services.AddGrpc();

var app = builder.Build();

// Verificar conexión a la base de datos Hospital Central
using (var scope = app.Services.CreateScope())
{
    var contextCentral = scope.ServiceProvider.GetRequiredService<AdministracionDbContext>();
    try
    {
        contextCentral.Database.CanConnect();
        app.Logger.LogInformation("Conexión a base Hospital Central verificada exitosamente");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al conectar con la base Hospital Central");
    }

    // Verificar conexión a la base de la Clinica Extension
    var contextClinica = scope.ServiceProvider.GetRequiredService<ClinicaExtensionDbContext>();
    try
    {
        contextClinica.Database.CanConnect();
        app.Logger.LogInformation("Conexión a base Clinica Extension verificada exitosamente");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al conectar con la base Clinica Extension");
    }
}

// Endpoints gRPC
app.MapGrpcService<MedicosServiceImpl>();
app.MapGrpcService<PacientesServiceImpl>();
app.MapGrpcService<EspecialidadesServiceImpl>();
app.MapGrpcService<UsuariosServiceImpl>();

app.MapGet("/", () => "Microservicio de Administración - Hospital Central. Comuníquese con los puntos finales de gRPC a través de un cliente gRPC.");

app.Run();
