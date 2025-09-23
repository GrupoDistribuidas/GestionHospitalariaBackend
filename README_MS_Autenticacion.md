# ?? Microservicio de Autenticación - Hospital Central

## ? **Estado: PRODUCCIÓN**

### **?? Características de Seguridad:**
- **Hashing**: BCrypt con factor de trabajo 11
- **JWT**: Tokens con expiración de 2 horas
- **Validación**: Usuario y empleado activo
- **Logging**: Eventos de seguridad registrados

### **??? Base de Datos:**
- **Esquema**: Integrado con base `hospital_central` existente
- **Relaciones**: Usuario ? Empleado ? CentroMedico/TipoEmpleado/Especialidad
- **Validación**: Solo empleados con estado 'Activo'

### **?? Configuración:**

#### **1. Requisitos:**
- .NET 9
- MySQL Server
- Base de datos `hospital_central` configurada

#### **2. Ejecución:**
```bash
cd Microservicio.Autenticacion
dotnet run
```

#### **3. Endpoints disponibles:**
- **HTTPS**: `https://localhost:7297`
- **HTTP**: `http://localhost:5066`

### **?? Pruebas:**

#### **Postman/gRPC:**
- **URL**: `localhost:7297`
- **Servicio**: `AuthService`
- **Métodos**: `Login`, `ValidateToken`

#### **Credenciales de ejemplo:**
```json
{
  "username": "lgomez",
  "password": "clinica2025"
}
```

### **?? JWT Claims incluidos:**
- `NameIdentifier`: ID del usuario
- `Name`: Nombre de usuario
- `Email`: Email del empleado
- `Role`: Tipo de empleado
- `nombre_completo`: Nombre del empleado
- `id_empleado`: ID del empleado
- `centro_medico`: Nombre del centro médico
- `id_centro_medico`: ID del centro médico
- `especialidad`: Especialidad del empleado

### **?? Logs esperados:**
```
info: Login exitoso para usuario: {username}
warn: Intento de login fallido para usuario: {username}
```

### **?? Herramientas incluidas:**
- `PasswordHelper`: Utilidades para BCrypt
- `HospitalDbContext`: Contexto EF con todas las relaciones
- Reflexión gRPC habilitada para desarrollo

## ?? **Microservicio listo para producción**