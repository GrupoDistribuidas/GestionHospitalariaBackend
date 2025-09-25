using Microsoft.EntityFrameworkCore;
using Microservicio.Administracion.Data;
using Microservicio.Administracion.Services;

var builder = WebApplication.CreateBuilder(args);

// gRPC
builder.Services.AddGrpc();

// 1) Cadena de conexión: intenta "MySql" y si no existe usa "DefaultConnection"
var cs =
    builder.Configuration.GetConnectionString("MySql")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "No encontré ninguna cadena de conexión ('ConnectionStrings:MySql' ni 'ConnectionStrings:DefaultConnection').");

// 2) Registrar **el mismo DbContext** que inyecta tu servicio (HospitalContext)
builder.Services.AddDbContext<HospitalContext>(o =>
    // MySQL 8.x (Oracle)
    o.UseMySql(cs, new MySqlServerVersion(new Version(8, 0, 36)))
    // Si usas MariaDB, comenta la línea de arriba y usa la de Pomelo:
    // o.UseMySql(cs, Pomelo.EntityFrameworkCore.MySql.Infrastructure
    //     .ServerVersion.Create(new Version(11, 4, 2), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MariaDb))
);

// 3) Build
var app = builder.Build();

// 4) (Opcional) Verificar conexión a BD con el **HospitalContext**
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HospitalContext>();
    try
    {
        var ok = await db.Database.CanConnectAsync();
        if (ok) app.Logger.LogInformation("Conexión a MySQL verificada correctamente.");
        else    app.Logger.LogWarning("No se pudo establecer conexión con la base de datos.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error al conectar con la base de datos.");
    }
}

// 5) Mapear servicios gRPC que **sí** existen
app.MapGrpcService<EspecialidadesGrpcService>();
// Si tienes el servicio de médicos, descomenta y usa el nombre real de la clase:
// app.MapGrpcService<MedicosGrpcService>();

app.MapGet("/", () => "Administracion gRPC running");
app.Run();
