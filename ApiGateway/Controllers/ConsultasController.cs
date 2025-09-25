using Microsoft.AspNetCore.Mvc;
using Microservicio.Consultas.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todos los endpoints
    public class ConsultasController : ControllerBase
    {
        private readonly ConsultasService.ConsultasServiceClient _client;

        public ConsultasController(ConsultasService.ConsultasServiceClient client)
        {
            _client = client;
        }

        [HttpGet]
        public async Task<IActionResult> GetTodasConsultas()
        {
            var request = new Google.Protobuf.WellKnownTypes.Empty();
            try
            {
                var response = await _client.ObtenerTodasConsultasAsync(request);
                return Ok(response.Consultas);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetConsultaPorId(int id)
        {
            var request = new ConsultaPorIdRequest { IdConsultaMedica = id };
            try
            {
                var response = await _client.ObtenerConsultaPorIdAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CrearConsulta([FromBody] InsertarConsultaRequest request)
        {
            try
            {
                var response = await _client.InsertarConsultaAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarConsulta(int id, [FromBody] ActualizarConsultaRequest request)
        {
            request.IdConsultaMedica = id;
            try
            {
                var response = await _client.ActualizarConsultaAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarConsulta(int id)
        {
            var request = new EliminarConsultaRequest { IdConsultaMedica = id };
            try
            {
                var response = await _client.EliminarConsultaAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }
    }
}
