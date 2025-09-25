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
        private readonly ConsultasDbContext _dbContext;
        private readonly PacientesService.PacientesServiceClient _pacientesClient;
        private readonly MedicosService.MedicosServiceClient _medicosClient;

        public ConsultasServiceImpl(
            ConsultasDbContext dbContext,
            PacientesService.PacientesServiceClient pacientesClient,
            MedicosService.MedicosServiceClient medicosClient)
        {
            _dbContext = dbContext;
            _pacientesClient = pacientesClient;
            _medicosClient = medicosClient;
        }

        public override async Task<ConsultaResponse> ObtenerConsultaPorId(ConsultaPorIdRequest request, ServerCallContext context)
        {
            var consulta = await _dbContext.ConsultasMedicas
                .FirstOrDefaultAsync(c => c.IdConsultaMedica == request.IdConsultaMedica);

            if (consulta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));

            return MapToResponse(consulta);
        }

        public override async Task<ConsultasListResponse> ObtenerTodasConsultas(Empty request, ServerCallContext context)
        {
            var consultas = await _dbContext.ConsultasMedicas.ToListAsync();
            var response = new ConsultasListResponse();
            response.Consultas.AddRange(consultas.Select(MapToResponse));
            return response;
        }

        public override async Task<ConsultaResponse> InsertarConsulta(InsertarConsultaRequest request, ServerCallContext context)
        {
            // Validaciones con microservicio de pacientes y medicos
            try
            {
                var paciente = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = request.IdPaciente });
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            try
            {
                var medico = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = request.IdMedico });
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Medico no encontrado"));
            }

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

            _dbContext.ConsultasMedicas.Add(consulta);
            await _dbContext.SaveChangesAsync();
            return MapToResponse(consulta);
        }

        public override async Task<ConsultaResponse> ActualizarConsulta(ActualizarConsultaRequest request, ServerCallContext context)
        {
            var consulta = await _dbContext.ConsultasMedicas.FindAsync(request.IdConsultaMedica);
            if (consulta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));

            // Validaciones externas
            try
            {
                var paciente = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = request.IdPaciente });
            }
            catch
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));
            }

            try
            {
                var medico = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = request.IdMedico });
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

            await _dbContext.SaveChangesAsync();
            return MapToResponse(consulta);
        }

        public override async Task<EliminarConsultaResponse> EliminarConsulta(EliminarConsultaRequest request, ServerCallContext context)
        {
            var consulta = await _dbContext.ConsultasMedicas.FindAsync(request.IdConsultaMedica);
            if (consulta == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));

            _dbContext.ConsultasMedicas.Remove(consulta);
            await _dbContext.SaveChangesAsync();
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
                        var medicoInfo = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = medicoId });

                        var consultasDelMedico = consultas.Where(c => c.IdMedico == medicoId).ToList();

                        var medicoReporte = new MedicoReporteResponse
                        {
                            IdMedico = medicoId,
                            NombreMedico = medicoInfo.Nombre,
                            IdEspecialidad = medicoInfo.IdEspecialidad,
                            NombreEspecialidad = $"Especialidad ID: {medicoInfo.IdEspecialidad}", // Temporal hasta obtener nombre real
                            TotalConsultas = consultasDelMedico.Count
                        };

                        // Agregar consultas del médico con información del paciente
                        foreach (var consulta in consultasDelMedico)
                        {
                            try
                            {
                                // Obtener información del paciente
                                var pacienteInfo = await _pacientesClient.ObtenerPacientePorIdAsync(new PacientePorIdRequest { IdPaciente = consulta.IdPaciente });

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
