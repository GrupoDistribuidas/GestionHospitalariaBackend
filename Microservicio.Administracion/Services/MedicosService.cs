using Grpc.Core;
using Microservicio.Administracion.Protos;
using Microservicio.Administracion.Data;
using Microservicio.Administracion.Models;
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Administracion.Services
{
    public class MedicosServiceImpl : MedicosService.MedicosServiceBase
    {
        private readonly AdministracionDbContext _dbContext;

        public MedicosServiceImpl(AdministracionDbContext dbContext)
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
            // Si se proporciona header x-centro-medico, filtrar por ese centro
            int? centroFilter = null;
            var md = context.RequestHeaders.Get("x-centro-medico");
            if (md != null && int.TryParse(md.Value, out var parsed)) centroFilter = parsed;

            List<Empleado> medicos;
            if (centroFilter.HasValue)
            {
                medicos = await _dbContext.Empleados.Where(e => e.IdCentroMedico == centroFilter.Value).ToListAsync();
            }
            else
            {
                medicos = await _dbContext.Empleados.ToListAsync();
            }

            var response = new MedicosListResponse();
            response.Medicos.AddRange(medicos.Select(MapToResponse));
            return response;
        }

        public override async Task<MedicoResponse> InsertarMedico(InsertarMedicoRequest request, ServerCallContext context)
        {
            // Validar existencia de la especialidad
            var especialidad = await _dbContext.Especialidades
                .FirstOrDefaultAsync(e => e.IdEspecialidad == request.IdEspecialidad);

            if (especialidad == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"La especialidad con ID {request.IdEspecialidad} no existe"));

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

            // Si se especifica x-centro-medico, validar que el médico pertenece a ese centro
            var md2 = context.RequestHeaders.Get("x-centro-medico");
            if (md2 != null && int.TryParse(md2.Value, out var c2) && medico.IdCentroMedico != c2)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Médico no encontrado en este centro"));
            }

            // Validar existencia de la especialidad
            var especialidad = await _dbContext.Especialidades
                .FirstOrDefaultAsync(e => e.IdEspecialidad == request.IdEspecialidad);

            if (especialidad == null)
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"La especialidad con ID {request.IdEspecialidad} no existe"));

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
