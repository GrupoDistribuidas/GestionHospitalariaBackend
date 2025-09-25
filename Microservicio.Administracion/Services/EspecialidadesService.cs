using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microservicio.Administracion.Data;   // Tu DbContext
using Microservicio.Administracion.Models; // Tu modelo Especialidad
using Microservicio.Administracion.Protos; // <- Debe coincidir con option csharp_namespace

namespace Microservicio.Administracion.Services
{
    public class EspecialidadesServiceImpl : EspecialidadesService.EspecialidadesServiceBase
    {
        private readonly AdministracionDbContext _dbContext;

        public EspecialidadesServiceImpl(AdministracionDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task<EspecialidadResponse> ObtenerEspecialidadPorId(EspecialidadPorIdRequest request, ServerCallContext context)
        {
            var especialidad = await _dbContext.Especialidades.FindAsync(request.IdEspecialidad);
            if (especialidad == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Especialidad no encontrada"));

            return MapToResponse(especialidad);
        }

        public override async Task<EspecialidadesListResponse> ObtenerTodasEspecialidades(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            var especialidades = await _dbContext.Especialidades.ToListAsync();
            var response = new EspecialidadesListResponse();
            response.Especialidades.AddRange(especialidades.Select(MapToResponse));
            return response;
        }

        public override async Task<EspecialidadResponse> InsertarEspecialidad(InsertarEspecialidadRequest request, ServerCallContext context)
        {
            var especialidad = new Especialidad
            {
                Nombre = request.Nombre
            };

            _dbContext.Especialidades.Add(especialidad);
            await _dbContext.SaveChangesAsync();
            return MapToResponse(especialidad);
        }

        public override async Task<EspecialidadResponse> ActualizarEspecialidad(ActualizarEspecialidadRequest request, ServerCallContext context)
        {
            var especialidad = await _dbContext.Especialidades.FindAsync(request.IdEspecialidad);
            if (especialidad == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Especialidad no encontrada"));

            especialidad.Nombre = request.Nombre;
            await _dbContext.SaveChangesAsync();
            return MapToResponse(especialidad);
        }

        public override async Task<EliminarEspecialidadResponse> EliminarEspecialidad(EliminarEspecialidadRequest request, ServerCallContext context)
        {
            var especialidad = await _dbContext.Especialidades.FindAsync(request.IdEspecialidad);
            if (especialidad == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Especialidad no encontrada"));

            _dbContext.Especialidades.Remove(especialidad);
            await _dbContext.SaveChangesAsync();
            return new EliminarEspecialidadResponse { Exito = true };
        }

        private EspecialidadResponse MapToResponse(Especialidad especialidad)
        {
            return new EspecialidadResponse
            {
                IdEspecialidad = especialidad.IdEspecialidad,
                Nombre = especialidad.Nombre
            };
        }
    }
}
