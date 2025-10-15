using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microservicio.Administracion.Protos;
using Microservicio.ClinicaExtension.Protos;
using Microservicio.Consultas.Protos;
using ApiGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Controllers y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Gateway - Hospital Management System",
        Version = "v1",
        Description = "API Gateway para el sistema de gestión hospitalaria. Requiere autenticación JWT."
    });

    // Configuración JWT para Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. \r\n\r\n" +
                      "Ingresa 'Bearer' [espacio] y luego tu token en el campo de texto a continuación.\r\n\r\n" +
                      "Ejemplo: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no configurada");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer no configurada");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:4200",
                "http://localhost:5173",
                "http://localhost:5088",   // API Gateway HTTP
                "https://localhost:7221"   // API Gateway HTTPS
            )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// gRPC Clients
builder.Services.AddGrpcClient<MedicosService.MedicosServiceClient>(o =>
{
    o.Address = new Uri("http://10.79.13.31:5100"); // Microservicio Administracion
});

builder.Services.AddGrpcClient<PacientesService.PacientesServiceClient>(o =>
{
    o.Address = new Uri("http://10.79.13.31:5100"); // Microservicio Administracion
});

builder.Services.AddGrpcClient<ConsultasService.ConsultasServiceClient>(o =>
{
    o.Address = new Uri("http://10.79.14.104:5105"); // Microservicio Consultas
});

builder.Services.AddGrpcClient<EspecialidadesService.EspecialidadesServiceClient>(o =>
{
    o.Address = new Uri("http://10.79.13.31:5100"); // Microservicio Administracion
});

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Comentar HTTPS redirect para desarrollo si es necesario
// app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

// Middleware personalizado para manejo de JWT
app.UseMiddleware<JwtAuthenticationMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health
app.MapGet("/health", () => new { status = "OK", service = "ApiGateway", timestamp = DateTime.UtcNow });

app.Run();
