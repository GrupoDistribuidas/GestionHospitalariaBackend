# 🌐 API Gateway - Sistema de Gestión Hospitalaria

## 📋 Descripción

El **API Gateway** es el punto de entrada principal para todas las comunicaciones del frontend con los microservicios del sistema de gestión hospitalaria. Actúa como un proxy inteligente que traduce peticiones HTTP/REST del cliente a llamadas gRPC hacia los microservicios backend.

## 🏗️ Arquitectura

```
┌─────────────┐    HTTP/REST    ┌─────────────┐    gRPC    ┌──────────────────┐
│   Frontend  │ ───────────────► │ API Gateway │ ──────────► │  Microservicios  │
│ (React/Vue/ │                 │             │            │   (Autenticación, │
│  Angular)   │ ◄─────────────── │             │ ◄────────── │  Administración, │
└─────────────┘    JSON         └─────────────┘            │   Consultas...)  │
                                                           └──────────────────┘
```

### Responsabilidades del API Gateway

- ✅ **Traducción de Protocolos**: HTTP/REST ↔ gRPC
- ✅ **Autenticación y Autorización**: Validación de tokens JWT
- ✅ **Enrutamiento**: Dirigir peticiones al microservicio correcto
- ✅ **CORS**: Configuración para permitir acceso desde frontends
- ✅ **Logging**: Registro centralizado de peticiones y respuestas
- ✅ **Documentación**: Swagger/OpenAPI integrado

## 🚀 Configuración y Ejecución

### Prerequisitos

- .NET 9.0 SDK
- Microservicio de Autenticación ejecutándose
- Base de datos MySQL configurada

### Instalación

1. **Clonar el repositorio**:
   ```bash
   git clone [URL_DEL_REPOSITORIO]
   cd ApiGateway
   ```

2. **Restaurar dependencias**:
   ```bash
   dotnet restore
   ```

3. **Compilar el proyecto**:
   ```bash
   dotnet build
   ```

### Configuración

#### appsettings.json / appsettings.Development.json

```json
{
  "Microservices": {
    "AuthenticationService": "http://localhost:5066"
  },
  "Jwt": {
    "Key": "ClaveSuperSecreta123ParaJWT_MuyLarga_32Caracteres",
    "Issuer": "Microservicio.Autenticacion",
    "Audience": "ApiGateway"
  }
}
```

**Configuraciones importantes:**
- `Microservices:AuthenticationService`: URL del microservicio de autenticación
- `Jwt:Key`: Clave secreta para validar tokens JWT (debe coincidir con el microservicio)
- `Jwt:Issuer`: Emisor del token (debe coincidir con el microservicio)

### Ejecución

#### Paso 1: Ejecutar Microservicio de Autenticación
```bash
cd Microservicio.Autenticacion
dotnet run
# Debe ejecutarse en http://localhost:5066
```

#### Paso 2: Ejecutar API Gateway
```bash
cd ApiGateway
dotnet run
# Se ejecutará en http://localhost:5088
```

## 📚 API Endpoints

### 🔐 Autenticación

#### POST /api/auth/login
Autentica un usuario y devuelve un token JWT.

**Request:**
```json
{
  "username": "admin",
  "password": "admin123"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "message": "Login exitoso"
}
```

**Response (401 Unauthorized):**
```json
{
  "success": false,
  "token": "",
  "message": "Credenciales inválidas"
}
```

#### POST /api/auth/validate-token
Valida un token JWT.

**Request:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response (200 OK):**
```json
{
  "isValid": true,
  "username": "admin",
  "message": "Token válido"
}
```

### 🏥 Health Checks

#### GET /health
Estado general del API Gateway.

**Response:**
```json
{
  "status": "OK",
  "service": "ApiGateway",
  "timestamp": "2025-09-23T10:30:00Z"
}
```

#### GET /api/auth/health
Estado del controlador de autenticación.

**Response:**
```json
{
  "status": "OK",
  "service": "ApiGateway - Auth Controller",
  "timestamp": "2025-09-23T10:30:00Z"
}
```

