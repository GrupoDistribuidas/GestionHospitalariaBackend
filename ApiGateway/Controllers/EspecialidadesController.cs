using Administracion.Protos;
using ApiGateway.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/especialidades")]
public class EspecialidadesController : ControllerBase
{
    private readonly EspecialidadesService.EspecialidadesServiceClient _client;

    public EspecialidadesController(EspecialidadesService.EspecialidadesServiceClient client)
        => _client = client;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EspecialidadResponse>>> List(CancellationToken ct)
    {
        try
        {
            var resp = await _client.ListAsync(new Empty(), headers: BuildAuthMetadata(), cancellationToken: ct);
            return Ok(resp.Items.Select(x => new EspecialidadResponse(x.Id, x.Nombre)));
        }
        catch (RpcException ex) { return MapRpcException(ex); }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EspecialidadResponse>> GetById(int id, CancellationToken ct)
    {
        try
        {
            var r = await _client.GetByIdAsync(new IdRequest { Id = id }, BuildAuthMetadata(), cancellationToken: ct);
            return Ok(new EspecialidadResponse(r.Id, r.Nombre));
        }
        catch (RpcException ex) { return MapRpcException(ex); }
    }

    [HttpPost]
    public async Task<ActionResult<EspecialidadResponse>> Create([FromBody] EspecialidadCreateRequest body, CancellationToken ct)
    {
        try
        {
            var r = await _client.CreateAsync(new CreateEspecialidadRequest { Nombre = body.nombre },
                                              BuildAuthMetadata(), cancellationToken: ct);
            return CreatedAtAction(nameof(GetById), new { id = r.Id }, new EspecialidadResponse(r.Id, r.Nombre));
        }
        catch (RpcException ex) { return MapRpcException(ex); }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EspecialidadResponse>> Update(int id, [FromBody] EspecialidadUpdateRequest body, CancellationToken ct)
    {
        try
        {
            var r = await _client.UpdateAsync(new UpdateEspecialidadRequest { Id = id, Nombre = body.nombre ?? "" },
                                              BuildAuthMetadata(), cancellationToken: ct);
            return Ok(new EspecialidadResponse(r.Id, r.Nombre));
        }
        catch (RpcException ex) { return MapRpcException(ex); }
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            var r = await _client.DeleteAsync(new IdRequest { Id = id }, BuildAuthMetadata(), cancellationToken: ct);
            return r.Deleted ? NoContent() : NotFound();
        }
        catch (RpcException ex) { return MapRpcException(ex); }
    }

    // --- helpers ---
private static ActionResult MapRpcException(RpcException ex)
{
    return ex.StatusCode switch
    {
        Grpc.Core.StatusCode.InvalidArgument   => new BadRequestObjectResult(new { error = ex.Status.Detail }),
        Grpc.Core.StatusCode.NotFound          => new NotFoundObjectResult(new { error = ex.Status.Detail }),
        Grpc.Core.StatusCode.AlreadyExists     => new ConflictObjectResult(new { error = ex.Status.Detail }),
        Grpc.Core.StatusCode.Unauthenticated   => new UnauthorizedObjectResult(new { error = ex.Status.Detail }),
        Grpc.Core.StatusCode.PermissionDenied  => new ObjectResult(new { error = ex.Status.Detail }) { StatusCode = 403 },
        Grpc.Core.StatusCode.Unavailable       => new ObjectResult(new { error = "Servicio no disponible" }) { StatusCode = 503 },
        _ => new ObjectResult(new { error = ex.Status.Detail, code = ex.StatusCode.ToString() }) { StatusCode = 500 }
    };
}


    private Grpc.Core.Metadata BuildAuthMetadata()
    {
        // Propaga el JWT (si tienes Microservicio.Autenticacion)
        var auth = Request.Headers.Authorization.ToString();
        var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth.Substring(7) : null;
        var md = new Grpc.Core.Metadata();
        if (!string.IsNullOrEmpty(token))
            md.Add("authorization", $"Bearer {token}");
        return md;
    }
}
