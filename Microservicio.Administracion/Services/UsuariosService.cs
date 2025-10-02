using Grpc.Core;
using Microservicio.Administracion.Data;
using Microservicio.Administracion.Models;
using Microservicio.Administracion.Protos;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace Microservicio.Administracion.Services
{
    public class UsuariosServiceImpl : UsuariosService.UsuariosServiceBase
    {
        private readonly AdministracionDbContext _dbContext;
        private readonly ILogger<UsuariosServiceImpl> _logger;

        public UsuariosServiceImpl(AdministracionDbContext dbContext, ILogger<UsuariosServiceImpl> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public override async Task<UsuarioResponse> ObtenerUsuarioPorId(UsuarioPorIdRequest request, ServerCallContext context)
        {
            try
            {
                var usuario = await _dbContext.Usuarios
                    .Include(u => u.Empleado)
                    .FirstOrDefaultAsync(u => u.IdUsuario == request.IdUsuario);

                if (usuario == null)
                {
                    return new UsuarioResponse
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    };
                }

                return new UsuarioResponse
                {
                    Success = true,
                    Message = "Usuario encontrado exitosamente",
                    Usuario = MapToUsuarioData(usuario)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por ID: {Id}", request.IdUsuario);
                return new UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        public override async Task<UsuarioResponse> ObtenerUsuarioPorNombre(UsuarioPorNombreRequest request, ServerCallContext context)
        {
            try
            {
                var usuario = await _dbContext.Usuarios
                    .Include(u => u.Empleado)
                    .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario);

                if (usuario == null)
                {
                    return new UsuarioResponse
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    };
                }

                return new UsuarioResponse
                {
                    Success = true,
                    Message = "Usuario encontrado exitosamente",
                    Usuario = MapToUsuarioData(usuario)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por nombre: {Nombre}", request.NombreUsuario);
                return new UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        public override async Task<UsuariosListResponse> ObtenerTodosUsuarios(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            try
            {
                var usuarios = await _dbContext.Usuarios
                    .Include(u => u.Empleado)
                    .ToListAsync();

                var response = new UsuariosListResponse
                {
                    Success = true,
                    Message = "Usuarios obtenidos exitosamente"
                };

                response.Usuarios.AddRange(usuarios.Select(MapToUsuarioData));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los usuarios");
                return new UsuariosListResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        public override async Task<UsuarioResponse> InsertarUsuario(InsertarUsuarioRequest request, ServerCallContext context)
        {
            try
            {
                // Verificar si el nombre de usuario ya existe
                var usuarioExistente = await _dbContext.Usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario);

                if (usuarioExistente != null)
                {
                    return new UsuarioResponse
                    {
                        Success = false,
                        Message = "El nombre de usuario ya existe"
                    };
                }

                // Verificar si el empleado existe (si se proporciona)
                if (request.IdEmpleado > 0)
                {
                    var empleadoExiste = await _dbContext.Empleados
                        .AnyAsync(e => e.IdEmpleado == request.IdEmpleado);

                    if (!empleadoExiste)
                    {
                        return new UsuarioResponse
                        {
                            Success = false,
                            Message = "El empleado especificado no existe"
                        };
                    }
                }

                // Encriptar la contraseña
                var contraseñaEncriptada = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);

                var usuario = new Usuario
                {
                    NombreUsuario = request.NombreUsuario,
                    Contraseña = contraseñaEncriptada,
                    Rol = request.Rol ?? "Usuario",
                    IdEmpleado = request.IdEmpleado > 0 ? request.IdEmpleado : null
                };

                _dbContext.Usuarios.Add(usuario);
                await _dbContext.SaveChangesAsync();

                // Recargar el usuario con las relaciones
                var usuarioCreado = await _dbContext.Usuarios
                    .Include(u => u.Empleado)
                    .FirstOrDefaultAsync(u => u.IdUsuario == usuario.IdUsuario);

                return new UsuarioResponse
                {
                    Success = true,
                    Message = "Usuario creado exitosamente",
                    Usuario = MapToUsuarioData(usuarioCreado!)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al insertar usuario: {NombreUsuario}", request.NombreUsuario);
                return new UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        public override async Task<UsuarioResponse> ActualizarUsuario(ActualizarUsuarioRequest request, ServerCallContext context)
        {
            try
            {
                var usuario = await _dbContext.Usuarios.FindAsync(request.IdUsuario);
                if (usuario == null)
                {
                    return new UsuarioResponse
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    };
                }

                // Verificar si el nombre de usuario ya existe (excepto el actual)
                var usuarioExistente = await _dbContext.Usuarios
                    .FirstOrDefaultAsync(u => u.NombreUsuario == request.NombreUsuario && u.IdUsuario != request.IdUsuario);

                if (usuarioExistente != null)
                {
                    return new UsuarioResponse
                    {
                        Success = false,
                        Message = "El nombre de usuario ya existe"
                    };
                }

                // Verificar si el empleado existe (si se proporciona)
                if (request.IdEmpleado > 0)
                {
                    var empleadoExiste = await _dbContext.Empleados
                        .AnyAsync(e => e.IdEmpleado == request.IdEmpleado);

                    if (!empleadoExiste)
                    {
                        return new UsuarioResponse
                        {
                            Success = false,
                            Message = "El empleado especificado no existe"
                        };
                    }
                }

                // Actualizar campos
                usuario.NombreUsuario = request.NombreUsuario;
                usuario.Rol = request.Rol;
                usuario.IdEmpleado = request.IdEmpleado > 0 ? request.IdEmpleado : null;

                // Solo actualizar contraseña si se proporciona una nueva
                if (!string.IsNullOrEmpty(request.Contrasena))
                {
                    usuario.Contraseña = BCrypt.Net.BCrypt.HashPassword(request.Contrasena);
                }

                await _dbContext.SaveChangesAsync();

                // Recargar el usuario con las relaciones
                var usuarioActualizado = await _dbContext.Usuarios
                    .Include(u => u.Empleado)
                    .FirstOrDefaultAsync(u => u.IdUsuario == usuario.IdUsuario);

                return new UsuarioResponse
                {
                    Success = true,
                    Message = "Usuario actualizado exitosamente",
                    Usuario = MapToUsuarioData(usuarioActualizado!)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario: {Id}", request.IdUsuario);
                return new UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        public override async Task<EliminarUsuarioResponse> EliminarUsuario(EliminarUsuarioRequest request, ServerCallContext context)
        {
            try
            {
                var usuario = await _dbContext.Usuarios.FindAsync(request.IdUsuario);
                if (usuario == null)
                {
                    return new EliminarUsuarioResponse
                    {
                        Success = false,
                        Message = "Usuario no encontrado"
                    };
                }

                _dbContext.Usuarios.Remove(usuario);
                await _dbContext.SaveChangesAsync();

                return new EliminarUsuarioResponse
                {
                    Success = true,
                    Message = "Usuario eliminado exitosamente"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario: {Id}", request.IdUsuario);
                return new EliminarUsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                };
            }
        }

        private UsuarioData MapToUsuarioData(Usuario usuario)
        {
            var usuarioData = new UsuarioData
            {
                IdUsuario = usuario.IdUsuario,
                NombreUsuario = usuario.NombreUsuario,
                Rol = usuario.Rol,
                IdEmpleado = usuario.IdEmpleado ?? 0
            };

            if (usuario.Empleado != null)
            {
                usuarioData.Empleado = new EmpleadoData
                {
                    IdEmpleado = usuario.Empleado.IdEmpleado,
                    Nombre = usuario.Empleado.Nombre,
                    Email = usuario.Empleado.Email ?? "",
                    Telefono = usuario.Empleado.Telefono ?? ""
                };
            }

            return usuarioData;
        }
    }
}