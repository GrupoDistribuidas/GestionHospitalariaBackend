using Grpc.Core;
using Microservicio.Consultas.Protos;
using Microservicio.Consultas.Data;
using Microservicio.Consultas.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Consultas.Services
{
    public class ConsultasService : Protos.ConsultasService.ConsultasServiceBase
    {
        private readonly ConsultasDbContext _dbContext;

        public ConsultasService(ConsultasDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task<ConsultaResponse> ObtenerConsultaPorId(ConsultaPorIdRequest request, ServerCallContext context)
        {
            var consulta = await _dbContext.ConsultasMedicas
                .FirstOrDefaultAsync(c => c.IdConsultaMedica == request.IdConsultaMedica);

            if (consulta == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Consulta no encontrada"));
            }

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
