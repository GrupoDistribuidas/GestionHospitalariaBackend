using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Grpc.Net.Client;
using Microservicio.Administracion.Protos;
using ApiModels = ApiGateway.Models;
using GrpcModels = Microservicio.Administracion.Protos;

namespace ApiGateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly ILogger<UsuariosController> _logger;
        private readonly IConfiguration _configuration;

        public UsuariosController(ILogger<UsuariosController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        private GrpcChannel CreateGrpcChannel()
        {
            var administracionUrl = _configuration["Grpc:AdministracionUrl"] ?? "http://administracion:5100";
            return GrpcChannel.ForAddress(administracionUrl);
        }

        /// <summary>
        /// Obtiene todos los usuarios
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiModels.UsuariosListResponse>> ObtenerTodosUsuarios()
        {
            try
            {
                using var channel = CreateGrpcChannel();
                var client = new UsuariosService.UsuariosServiceClient(channel);

                var response = await client.ObtenerTodosUsuariosAsync(new Google.Protobuf.WellKnownTypes.Empty());

                var usuariosModels = response.Usuarios.Select(u => new ApiModels.UsuarioModel
                {
                    IdUsuario = u.IdUsuario,
                    NombreUsuario = u.NombreUsuario,
                    Rol = u.Rol,
                    IdEmpleado = u.IdEmpleado,
                    Empleado = u.Empleado != null ? new ApiModels.EmpleadoModel
                    {
                        IdEmpleado = u.Empleado.IdEmpleado,
                        Nombre = u.Empleado.Nombre,
                        Email = u.Empleado.Email,
                        Telefono = u.Empleado.Telefono
                    } : null
                }).ToList();

                return Ok(new ApiModels.UsuariosListResponse
                {
                    Success = response.Success,
                    Message = response.Message,
                    Usuarios = usuariosModels
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(500, new ApiModels.UsuariosListResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Obtiene un usuario por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiModels.UsuarioResponse>> ObtenerUsuarioPorId(int id)
        {
            try
            {
                using var channel = CreateGrpcChannel();
                var client = new UsuariosService.UsuariosServiceClient(channel);

                var response = await client.ObtenerUsuarioPorIdAsync(new UsuarioPorIdRequest { IdUsuario = id });

                if (!response.Success)
                {
                    return NotFound(new ApiModels.UsuarioResponse
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                var usuarioModel = new ApiModels.UsuarioModel
                {
                    IdUsuario = response.Usuario.IdUsuario,
                    NombreUsuario = response.Usuario.NombreUsuario,
                    Rol = response.Usuario.Rol,
                    IdEmpleado = response.Usuario.IdEmpleado,
                    Empleado = response.Usuario.Empleado != null ? new ApiModels.EmpleadoModel
                    {
                        IdEmpleado = response.Usuario.Empleado.IdEmpleado,
                        Nombre = response.Usuario.Empleado.Nombre,
                        Email = response.Usuario.Empleado.Email,
                        Telefono = response.Usuario.Empleado.Telefono
                    } : null
                };

                return Ok(new ApiModels.UsuarioResponse
                {
                    Success = response.Success,
                    Message = response.Message,
                    Usuario = usuarioModel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por ID: {Id}", id);
                return StatusCode(500, new ApiModels.UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Obtiene un usuario por nombre de usuario
        /// </summary>
        [HttpGet("buscar/{nombreUsuario}")]
        public async Task<ActionResult<ApiModels.UsuarioResponse>> ObtenerUsuarioPorNombre(string nombreUsuario)
        {
            try
            {
                using var channel = CreateGrpcChannel();
                var client = new UsuariosService.UsuariosServiceClient(channel);

                var response = await client.ObtenerUsuarioPorNombreAsync(new UsuarioPorNombreRequest { NombreUsuario = nombreUsuario });

                if (!response.Success)
                {
                    return NotFound(new ApiModels.UsuarioResponse
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                var usuarioModel = new ApiModels.UsuarioModel
                {
                    IdUsuario = response.Usuario.IdUsuario,
                    NombreUsuario = response.Usuario.NombreUsuario,
                    Rol = response.Usuario.Rol,
                    IdEmpleado = response.Usuario.IdEmpleado,
                    Empleado = response.Usuario.Empleado != null ? new ApiModels.EmpleadoModel
                    {
                        IdEmpleado = response.Usuario.Empleado.IdEmpleado,
                        Nombre = response.Usuario.Empleado.Nombre,
                        Email = response.Usuario.Empleado.Email,
                        Telefono = response.Usuario.Empleado.Telefono
                    } : null
                };

                return Ok(new ApiModels.UsuarioResponse
                {
                    Success = response.Success,
                    Message = response.Message,
                    Usuario = usuarioModel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario por nombre: {NombreUsuario}", nombreUsuario);
                return StatusCode(500, new ApiModels.UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiModels.UsuarioResponse>> CrearUsuario([FromBody] ApiModels.CrearUsuarioRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiModels.UsuarioResponse
                {
                    Success = false,
                    Message = "Datos de entrada inválidos"
                });
            }

            try
            {
                using var channel = CreateGrpcChannel();
                var client = new UsuariosService.UsuariosServiceClient(channel);

                var grpcRequest = new InsertarUsuarioRequest
                {
                    NombreUsuario = request.NombreUsuario,
                    Contrasena = request.Contraseña,
                    Rol = request.Rol ?? "Usuario",
                    IdEmpleado = request.IdEmpleado ?? 0
                };

                var response = await client.InsertarUsuarioAsync(grpcRequest);

                if (!response.Success)
                {
                    return BadRequest(new ApiModels.UsuarioResponse
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                var usuarioModel = new ApiModels.UsuarioModel
                {
                    IdUsuario = response.Usuario.IdUsuario,
                    NombreUsuario = response.Usuario.NombreUsuario,
                    Rol = response.Usuario.Rol,
                    IdEmpleado = response.Usuario.IdEmpleado,
                    Empleado = response.Usuario.Empleado != null ? new ApiModels.EmpleadoModel
                    {
                        IdEmpleado = response.Usuario.Empleado.IdEmpleado,
                        Nombre = response.Usuario.Empleado.Nombre,
                        Email = response.Usuario.Empleado.Email,
                        Telefono = response.Usuario.Empleado.Telefono
                    } : null
                };

                return CreatedAtAction(nameof(ObtenerUsuarioPorId), new { id = response.Usuario.IdUsuario }, new ApiModels.UsuarioResponse
                {
                    Success = response.Success,
                    Message = response.Message,
                    Usuario = usuarioModel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario: {NombreUsuario}", request.NombreUsuario);
                return StatusCode(500, new ApiModels.UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Actualiza un usuario existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiModels.UsuarioResponse>> ActualizarUsuario(int id, [FromBody] ApiModels.ActualizarUsuarioRequest request)
        {
            if (id != request.IdUsuario)
            {
                return BadRequest(new ApiModels.UsuarioResponse
                {
                    Success = false,
                    Message = "El ID del usuario no coincide"
                });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiModels.UsuarioResponse
                {
                    Success = false,
                    Message = "Datos de entrada inválidos"
                });
            }

            try
            {
                using var channel = CreateGrpcChannel();
                var client = new UsuariosService.UsuariosServiceClient(channel);

                var grpcRequest = new GrpcModels.ActualizarUsuarioRequest
                {
                    IdUsuario = request.IdUsuario,
                    NombreUsuario = request.NombreUsuario,
                    Contrasena = request.Contraseña ?? "",
                    Rol = request.Rol ?? "Usuario",
                    IdEmpleado = request.IdEmpleado ?? 0
                };

                var response = await client.ActualizarUsuarioAsync(grpcRequest);

                if (!response.Success)
                {
                    return BadRequest(new ApiModels.UsuarioResponse
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                var usuarioModel = new ApiModels.UsuarioModel
                {
                    IdUsuario = response.Usuario.IdUsuario,
                    NombreUsuario = response.Usuario.NombreUsuario,
                    Rol = response.Usuario.Rol,
                    IdEmpleado = response.Usuario.IdEmpleado,
                    Empleado = response.Usuario.Empleado != null ? new ApiModels.EmpleadoModel
                    {
                        IdEmpleado = response.Usuario.Empleado.IdEmpleado,
                        Nombre = response.Usuario.Empleado.Nombre,
                        Email = response.Usuario.Empleado.Email,
                        Telefono = response.Usuario.Empleado.Telefono
                    } : null
                };

                return Ok(new ApiModels.UsuarioResponse
                {
                    Success = response.Success,
                    Message = response.Message,
                    Usuario = usuarioModel
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario: {Id}", id);
                return StatusCode(500, new ApiModels.UsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }

        /// <summary>
        /// Elimina un usuario
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiModels.EliminarUsuarioResponse>> EliminarUsuario(int id)
        {
            try
            {
                using var channel = CreateGrpcChannel();
                var client = new UsuariosService.UsuariosServiceClient(channel);

                var response = await client.EliminarUsuarioAsync(new EliminarUsuarioRequest { IdUsuario = id });

                if (!response.Success)
                {
                    return NotFound(new ApiModels.EliminarUsuarioResponse
                    {
                        Success = false,
                        Message = response.Message
                    });
                }

                return Ok(new ApiModels.EliminarUsuarioResponse
                {
                    Success = response.Success,
                    Message = response.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario: {Id}", id);
                return StatusCode(500, new ApiModels.EliminarUsuarioResponse
                {
                    Success = false,
                    Message = "Error interno del servidor"
                });
            }
        }
    }
}