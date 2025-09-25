using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microservicio.Administracion.Protos;
using Microservicio.ClinicaExtension.Protos;
using Microservicio.Consultas.Protos;

var builder = WebApplication.CreateBuilder(args);

// Controllers y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    o.Address = new Uri("http://localhost:5100"); // Microservicio Administracion
});

builder.Services.AddGrpcClient<PacientesService.PacientesServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:5100"); // Microservicio Administracion
});

builder.Services.AddGrpcClient<ConsultasService.ConsultasServiceClient>(o =>
{
    o.Address = new Uri("http://localhost:5105"); // Microservicio Consultas
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health
app.MapGet("/health", () => new { status = "OK", service = "ApiGateway", timestamp = DateTime.UtcNow });

app.Run();
