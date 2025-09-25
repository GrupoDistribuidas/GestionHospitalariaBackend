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

# API de Reportes de Consultas Médicas

## Descripción
Este API proporciona endpoints para generar reportes de consultas médicas por médico con capacidades de filtrado avanzadas usando JSON en el body de las peticiones.

## Endpoints Disponibles

### 1. Reporte de Consultas por Médico
**POST** `/api/reportes/consultas-por-medico`

Obtiene un reporte detallado de todas las consultas realizadas por cada médico con filtros opcionales enviados en el body como JSON.

#### Body de la petición (JSON):
```json
{
  "idMedico": 123,                    // Opcional - ID del médico específico
  "fechaInicio": "2024-01-01",        // Opcional - Fecha inicio formato yyyy-MM-dd
  "fechaFin": "2024-12-31",           // Opcional - Fecha fin formato yyyy-MM-dd
  "motivo": "control",                // Opcional - Filtro por motivo de consulta
  "diagnostico": "hipertensión"       // Opcional - Filtro por diagnóstico
}
```

#### Ejemplos de uso:

1. **Obtener todas las consultas de todos los médicos:**
   ```json
   POST /api/reportes/consultas-por-medico
   Content-Type: application/json
   
   {}
   ```

2. **Filtrar por médico específico:**
   ```json
   POST /api/reportes/consultas-por-medico
   Content-Type: application/json
   
   {
     "idMedico": 123
   }
   ```

3. **Filtrar por rango de fechas:**
   ```json
   POST /api/reportes/consultas-por-medico
   Content-Type: application/json
   
   {
     "fechaInicio": "2024-01-01",
     "fechaFin": "2024-12-31"
   }
   ```

4. **Filtrar por motivo y diagnóstico:**
   ```json
   POST /api/reportes/consultas-por-medico
   Content-Type: application/json
   
   {
     "motivo": "urgencia",
     "diagnostico": "hipertensión"
   }
   ```

5. **Filtro combinado completo:**
   ```json
   POST /api/reportes/consultas-por-medico
   Content-Type: application/json
   
   {
     "idMedico": 123,
     "fechaInicio": "2024-06-01",
     "fechaFin": "2024-06-30",
     "motivo": "control",
     "diagnostico": "diabetes"
   }
   ```

#### Respuesta:
```json
{
  "resumen": {
    "totalConsultasGeneral": 250,
    "totalMedicos": 8,
    "fechaGeneracion": "2024-09-25 14:30:00",
    "filtros": {
      "medicoId": null,
      "fechaInicio": "2024-01-01",
      "fechaFin": "2024-12-31",
      "especialidadId": null,
      "tipoConsulta": null
    }
  },
  "medicos": [
    {
      "idMedico": 123,
      "nombreMedico": "Dr. Juan Pérez",
      "idEspecialidad": 2,
      "nombreEspecialidad": "Especialidad ID: 2",
      "totalConsultas": 45,
      "consultas": [
        {
          "idConsulta": 1001,
          "fecha": "2024-09-15",
          "hora": "09:30:00",
          "motivo": "Control rutinario",
          "diagnostico": "Paciente en buen estado",
          "tratamiento": "Continuar medicación actual",
          "paciente": {
            "idPaciente": 456,
            "nombrePaciente": "María García"
          }
        }
      ]
    }
  ]
}
```

### 2. Estadísticas de Consultas
**POST** `/api/reportes/estadisticas-consultas`

Obtiene estadísticas resumidas de las consultas médicas usando JSON en el body.

#### Body de la petición (JSON):
```json
{
  "fechaInicio": "2024-01-01",        // Opcional - Fecha inicio formato yyyy-MM-dd
  "fechaFin": "2024-12-31"            // Opcional - Fecha fin formato yyyy-MM-dd
}
```

#### Ejemplos de uso:

1. **Estadísticas generales:**
   ```json
   POST /api/reportes/estadisticas-consultas
   Content-Type: application/json
   
   {}
   ```

2. **Estadísticas por período:**
   ```json
   POST /api/reportes/estadisticas-consultas
   Content-Type: application/json
   
   {
     "fechaInicio": "2024-01-01",
     "fechaFin": "2024-06-30"
   }
   ```

#### Respuesta:
```json
{
  "totalConsultas": 250,
  "totalMedicos": 8,
  "fechaGeneracion": "2024-09-25 14:30:00",
  "promedioConsultasPorMedico": 31.25,
  "medicoConMasConsultas": "Dr. Juan Pérez",
  "maxConsultasPorMedico": 45,
  "especialidades": [
    {
      "idEspecialidad": 2,
      "nombreEspecialidad": "Especialidad ID: 2",
      "totalMedicos": 3,
      "totalConsultas": 120
    },
    {
      "idEspecialidad": 1,
      "nombreEspecialidad": "Especialidad ID: 1",
      "totalMedicos": 2,
      "totalConsultas": 80
    }
  ]
}
```

## Códigos de Respuesta

- **200 OK**: Consulta exitosa
- **400 Bad Request**: Parámetros inválidos (formato de fecha incorrecto, fecha inicio mayor que fecha fin)
- **404 Not Found**: No se encontraron datos para los filtros especificados
- **500 Internal Server Error**: Error interno del servidor

## Notas Importantes

1. **Formato de Fechas**: Todas las fechas deben estar en formato `yyyy-MM-dd` (ISO 8601)
2. **Filtros Opcionales**: Todos los parámetros de filtro son opcionales
3. **Rendimiento**: Para rangos de fechas muy amplios, considerar paginar los resultados
4. **Dependencias**: Requiere que los microservicios de Administración y Consultas estén funcionando
5. **Especialidades**: Actualmente se muestra como "Especialidad ID: X" - se puede mejorar para mostrar nombres reales

## Casos de Uso Comunes

1. **Reporte mensual por médico**: Filtrar por médico específico y rango de un mes
2. **Análisis de productividad**: Usar estadísticas para comparar médicos
3. **Auditoría por especialidad**: Filtrar por especialidad específica
4. **Reportes de urgencias**: Filtrar por tipo de consulta "urgencia"
5. **Análisis temporal**: Comparar períodos usando diferentes rangos de fechas

## Mejoras Futuras

1. Agregar paginación para grandes volúmenes de datos
2. Implementar caché para consultas frecuentes
3. Agregar exportación a PDF/Excel
4. Incluir nombres reales de especialidades
5. Agregar métricas de tiempo promedio por consulta
6. Implementar filtros adicionales (estado del paciente, duración de consulta, etc.)

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