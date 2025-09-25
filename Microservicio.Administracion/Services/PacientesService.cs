using Grpc.Core;
using Microservicio.ClinicaExtension.Data; // DbContext de la clínica
using Microservicio.ClinicaExtension.Models; // Modelo Paciente
using Microservicio.ClinicaExtension.Protos; // Tipos generados por Proto
using Microsoft.EntityFrameworkCore;

namespace Microservicio.Administracion.Services
{
    // Cambié el nombre de la clase para evitar conflicto con el service generado
    public class PacientesServiceImpl : PacientesService.PacientesServiceBase
    {
        private readonly ClinicaExtensionDbContext _dbContext;

        public PacientesServiceImpl(ClinicaExtensionDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public override async Task<PacienteResponse> ObtenerPacientePorId(PacientePorIdRequest request, ServerCallContext context)
        {
            var paciente = await _dbContext.Pacientes.FindAsync(request.IdPaciente);
            if (paciente == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));

            return MapToResponse(paciente);
        }

        public override async Task<PacientesListResponse> ObtenerTodosPacientes(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            var pacientes = await _dbContext.Pacientes.ToListAsync();
            var response = new PacientesListResponse();
            response.Pacientes.AddRange(pacientes.Select(MapToResponse));
            return response;
        }

        public override async Task<PacienteResponse> InsertarPaciente(InsertarPacienteRequest request, ServerCallContext context)
        {
            var paciente = new Paciente
            {
                Nombre = request.Nombre,
                Cedula = request.Cedula,
                FechaNacimiento = DateTime.Parse(request.FechaNacimiento),
                Telefono = request.Telefono,
                Direccion = request.Direccion
            };

            _dbContext.Pacientes.Add(paciente);
            await _dbContext.SaveChangesAsync();
            return MapToResponse(paciente);
        }

        public override async Task<PacienteResponse> ActualizarPaciente(ActualizarPacienteRequest request, ServerCallContext context)
        {
            var paciente = await _dbContext.Pacientes.FindAsync(request.IdPaciente);
            if (paciente == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));

            paciente.Nombre = request.Nombre;
            paciente.Cedula = request.Cedula;
            paciente.FechaNacimiento = DateTime.Parse(request.FechaNacimiento);
            paciente.Telefono = request.Telefono;
            paciente.Direccion = request.Direccion;

            await _dbContext.SaveChangesAsync();
            return MapToResponse(paciente);
        }

        public override async Task<EliminarPacienteResponse> EliminarPaciente(EliminarPacienteRequest request, ServerCallContext context)
        {
            var paciente = await _dbContext.Pacientes.FindAsync(request.IdPaciente);
            if (paciente == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));

            _dbContext.Pacientes.Remove(paciente);
            await _dbContext.SaveChangesAsync();
            return new EliminarPacienteResponse { Exito = true };
        }

        private PacienteResponse MapToResponse(Paciente paciente)
        {
            return new PacienteResponse
            {
                IdPaciente = paciente.IdPaciente,
                Nombre = paciente.Nombre,
                Cedula = paciente.Cedula,
                Telefono = paciente.Telefono ?? string.Empty,
                Direccion = paciente.Direccion ?? string.Empty,
                FechaNacimiento = paciente.FechaNacimiento.ToString("yyyy-MM-dd")
            };
        }
    }
}