## 🔧 Desarrollo

### Estructura del Proyecto

```
ApiGateway/
├── Controllers/
│   └── AuthController.cs          # Controlador de autenticación
├── Models/
│   └── AuthModels.cs              # DTOs para autenticación
├── Middleware/
│   └── TokenValidationMiddleware.cs # Middleware de validación de tokens
├── Protos/
│   └── auth.proto                 # Definición gRPC
├── Properties/
│   └── launchSettings.json        # Configuración de puertos
├── Program.cs                     # Configuración principal
├── appsettings.json              # Configuración de producción
├── appsettings.Development.json  # Configuración de desarrollo
└── ApiGateway.http               # Pruebas HTTP
```

### Tecnologías Utilizadas

- **ASP.NET Core 9.0**: Framework web
- **gRPC**: Comunicación con microservicios
- **JWT Bearer Authentication**: Autenticación
- **Swagger/OpenAPI**: Documentación de API
- **Serilog**: Logging (opcional)

### Middleware Pipeline

```csharp
app.UseCors("AllowFrontend");           // CORS
app.UseAuthentication();                // Autenticación JWT
app.UseAuthorization();                 // Autorización
// app.UseTokenValidation();            // Validación personalizada (opcional)
app.MapControllers();                   // Controladores
```

## 🧪 Pruebas

### Usando VS Code + REST Client

1. Abrir `ApiGateway.http`
2. Ejecutar las peticiones haciendo clic en "Send Request"

### Usando Swagger UI

1. Navegar a `http://localhost:5088/swagger`
2. Probar los endpoints interactivamente

### Usando PowerShell

```powershell
# Health Check
Invoke-RestMethod -Uri "http://localhost:5088/health" -Method GET

# Login
$loginBody = @{
    username = "admin"
    password = "admin123"
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5088/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"

# Usar el token en peticiones posteriores
$headers = @{
    "Authorization" = "Bearer $($response.token)"
}
```

## 🔒 Seguridad

### CORS Configuration
```csharp
options.AddPolicy("AllowFrontend", policy =>
{
    policy.WithOrigins("http://localhost:3000", "http://localhost:4200", "http://localhost:5173")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});
```

### JWT Validation
- Validación automática de tokens en endpoints protegidos
- Configuración de clave simétrica compartida con microservicios
- Verificación de emisor y audiencia

## 📊 Logging

El API Gateway registra:
- Todas las peticiones HTTP entrantes
- Llamadas gRPC a microservicios
- Errores y excepciones
- Información de autenticación

## 🚨 Manejo de Errores

| Código | Descripción | Escenario |
|--------|-------------|-----------|
| 200 | OK | Operación exitosa |
| 400 | Bad Request | Datos de entrada inválidos |
| 401 | Unauthorized | Token inválido o faltante |
| 500 | Internal Server Error | Error en microservicio o conexión |

## 🔮 Próximos Pasos

- [ ] Implementar Rate Limiting
- [ ] Agregar Circuit Breaker pattern
- [ ] Implementar caching Redis
- [ ] Agregar más microservicios (Administración, Consultas)
- [ ] Implementar API versioning
- [ ] Métricas con Prometheus

## 🛠️ Troubleshooting

### Problemas Comunes

**1. Error de conexión gRPC**
```
Grpc.Core.RpcException: Status(StatusCode="Unavailable", Detail="...")
```
**Solución**: Verificar que el microservicio de autenticación esté ejecutándose en el puerto correcto.

**2. Error JWT**
```
Bearer token validation failed
```
**Solución**: Verificar que la clave JWT sea la misma en ambos servicios.

**3. Error CORS**
```
Access-Control-Allow-Origin
```
**Solución**: Agregar la URL del frontend a la configuración CORS.

## 📞 Soporte

Para reportar bugs o solicitar features, crear un issue en el repositorio.

---

**Versión**: 1.0.0  
**Última actualización**: Septiembre 2025  
**Mantenido por**: Equipo de Desarrollo - Sistema Gestión Hospitalaria