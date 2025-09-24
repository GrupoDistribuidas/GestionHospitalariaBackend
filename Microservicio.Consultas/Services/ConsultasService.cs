using Grpc.Core;
using Microservicio.Consultas.Protos;
using Microservicio.Consultas.Data;
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
