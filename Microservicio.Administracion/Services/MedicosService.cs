using Grpc.Core;
using Microservicio.Administracion.Protos;
using Microservicio.Administracion.Data;
using Microservicio.Administracion.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Administracion.Services
{
    public class MedicosService : Protos.MedicosService.MedicosServiceBase
    {
        private readonly AdministracionDbContext _dbContext;

        public MedicosService(AdministracionDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task<MedicoResponse> ObtenerMedicoPorId(MedicoPorIdRequest request, ServerCallContext context)
        {
            var medico = await _dbContext.Empleados.FindAsync(request.IdEmpleado);
            if (medico == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado"));
            return MapToResponse(medico);
        }

    public override async Task<MedicosListResponse> ObtenerTodosMedicos(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            var medicos = await _dbContext.Empleados.ToListAsync();
            var response = new MedicosListResponse();
            response.Medicos.AddRange(medicos.Select(MapToResponse));
            return response;
        }

        public override async Task<MedicoResponse> InsertarMedico(InsertarMedicoRequest request, ServerCallContext context)
        {
            var medico = new Empleado
            {
                IdCentroMedico = request.IdCentroMedico,
                IdTipo = request.IdTipo,
                IdEspecialidad = request.IdEspecialidad,
                Nombre = request.Nombre,
                Telefono = request.Telefono,
                Email = request.Email,
                Salario = (decimal?)request.Salario,
                Horario = request.Horario,
                Estado = request.Estado
            };
            _dbContext.Empleados.Add(medico);
            await _dbContext.SaveChangesAsync();
            return MapToResponse(medico);
        }

        public override async Task<MedicoResponse> ActualizarMedico(ActualizarMedicoRequest request, ServerCallContext context)
        {
            var medico = await _dbContext.Empleados.FindAsync(request.IdEmpleado);
            if (medico == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado"));

            medico.IdCentroMedico = request.IdCentroMedico;
            medico.IdTipo = request.IdTipo;
            medico.IdEspecialidad = request.IdEspecialidad;
            medico.Nombre = request.Nombre;
            medico.Telefono = request.Telefono;
            medico.Email = request.Email;
            medico.Salario = (decimal?)request.Salario;
            medico.Horario = request.Horario;
            medico.Estado = request.Estado;

            await _dbContext.SaveChangesAsync();
            return MapToResponse(medico);
        }

        public override async Task<EliminarMedicoResponse> EliminarMedico(EliminarMedicoRequest request, ServerCallContext context)
        {
            var medico = await _dbContext.Empleados.FindAsync(request.IdEmpleado);
            if (medico == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado"));
            _dbContext.Empleados.Remove(medico);
            await _dbContext.SaveChangesAsync();
            return new EliminarMedicoResponse { Exito = true };
        }

        private MedicoResponse MapToResponse(Empleado medico)
        {
            return new MedicoResponse
            {
                IdEmpleado = medico.IdEmpleado,
                IdCentroMedico = medico.IdCentroMedico ?? 0,
                IdTipo = medico.IdTipo ?? 0,
                IdEspecialidad = medico.IdEspecialidad ?? 0,
                Nombre = medico.Nombre ?? string.Empty,
                Telefono = medico.Telefono ?? string.Empty,
                Email = medico.Email ?? string.Empty,
                Salario = (double)(medico.Salario ?? 0),
                Horario = medico.Horario ?? string.Empty,
                Estado = medico.Estado ?? string.Empty
            };
        }
    }
}
