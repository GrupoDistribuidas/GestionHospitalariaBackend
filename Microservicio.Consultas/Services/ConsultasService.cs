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
