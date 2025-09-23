# ?? Microservicio de Autenticaci�n - Hospital Central

## ? **Estado: PRODUCCI�N**

### **?? Caracter�sticas de Seguridad:**
- **Hashing**: BCrypt con factor de trabajo 11
- **JWT**: Tokens con expiraci�n de 2 horas
- **Validaci�n**: Usuario y empleado activo
- **Logging**: Eventos de seguridad registrados

### **??? Base de Datos:**
- **Esquema**: Integrado con base `hospital_central` existente
- **Relaciones**: Usuario ? Empleado ? CentroMedico/TipoEmpleado/Especialidad
- **Validaci�n**: Solo empleados con estado 'Activo'

### **?? Configuraci�n:**

#### **1. Requisitos:**
- .NET 9
- MySQL Server
- Base de datos `hospital_central` configurada

#### **2. Ejecuci�n:**
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
- **M�todos**: `Login`, `ValidateToken`

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
- `centro_medico`: Nombre del centro m�dico
- `id_centro_medico`: ID del centro m�dico
- `especialidad`: Especialidad del empleado

### **?? Logs esperados:**
```
info: Login exitoso para usuario: {username}
warn: Intento de login fallido para usuario: {username}
```

### **?? Herramientas incluidas:**
- `PasswordHelper`: Utilidades para BCrypt
- `HospitalDbContext`: Contexto EF con todas las relaciones
- Reflexi�n gRPC habilitada para desarrollo

## ?? **Microservicio listo para producci�n**