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

        public ReportesController(ConsultasService.ConsultasServiceClient consultasClient)
        {
            _consultasClient = consultasClient;
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

                // Crear respuesta estructurada
                var resultado = new ReporteConsultasResponse
                {
                    Resumen = new ResumenReporte
                    {
                        TotalConsultasGeneral = response.TotalConsultasGeneral,
                        TotalMedicos = response.Medicos.Count,
                        FechaGeneracion = response.FechaGeneracion,
                        Filtros = new FiltrosReporte
                        {
                            MedicoId = request.IdMedico,
                            FechaInicio = request.FechaInicio,
                            FechaFin = request.FechaFin,
                            Motivo = request.Motivo,
                            Diagnostico = request.Diagnostico
                        }
                    },
                    Medicos = response.Medicos.Select(m => new MedicoReporte
                    {
                        IdMedico = m.IdMedico,
                        NombreMedico = m.NombreMedico,
                        IdEspecialidad = m.IdEspecialidad,
                        NombreEspecialidad = m.NombreEspecialidad,
                        TotalConsultas = m.TotalConsultas,
                        Consultas = m.Consultas.Select(c => new ConsultaReporte
                        {
                            IdConsulta = c.IdConsultaMedica,
                            Fecha = c.Fecha,
                            Hora = c.Hora,
                            Motivo = c.Motivo,
                            Diagnostico = c.Diagnostico,
                            Tratamiento = c.Tratamiento,
                            Paciente = new PacienteReporte
                            {
                                IdPaciente = c.IdPaciente,
                                NombrePaciente = c.NombrePaciente
                            }
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
    }
}