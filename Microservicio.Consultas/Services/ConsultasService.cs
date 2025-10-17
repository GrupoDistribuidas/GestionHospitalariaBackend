using Grpc.Core;
using Microservicio.Consultas.Protos;
using Google.Protobuf.WellKnownTypes;
using Microservicio.Consultas.Data;
using Microservicio.Consultas.Models;
using Microservicio.Administracion.Protos;
using Microservicio.ClinicaExtension.Protos;
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Consultas.Services
{
    // Cambiamos el nombre para evitar conflictos con el tipo generado por gRPC
    public class ConsultasServiceImpl : ConsultasService.ConsultasServiceBase
    {
        private readonly ConsultasDbContext _dbContext; // mantenemos para compatibilidad en scope, pero no lo usaremos directamente
        private readonly PacientesService.PacientesServiceClient _pacientesClient;
        private readonly MedicosService.MedicosServiceClient _medicosClient;
        private readonly IConsultasDbContextFactory _dbFactory;

        public ConsultasServiceImpl(
            ConsultasDbContext dbContext,
            IConsultasDbContextFactory dbFactory,
            PacientesService.PacientesServiceClient pacientesClient,
            MedicosService.MedicosServiceClient medicosClient)
        {
            _dbContext = dbContext;
            _dbFactory = dbFactory;
            _pacientesClient = pacientesClient;
            _medicosClient = medicosClient;
        }

        public override async Task<ConsultaResponse> ObtenerConsultaPorId(ConsultaPorIdRequest request, ServerCallContext context)
        {
            var centroHeader = context.RequestHeaders.Get("x-centro-medico")?.Value;
            int centroId = 1;
            if (!string.IsNullOrEmpty(centroHeader) && int.TryParse(centroHeader, out var parsedCentro)) centroId = parsedCentro;

            // Log para trazabilidad
            var logger = context.GetHttpContext()?.RequestServices.GetService<ILogger<ConsultasServiceImpl>>();
            logger?.LogInformation("ConsultasService: ObtenerConsultaPorId - centro resuelto={Centro}", centroId);

            using var db = _dbFactory.CreateForCentro(centroId);
            var consulta = await db.ConsultasMedicas.FirstOrDefaultAsync(c => c.IdConsultaMedica == request.IdConsultaMedica);

            if (consulta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));

            return MapToResponse(consulta);
        }

        public override async Task<ConsultasListResponse> ObtenerTodasConsultas(Empty request, ServerCallContext context)
        {
            var centroHeaderAll = context.RequestHeaders.Get("x-centro-medico")?.Value;
            int centroIdAll = 1;
            if (!string.IsNullOrEmpty(centroHeaderAll) && int.TryParse(centroHeaderAll, out var pAll)) centroIdAll = pAll;

            var loggerAll = context.GetHttpContext()?.RequestServices.GetService<ILogger<ConsultasServiceImpl>>();
            loggerAll?.LogInformation("ConsultasService: ObtenerTodasConsultas - centro resuelto={Centro}", centroIdAll);

            using var dbAll = _dbFactory.CreateForCentro(centroIdAll);
            var consultas = await dbAll.ConsultasMedicas.ToListAsync();
            var response = new ConsultasListResponse();
            response.Consultas.AddRange(consultas.Select(MapToResponse));
            return response;
        }

        public override async Task<ConsultaResponse> InsertarConsulta(InsertarConsultaRequest request, ServerCallContext context)
        {
            // Validaciones con microservicio de pacientes y medicos
            try
            {
                // Propagar header x-centro-medico si existe
                var centroHeader = context.RequestHeaders.Get("x-centro-medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroHeader)) headers = new Metadata { { "x-centro-medico", centroHeader } };

                var loggerIns = context.GetHttpContext()?.RequestServices.GetService<ILogger<ConsultasServiceImpl>>();
                loggerIns?.LogInformation("ConsultasService: InsertarConsulta - validando paciente en centro={Centro} id_paciente={IdPaciente}", centroHeader ?? "(null)", request.IdPaciente);

                var paciente = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = request.IdPaciente }, headers != null ? new CallOptions(headers) : default);
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            try
            {
                var centroHeader2 = context.RequestHeaders.Get("x-centro-medico")?.Value;
                Metadata? headers2 = null;
                if (!string.IsNullOrEmpty(centroHeader2)) headers2 = new Metadata { { "x-centro-medico", centroHeader2 } };

                var medico = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = request.IdMedico }, headers2 != null ? new CallOptions(headers2) : default);
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Medico no encontrado"));
            }

            var centroHeaderIns = context.RequestHeaders.Get("x-centro-medico")?.Value;
            int centroIdIns = 1;
            if (!string.IsNullOrEmpty(centroHeaderIns) && int.TryParse(centroHeaderIns, out var pIns)) centroIdIns = pIns;

            var loggerIns2 = context.GetHttpContext()?.RequestServices.GetService<ILogger<ConsultasServiceImpl>>();
            loggerIns2?.LogInformation("ConsultasService: InsertarConsulta - guardando consulta en centro={Centro}", centroIdIns);

            var consulta = new ConsultaMedica
            {
                Fecha = DateTime.Parse(request.Fecha),
                Hora = TimeSpan.Parse(request.Hora),
                Motivo = request.Motivo,
                Diagnostico = request.Diagnostico,
                Tratamiento = request.Tratamiento,
                IdPaciente = request.IdPaciente,
                IdMedico = request.IdMedico
            };
            using var dbIns = _dbFactory.CreateForCentro(centroIdIns);
            dbIns.ConsultasMedicas.Add(consulta);
            await dbIns.SaveChangesAsync();
            return MapToResponse(consulta);
        }

        public override async Task<ConsultaResponse> ActualizarConsulta(ActualizarConsultaRequest request, ServerCallContext context)
        {
            var centroHeaderUpd = context.RequestHeaders.Get("x-centro-medico")?.Value;
            int centroIdUpd = 1;
            if (!string.IsNullOrEmpty(centroHeaderUpd) && int.TryParse(centroHeaderUpd, out var pUpd)) centroIdUpd = pUpd;

            using var dbUpd = _dbFactory.CreateForCentro(centroIdUpd);
            var consulta = await dbUpd.ConsultasMedicas.FindAsync(request.IdConsultaMedica);
            if (consulta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));

            // Validaciones externas
            try
            {
                var centroHeaderUp = context.RequestHeaders.Get("x-centro-medico")?.Value;
                Metadata? headersUp = null;
                if (!string.IsNullOrEmpty(centroHeaderUp)) headersUp = new Metadata { { "x-centro-medico", centroHeaderUp } };

                var paciente = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = request.IdPaciente }, headersUp != null ? new CallOptions(headersUp) : default);
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            try
            {
                var centroHeaderUp2 = context.RequestHeaders.Get("x-centro-medico")?.Value;
                Metadata? headersUp2 = null;
                if (!string.IsNullOrEmpty(centroHeaderUp2)) headersUp2 = new Metadata { { "x-centro-medico", centroHeaderUp2 } };

                var medico = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = request.IdMedico }, headersUp2 != null ? new CallOptions(headersUp2) : default);
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Medico no encontrado"));
            }

            consulta.Fecha = DateTime.Parse(request.Fecha);
            consulta.Hora = TimeSpan.Parse(request.Hora);
            consulta.Motivo = request.Motivo;
            consulta.Diagnostico = request.Diagnostico;
            consulta.Tratamiento = request.Tratamiento;
            consulta.IdPaciente = request.IdPaciente;
            consulta.IdMedico = request.IdMedico;

            await dbUpd.SaveChangesAsync();
            return MapToResponse(consulta);
        }

        public override async Task<EliminarConsultaResponse> EliminarConsulta(EliminarConsultaRequest request, ServerCallContext context)
        {
            var centroHeaderDel = context.RequestHeaders.Get("x-centro-medico")?.Value;
            int centroIdDel = 1;
            if (!string.IsNullOrEmpty(centroHeaderDel) && int.TryParse(centroHeaderDel, out var pDel)) centroIdDel = pDel;

            using var dbDel = _dbFactory.CreateForCentro(centroIdDel);
            var consulta = await dbDel.ConsultasMedicas.FindAsync(request.IdConsultaMedica);
            if (consulta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));
            dbDel.ConsultasMedicas.Remove(consulta);
            await dbDel.SaveChangesAsync();
            return new EliminarConsultaResponse { Exito = true };
        }

        public override async Task<ReporteConsultasPorMedicoResponse> ObtenerReporteConsultasPorMedico(ReporteConsultasPorMedicoRequest request, ServerCallContext context)
        {
            try
            {
                // Base query para consultas
                var consultasQuery = _dbContext.ConsultasMedicas.AsQueryable();

                // Aplicar filtros
                if (request.IdMedico > 0)
                {
                    consultasQuery = consultasQuery.Where(c => c.IdMedico == request.IdMedico);
                }

                if (!string.IsNullOrEmpty(request.FechaInicio))
                {
                    var fechaInicio = DateTime.Parse(request.FechaInicio);
                    consultasQuery = consultasQuery.Where(c => c.Fecha >= fechaInicio);
                }

                if (!string.IsNullOrEmpty(request.FechaFin))
                {
                    var fechaFin = DateTime.Parse(request.FechaFin);
                    consultasQuery = consultasQuery.Where(c => c.Fecha <= fechaFin);
                }

                if (!string.IsNullOrEmpty(request.Motivo))
                {
                    consultasQuery = consultasQuery.Where(c => c.Motivo != null && c.Motivo.Contains(request.Motivo));
                }

                if (!string.IsNullOrEmpty(request.Diagnostico))
                {
                    consultasQuery = consultasQuery.Where(c => c.Diagnostico != null && c.Diagnostico.Contains(request.Diagnostico));
                }

                var consultas = await consultasQuery.ToListAsync();

                // Obtener médicos únicos de las consultas
                var medicosIds = consultas.Where(c => c.IdMedico.HasValue && c.IdMedico.Value > 0).Select(c => c.IdMedico!.Value).Distinct().ToList();

                var reporteResponse = new ReporteConsultasPorMedicoResponse
                {
                    TotalConsultasGeneral = consultas.Count,
                    FechaGeneracion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                foreach (var medicoId in medicosIds)
                {
                    try
                    {
                        // Obtener información del médico desde el microservicio de administración
                        var centroHeaderRpt = context.RequestHeaders.Get("x-centro-medico")?.Value;
                        Metadata? headersRpt = null;
                        if (!string.IsNullOrEmpty(centroHeaderRpt)) headersRpt = new Metadata { { "x-centro-medico", centroHeaderRpt } };

                        var medicoInfo = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = medicoId }, headersRpt != null ? new CallOptions(headersRpt) : default);

                        var consultasDelMedico = consultas.Where(c => c.IdMedico == medicoId).ToList();

                        var medicoReporte = new MedicoReporteResponse
                        {
                            IdMedico = medicoId,
                            NombreMedico = medicoInfo.Nombre,
                            IdEspecialidad = medicoInfo.IdEspecialidad,
                            NombreEspecialidad = $"Especialidad ID: {medicoInfo.IdEspecialidad}", // Temporal hasta obtener nombre real
                            TotalConsultas = consultasDelMedico.Count,
                            TotalRegistradas = $"{consultasDelMedico.Count} consultas"
                        };

                        // Agregar consultas del médico con información del paciente
                        foreach (var consulta in consultasDelMedico)
                        {
                            try
                            {
                                // Obtener información del paciente
                                var pacienteInfo = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = consulta.IdPaciente }, headersRpt != null ? new CallOptions(headersRpt) : default);

                                var consultaReporte = new ConsultaReporteResponse
                                {
                                    IdConsultaMedica = consulta.IdConsultaMedica,
                                    Fecha = consulta.Fecha.ToString("yyyy-MM-dd"),
                                    Hora = consulta.Hora.ToString(),
                                    Motivo = consulta.Motivo ?? string.Empty,
                                    Diagnostico = consulta.Diagnostico ?? string.Empty,
                                    Tratamiento = consulta.Tratamiento ?? string.Empty,
                                    IdPaciente = consulta.IdPaciente,
                                    IdMedico = medicoId,
                                    NombrePaciente = pacienteInfo.Nombre
                                };

                                medicoReporte.Consultas.Add(consultaReporte);
                            }
                            catch
                            {
                                // Si no se puede obtener info del paciente, agregar la consulta sin el nombre
                                var consultaReporte = new ConsultaReporteResponse
                                {
                                    IdConsultaMedica = consulta.IdConsultaMedica,
                                    Fecha = consulta.Fecha.ToString("yyyy-MM-dd"),
                                    Hora = consulta.Hora.ToString(),
                                    Motivo = consulta.Motivo ?? string.Empty,
                                    Diagnostico = consulta.Diagnostico ?? string.Empty,
                                    Tratamiento = consulta.Tratamiento ?? string.Empty,
                                    IdPaciente = consulta.IdPaciente,
                                    IdMedico = medicoId,
                                    NombrePaciente = "Paciente no encontrado"
                                };

                                medicoReporte.Consultas.Add(consultaReporte);
                            }
                        }

                        reporteResponse.Medicos.Add(medicoReporte);
                    }
                    catch
                    {
                        // Si no se puede obtener info del médico, continuar con el siguiente
                        continue;
                    }
                }

                return reporteResponse;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.Internal, $"Error al generar reporte: {ex.Message}"));
            }
        }

        private ConsultaResponse MapToResponse(ConsultaMedica consulta)
        {
            return new ConsultaResponse
            {
                IdConsultaMedica = consulta.IdConsultaMedica,
                Fecha = consulta.Fecha.ToString("yyyy-MM-dd"),
                Hora = consulta.Hora.ToString(),
                Motivo = consulta.Motivo ?? string.Empty,
                Diagnostico = consulta.Diagnostico ?? string.Empty,
                Tratamiento = consulta.Tratamiento ?? string.Empty,
                IdPaciente = consulta.IdPaciente,
                IdMedico = consulta.IdMedico ?? 0
            };
        }
    }
}
