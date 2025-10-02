using Microsoft.AspNetCore.Mvc;
using Microservicio.Consultas.Protos;
using Grpc.Core;
using ApiGateway.Models;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todos los endpoints
    public class ReportesController : ControllerBase
    {
        private readonly ConsultasService.ConsultasServiceClient _consultasClient;
        private readonly Microservicio.Administracion.Protos.EspecialidadesService.EspecialidadesServiceClient _especialidadesClient;

        public ReportesController(
            ConsultasService.ConsultasServiceClient consultasClient,
            Microservicio.Administracion.Protos.EspecialidadesService.EspecialidadesServiceClient especialidadesClient)
        {
            _consultasClient = consultasClient;
            _especialidadesClient = especialidadesClient;
        }

        /// <summary>
        /// Obtiene un reporte de consultas médicas por médico con filtros opcionales
        /// </summary>
        /// <param name="request">Filtros para el reporte en formato JSON</param>
        /// <returns>Reporte de consultas médicas por médico</returns>
        [HttpPost("consultas-por-medico")]
        public async Task<ActionResult<ReporteConsultasResponse>> GetReporteConsultasPorMedico([FromBody] ReporteConsultasRequest request)
        {
            try
            {
                // Validar modelo
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validar formato de fechas si se proporcionan
                if (!string.IsNullOrEmpty(request.FechaInicio))
                {
                    if (!DateTime.TryParseExact(request.FechaInicio, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                    {
                        return BadRequest(new { error = "El formato de fecha de inicio debe ser yyyy-MM-dd" });
                    }
                }

                if (!string.IsNullOrEmpty(request.FechaFin))
                {
                    if (!DateTime.TryParseExact(request.FechaFin, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                    {
                        return BadRequest(new { error = "El formato de fecha de fin debe ser yyyy-MM-dd" });
                    }
                }

                // Validar que fecha inicio no sea mayor que fecha fin
                if (!string.IsNullOrEmpty(request.FechaInicio) && !string.IsNullOrEmpty(request.FechaFin))
                {
                    var inicio = DateTime.Parse(request.FechaInicio);
                    var fin = DateTime.Parse(request.FechaFin);
                    if (inicio > fin)
                    {
                        return BadRequest(new { error = "La fecha de inicio no puede ser mayor que la fecha de fin" });
                    }
                }

                var grpcRequest = new ReporteConsultasPorMedicoRequest
                {
                    IdMedico = request.IdMedico ?? 0,
                    FechaInicio = request.FechaInicio ?? string.Empty,
                    FechaFin = request.FechaFin ?? string.Empty,
                    Motivo = request.Motivo ?? string.Empty,
                    Diagnostico = request.Diagnostico ?? string.Empty
                };

                var response = await _consultasClient.ObtenerReporteConsultasPorMedicoAsync(grpcRequest);

                // Contar filtros activos
                int filtrosActivos = 0;
                if (request.IdMedico.HasValue && request.IdMedico > 0) filtrosActivos++;
                if (!string.IsNullOrEmpty(request.FechaInicio)) filtrosActivos++;
                if (!string.IsNullOrEmpty(request.FechaFin)) filtrosActivos++;
                if (!string.IsNullOrEmpty(request.Motivo)) filtrosActivos++;
                if (!string.IsNullOrEmpty(request.Diagnostico)) filtrosActivos++;

                // Crear respuesta estructurada según la interfaz del frontend
                var resultado = new ReporteConsultasResponse
                {
                    Resumen = new ResumenConsultas
                    {
                        TotalConsultas = response.TotalConsultasGeneral,
                        MedicosActivos = response.Medicos.Count,
                        FiltrosActivos = filtrosActivos,
                        FechaGeneracion = response.FechaGeneracion
                    },
                    FiltrosAplicados = new FiltrosAplicados
                    {
                        MedicoId = request.IdMedico,
                        NombreMedico = request.IdMedico.HasValue ? 
                            response.Medicos.FirstOrDefault(m => m.IdMedico == request.IdMedico)?.NombreMedico : null,
                        FechaInicio = request.FechaInicio,
                        FechaFin = request.FechaFin,
                        Motivo = request.Motivo,
                        Diagnostico = request.Diagnostico
                    },
                    MedicosPorConsultas = response.Medicos.Select(m => new MedicoConsultas
                    {
                        IdMedico = m.IdMedico,
                        NombreMedico = m.NombreMedico,
                        Especialidad = m.NombreEspecialidad,
                        TotalConsultas = m.TotalConsultas,
                        TotalRegistradas = m.TotalRegistradas,
                        Expandido = false, // Por defecto no expandido
                        Consultas = m.Consultas.Select(c => new ConsultaDetalle
                        {
                            IdConsulta = c.IdConsultaMedica,
                            Fecha = c.Fecha,
                            Hora = c.Hora,
                            NombrePaciente = c.NombrePaciente,
                            Motivo = c.Motivo,
                            Diagnostico = c.Diagnostico,
                            Tratamiento = c.Tratamiento,
                            IdPaciente = c.IdPaciente
                        }).ToList()
                    }).ToList()
                };

                return Ok(resultado);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Status.Detail });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene estadísticas resumidas de consultas por médico
        /// </summary>
        /// <param name="request">Filtros para las estadísticas en formato JSON</param>
        /// <returns>Estadísticas resumidas</returns>
        [HttpPost("estadisticas-consultas")]
        public async Task<ActionResult<EstadisticasConsultasResponse>> GetEstadisticasConsultas([FromBody] EstadisticasConsultasRequest request)
        {
            try
            {
                // Validar modelo
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var grpcRequest = new ReporteConsultasPorMedicoRequest
                {
                    FechaInicio = request.FechaInicio ?? string.Empty,
                    FechaFin = request.FechaFin ?? string.Empty
                };

                var response = await _consultasClient.ObtenerReporteConsultasPorMedicoAsync(grpcRequest);

                var estadisticas = new EstadisticasConsultasResponse
                {
                    TotalConsultas = response.TotalConsultasGeneral,
                    TotalMedicos = response.Medicos.Count,
                    FechaGeneracion = response.FechaGeneracion,
                    PromedioConsultasPorMedico = response.Medicos.Count > 0 ? 
                        Math.Round((double)response.TotalConsultasGeneral / response.Medicos.Count, 2) : 0,
                    MedicoConMasConsultas = response.Medicos.OrderByDescending(m => m.TotalConsultas).FirstOrDefault()?.NombreMedico,
                    MaxConsultasPorMedico = response.Medicos.Any() ? response.Medicos.Max(m => m.TotalConsultas) : 0,
                    Especialidades = response.Medicos
                        .GroupBy(m => new { m.IdEspecialidad, m.NombreEspecialidad })
                        .Select(g => new EspecialidadEstadistica
                        {
                            IdEspecialidad = g.Key.IdEspecialidad,
                            NombreEspecialidad = g.Key.NombreEspecialidad,
                            TotalMedicos = g.Count(),
                            TotalConsultas = g.Sum(m => m.TotalConsultas)
                        })
                        .OrderByDescending(e => e.TotalConsultas)
                        .ToList()
                };

                return Ok(estadisticas);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Status.Detail });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", detalle = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene la lista de médicos disponibles para usar en los filtros
        /// </summary>
        /// <returns>Lista de médicos con su información básica</returns>
        [HttpGet("medicos-disponibles")]
        public async Task<ActionResult<List<MedicoFiltro>>> GetMedicosDisponibles()
        {
            try
            {
                // Obtener todos los médicos desde el microservicio de administración
                var medicosClient = HttpContext.RequestServices.GetRequiredService<Microservicio.Administracion.Protos.MedicosService.MedicosServiceClient>();
                var medicosResponse = await medicosClient.ObtenerTodosMedicosAsync(new Google.Protobuf.WellKnownTypes.Empty());

                // Obtener todas las especialidades para resolver nombres
                var especialidadesResponse = await _especialidadesClient.ObtenerTodasEspecialidadesAsync(new Google.Protobuf.WellKnownTypes.Empty());
                var especialidadesDict = especialidadesResponse.Especialidades.ToDictionary(e => e.IdEspecialidad, e => e.Nombre);

                var medicosDisponibles = medicosResponse.Medicos.Select(m => new MedicoFiltro
                {
                    IdMedico = m.IdEmpleado,
                    NombreMedico = m.Nombre,
                    Especialidad = especialidadesDict.ContainsKey(m.IdEspecialidad) 
                        ? especialidadesDict[m.IdEspecialidad] 
                        : $"Especialidad ID: {m.IdEspecialidad}"
                }).ToList();

                return Ok(medicosDisponibles);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, new { error = ex.Status.Detail });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error interno del servidor", detalle = ex.Message });
            }
        }
    }
}