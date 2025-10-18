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
        private readonly IClinicaDbContextFactory _dbFactory;

        public PacientesServiceImpl(IClinicaDbContextFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public override async Task<PacienteResponse> ObtenerPacientePorId(PacientePorIdRequest request, ServerCallContext context)
        {
            // Resolver id del centro desde los metadata (header) si está presente
            int centroId = 1; // por defecto central
            var md = context.RequestHeaders.Get("x-centro-medico");
            if (md != null && int.TryParse(md.Value, out var parsed)) centroId = parsed;

            using var db = _dbFactory.CreateForCentro(centroId);
            var paciente = await db.Pacientes.FindAsync(request.IdPaciente);
            if (paciente == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));

            return MapToResponse(paciente);
        }

        public override async Task<PacientesListResponse> ObtenerTodosPacientes(Google.Protobuf.WellKnownTypes.Empty request, ServerCallContext context)
        {
            var md2 = context.RequestHeaders.Get("x-centro-medico");
            int centroId2 = 1;
            if (md2 != null && int.TryParse(md2.Value, out var p2)) centroId2 = p2;

            using var db2 = _dbFactory.CreateForCentro(centroId2);
            var pacientes = await db2.Pacientes.ToListAsync();
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

            var md3 = context.RequestHeaders.Get("x-centro-medico");
            int centroId3 = 1;
            if (md3 != null && int.TryParse(md3.Value, out var p3)) centroId3 = p3;

            using var db3 = _dbFactory.CreateForCentro(centroId3);
            db3.Pacientes.Add(paciente);
            await db3.SaveChangesAsync();
            return MapToResponse(paciente);
        }

        public override async Task<PacienteResponse> ActualizarPaciente(ActualizarPacienteRequest request, ServerCallContext context)
        {
            var md4 = context.RequestHeaders.Get("x-centro-medico");
            int centroId4 = 1;
            if (md4 != null && int.TryParse(md4.Value, out var p4)) centroId4 = p4;

            using var db4 = _dbFactory.CreateForCentro(centroId4);
            var paciente = await db4.Pacientes.FindAsync(request.IdPaciente);
            if (paciente == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));

            paciente.Nombre = request.Nombre;
            paciente.Cedula = request.Cedula;
            paciente.FechaNacimiento = DateTime.Parse(request.FechaNacimiento);
            paciente.Telefono = request.Telefono;
            paciente.Direccion = request.Direccion;

            await db4.SaveChangesAsync();
            return MapToResponse(paciente);
        }

        public override async Task<EliminarPacienteResponse> EliminarPaciente(EliminarPacienteRequest request, ServerCallContext context)
        {
            var md5 = context.RequestHeaders.Get("x-centro-medico");
            int centroId5 = 1;
            if (md5 != null && int.TryParse(md5.Value, out var p5)) centroId5 = p5;

            using var db5 = _dbFactory.CreateForCentro(centroId5);
            var paciente = await db5.Pacientes.FindAsync(request.IdPaciente);
            if (paciente == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Paciente no encontrado"));

            db5.Pacientes.Remove(paciente);
            await db5.SaveChangesAsync();
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
