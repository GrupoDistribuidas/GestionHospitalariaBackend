using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microservicio.Administracion.Data;
using Administracion.Protos;

public class EspecialidadesGrpcService : Administracion.Protos.EspecialidadesService.EspecialidadesServiceBase
{
    private readonly HospitalContext _db;
    public EspecialidadesGrpcService(HospitalContext db) => _db = db;

    public override async Task<EspecialidadDto> Create(CreateEspecialidadRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "nombre es requerido"));

        var entity = new Especialidad { Nombre = request.Nombre.Trim() };
        _db.Especialidades.Add(entity);
        await _db.SaveChangesAsync(context.CancellationToken);
        return new EspecialidadDto { Id = entity.Id, Nombre = entity.Nombre };
    }

    public override async Task<EspecialidadDto> GetById(IdRequest request, ServerCallContext context)
    {
        var entity = await _db.Especialidades.FindAsync(new object?[] { request.Id }, context.CancellationToken);
        if (entity is null)
            throw new RpcException(new Status(StatusCode.NotFound, "especialidad no encontrada"));

        return new EspecialidadDto { Id = entity.Id, Nombre = entity.Nombre };
    }

    public override async Task<EspecialidadDto> Update(UpdateEspecialidadRequest request, ServerCallContext context)
    {
        var entity = await _db.Especialidades.FindAsync(new object?[] { request.Id }, context.CancellationToken);
        if (entity is null)
            throw new RpcException(new Status(StatusCode.NotFound, "especialidad no encontrada"));

        if (!string.IsNullOrWhiteSpace(request.Nombre))
            entity.Nombre = request.Nombre.Trim();

        await _db.SaveChangesAsync(context.CancellationToken);
        return new EspecialidadDto { Id = entity.Id, Nombre = entity.Nombre };
    }

    public override async Task<DeleteResponse> Delete(IdRequest request, ServerCallContext context)
    {
        var entity = await _db.Especialidades.FindAsync(new object?[] { request.Id }, context.CancellationToken);
        if (entity is null) return new DeleteResponse { Deleted = false };

        _db.Especialidades.Remove(entity);
        await _db.SaveChangesAsync(context.CancellationToken);
        return new DeleteResponse { Deleted = true };
    }

    public override async Task<ListEspecialidadesResponse> List(Empty request, ServerCallContext context)
    {
        var items = await _db.Especialidades
            .AsNoTracking()
            .Select(e => new EspecialidadDto { Id = e.Id, Nombre = e.Nombre })
            .ToListAsync(context.CancellationToken);

        var resp = new ListEspecialidadesResponse();
        resp.Items.AddRange(items);
        return resp;
    }
}
