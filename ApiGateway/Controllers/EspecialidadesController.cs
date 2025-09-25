using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using AdministracionProtos = Microservicio.Administracion.Protos; // Alias para evitar conflicto
using Grpc.Core;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspecialidadesController : ControllerBase
    {
        private readonly AdministracionProtos.EspecialidadesService.EspecialidadesServiceClient _especialidadesClient;

        // Inyección del cliente gRPC
        public EspecialidadesController(AdministracionProtos.EspecialidadesService.EspecialidadesServiceClient especialidadesClient)
        {
            _especialidadesClient = especialidadesClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _especialidadesClient.ObtenerTodasEspecialidadesAsync(new Empty());
                return Ok(response.Especialidades);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var response = await _especialidadesClient.ObtenerEspecialidadPorIdAsync(
                    new AdministracionProtos.EspecialidadPorIdRequest { IdEspecialidad = id }
                );
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdministracionProtos.InsertarEspecialidadRequest request)
        {
            try
            {
                var response = await _especialidadesClient.InsertarEspecialidadAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AdministracionProtos.ActualizarEspecialidadRequest request)
        {
            try
            {
                request.IdEspecialidad = id;
                var response = await _especialidadesClient.ActualizarEspecialidadAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _especialidadesClient.EliminarEspecialidadAsync(
                    new AdministracionProtos.EliminarEspecialidadRequest { IdEspecialidad = id }
                );
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Status.Detail);
            }
        }
    }
}
