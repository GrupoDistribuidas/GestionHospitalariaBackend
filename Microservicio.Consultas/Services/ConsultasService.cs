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
            // Si el caller es Admin, agregamos las consultas de todos los centros
            var isAdmin = false;
            try
            {
                var http = context.GetHttpContext();
                if (http != null)
                {
                    var user = http.User;
                    isAdmin = user.IsInRole("Admin") || user.Claims.Any(c => c.Type == "rol_usuario" && c.Value == "Admin");
                }
            }
            catch { /* ignore */ }

            var response = new ConsultasListResponse();
            if (isAdmin)
            {
                var centers = new[] { 1, 2, 3 };
                foreach (var c in centers)
                {
                    using var db = _dbFactory.CreateForCentro(c);
                    var list = await db.ConsultasMedicas.ToListAsync();
                    response.Consultas.AddRange(list.Select(MapToResponse));
                }
                return response;
            }

            // comportamiento normal por centro
            var centroHeaderAll = context.RequestHeaders.Get("x-centro-medico")?.Value;
            int centroIdAll = 1;
            if (!string.IsNullOrEmpty(centroHeaderAll) && int.TryParse(centroHeaderAll, out var pAll)) centroIdAll = pAll;

            var loggerAll = context.GetHttpContext()?.RequestServices.GetService<ILogger<ConsultasServiceImpl>>();
            loggerAll?.LogInformation("ConsultasService: ObtenerTodasConsultas - centro resuelto={Centro}", centroIdAll);

            using var dbAll = _dbFactory.CreateForCentro(centroIdAll);
            var consultas = await dbAll.ConsultasMedicas.ToListAsync();
            response.Consultas.AddRange(consultas.Select(MapToResponse));
            return response;
        }

        public override async Task<ConsultaResponse> InsertarConsulta(InsertarConsultaRequest request, ServerCallContext context)
        {
            // Determinar si el caller es Admin
            var isAdminCaller = false;
            try
            {
                var http = context.GetHttpContext();
                if (http != null)
                {
                    var user = http.User;
                    isAdminCaller = user.IsInRole("Admin") || user.Claims.Any(c => c.Type == "rol_usuario" && c.Value == "Admin");
                }
            }
            catch { }

            // 1) Buscar paciente (si es Admin, buscar en todas las clinicas hasta encontrarlo)
            int pacienteCentro = 1; // fallback
            try
            {
                if (isAdminCaller)
                {
                    var found = false;
                    var centers = new[] { 1, 2, 3 };
                    foreach (var c in centers)
                    {
                        var hdr = new Metadata { { "x-centro-medico", c.ToString() } };
                        try
                        {
                            var p = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = request.IdPaciente }, new CallOptions(hdr));
                            pacienteCentro = c;
                            found = true;
                            break;
                        }
                        catch (RpcException) { /* no encontrado en este centro */ }
                    }
                    if (!found) throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
                }
                else
                {
                    var centroHeader = context.RequestHeaders.Get("x-centro-medico")?.Value;
                    Metadata? headers = null;
                    if (!string.IsNullOrEmpty(centroHeader)) headers = new Metadata { { "x-centro-medico", centroHeader } };
                    var paciente = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = request.IdPaciente }, headers != null ? new CallOptions(headers) : default);
                    if (int.TryParse(context.RequestHeaders.Get("x-centro-medico")?.Value, out var parsed)) pacienteCentro = parsed;
                }
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            // 2) Validar medico (si es Admin, buscar globalmente)
            try
            {
                if (isAdminCaller)
                {
                    // Medicos están en la DB de administracion y se pueden obtener sin header
                    var medicoInfo = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = request.IdMedico });
                }
                else
                {
                    var centroHeader2 = context.RequestHeaders.Get("x-centro-medico")?.Value;
                    Metadata? headers2 = null;
                    if (!string.IsNullOrEmpty(centroHeader2)) headers2 = new Metadata { { "x-centro-medico", centroHeader2 } };
                    var medico = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = request.IdMedico }, headers2 != null ? new CallOptions(headers2) : default);
                }
            }
            catch (RpcException)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Medico no encontrado"));
            }

            // 3) Guardar la consulta en la DB correspondiente al paciente
            var centroIdIns = pacienteCentro;
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
                // Detectar si caller es Admin
                var isAdmin = false;
                try
                {
                    var http = context.GetHttpContext();
                    if (http != null)
                    {
                        var user = http.User;
                        isAdmin = user.IsInRole("Admin") || user.Claims.Any(c => c.Type == "rol_usuario" && c.Value == "Admin");
                    }
                    // Además permitir que ApiGateway propague el rol por metadata gRPC
                    var rolHeader = context.RequestHeaders.Get("x-rol-usuario")?.Value;
                    if (!string.IsNullOrEmpty(rolHeader) && rolHeader == "Admin") isAdmin = true;
                }
                catch { }

                // Lista combinada de tuplas (consulta, centroId)
                var combined = new List<(ConsultaMedica consulta, int centro)>();

                // Filtro helper
                Func<IQueryable<ConsultaMedica>, IQueryable<ConsultaMedica>> applyFilters = q =>
                {
                    var qq = q;
                    if (request.IdMedico > 0)
                    {
                        qq = qq.Where(c => c.IdMedico == request.IdMedico);
                    }
                    if (!string.IsNullOrEmpty(request.FechaInicio))
                    {
                        var fechaInicio = DateTime.Parse(request.FechaInicio);
                        qq = qq.Where(c => c.Fecha >= fechaInicio);
                    }
                    if (!string.IsNullOrEmpty(request.FechaFin))
                    {
                        var fechaFin = DateTime.Parse(request.FechaFin);
                        qq = qq.Where(c => c.Fecha <= fechaFin);
                    }
                    if (!string.IsNullOrEmpty(request.Motivo))
                    {
                        qq = qq.Where(c => c.Motivo != null && c.Motivo.Contains(request.Motivo));
                    }
                    if (!string.IsNullOrEmpty(request.Diagnostico))
                    {
                        qq = qq.Where(c => c.Diagnostico != null && c.Diagnostico.Contains(request.Diagnostico));
                    }
                    return qq;
                };

                var loggerRpt = context.GetHttpContext()?.RequestServices.GetService<ILogger<ConsultasServiceImpl>>();
                loggerRpt?.LogInformation("ConsultasService: ObtenerReporteConsultasPorMedico - isAdmin={IsAdmin}", isAdmin);

                if (isAdmin)
                {
                    var centers = new[] { 1, 2, 3 };
                    foreach (var c in centers)
                    {
                        try
                        {
                            using var db = _dbFactory.CreateForCentro(c);
                            var q = applyFilters(db.ConsultasMedicas.AsQueryable());
                            var list = await q.ToListAsync();
                            loggerRpt?.LogInformation("ConsultasService: ObtenerReporteConsultasPorMedico - centro={Centro} obtuvo={Count}", c, list.Count);
                            foreach (var it in list) combined.Add((it, c));
                        }
                        catch
                        {
                            loggerRpt?.LogWarning("ConsultasService: ObtenerReporteConsultasPorMedico - centro={Centro} no disponible o error al consultar", c);
                            // si una extensión no está disponible, continuamos con las otras
                            continue;
                        }
                    }
                }
                else
                {
                    // comportamiento normal: tomar el centro del header o 1 por defecto
                    var centroHeader = context.RequestHeaders.Get("x-centro-medico")?.Value;
                    int centroId = 1;
                    if (!string.IsNullOrEmpty(centroHeader) && int.TryParse(centroHeader, out var parsed)) centroId = parsed;
                    using var db = _dbFactory.CreateForCentro(centroId);
                    var q = applyFilters(db.ConsultasMedicas.AsQueryable());
                    var list = await q.ToListAsync();
                    foreach (var it in list) combined.Add((it, centroId));
                }

                // Obtener médicos únicos
                var medicosIds = combined.Where(x => x.consulta.IdMedico.HasValue && x.consulta.IdMedico.Value > 0).Select(x => x.consulta.IdMedico!.Value).Distinct().ToList();

                var reporteResponse = new ReporteConsultasPorMedicoResponse
                {
                    TotalConsultasGeneral = combined.Count,
                    FechaGeneracion = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                foreach (var medicoId in medicosIds)
                {
                    try
                    {
                        // Obtener información del médico: si es Admin, solicitar sin header (global)
                        MedicoResponse medicoInfo;
                        if (isAdmin)
                        {
                            medicoInfo = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = medicoId });
                        }
                        else
                        {
                            var centroHeaderRpt = context.RequestHeaders.Get("x-centro-medico")?.Value;
                            Metadata? headersRpt = null;
                            if (!string.IsNullOrEmpty(centroHeaderRpt)) headersRpt = new Metadata { { "x-centro-medico", centroHeaderRpt } };
                            medicoInfo = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = medicoId }, headersRpt != null ? new CallOptions(headersRpt) : default);
                        }

                        var consultasDelMedico = combined.Where(c => c.consulta.IdMedico == medicoId).ToList();

                        var medicoReporte = new MedicoReporteResponse
                        {
                            IdMedico = medicoId,
                            NombreMedico = medicoInfo.Nombre,
                            IdEspecialidad = medicoInfo.IdEspecialidad,
                            NombreEspecialidad = $"Especialidad ID: {medicoInfo.IdEspecialidad}",
                            TotalConsultas = consultasDelMedico.Count,
                            TotalRegistradas = $"{consultasDelMedico.Count} consultas"
                        };

                        // Agregar consultas del médico con información del paciente (usando el centro correspondiente por cada consulta)
                        foreach (var (consulta, centro) in consultasDelMedico)
                        {
                            try
                            {
                                Metadata? pacienteHeaders = null;
                                if (isAdmin)
                                {
                                    pacienteHeaders = new Metadata { { "x-centro-medico", centro.ToString() } };
                                }
                                else
                                {
                                    var centroHeaderRpt = context.RequestHeaders.Get("x-centro-medico")?.Value;
                                    if (!string.IsNullOrEmpty(centroHeaderRpt)) pacienteHeaders = new Metadata { { "x-centro-medico", centroHeaderRpt } };
                                }

                                var pacienteInfo = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = consulta.IdPaciente }, pacienteHeaders != null ? new CallOptions(pacienteHeaders) : default);

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

                loggerRpt?.LogInformation("ConsultasService: ObtenerReporteConsultasPorMedico - total combinadas={Total}", combined.Count);
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
