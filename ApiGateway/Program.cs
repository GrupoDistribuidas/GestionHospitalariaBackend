using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ApiGateway.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configurar OpenAPI/Swagger
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Configurar autenticación JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? 
    throw new InvalidOperationException("Jwt:Key no configurada");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? 
    throw new InvalidOperationException("Jwt:Issuer no configurada");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = false, // Permitir múltiples audiencias
            ClockSkew = TimeSpan.Zero
        };
    });

// Configurar autorización
builder.Services.AddAuthorization();

// Configurar CORS para el frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:4200", "http://localhost:5173") // React, Angular, Vite
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Configurar CORS
app.UseCors("AllowFrontend");

// Configurar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Usar middleware personalizado de validación de tokens (comentado por ahora)
// app.UseTokenValidation();

// Mapear controladores
app.MapControllers();

// Endpoint de salud general
app.MapGet("/health", () => new { 
    status = "OK", 
    service = "ApiGateway", 
    timestamp = DateTime.UtcNow 
});

app.Run();
