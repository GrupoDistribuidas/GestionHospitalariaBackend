using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using AdminProtos = Microservicio.Administracion.Protos; // Alias
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todos los endpoints
    public class MedicosController : ControllerBase
    {
        private readonly AdminProtos.MedicosService.MedicosServiceClient _medicosClient;

        public MedicosController(AdminProtos.MedicosService.MedicosServiceClient medicosClient)
        {
            _medicosClient = medicosClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var centroClaim = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroClaim)) headers = new Metadata { { "x-centro-medico", centroClaim } };

                var response = await _medicosClient.ObtenerTodosMedicosAsync(new Empty(), headers != null ? new CallOptions(headers) : default);
                return Ok(response.Medicos);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var centroClaim = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroClaim)) headers = new Metadata { { "x-centro-medico", centroClaim } };

                var response = await _medicosClient.ObtenerMedicoPorIdAsync(
                    new AdminProtos.MedicoPorIdRequest { IdEmpleado = id }, headers != null ? new CallOptions(headers) : default
                );
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AdminProtos.InsertarMedicoRequest request)
        {
            try
            {
                var response = await _medicosClient.InsertarMedicoAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AdminProtos.ActualizarMedicoRequest request)
        {
            try
            {
                request.IdEmpleado = id;
                var response = await _medicosClient.ActualizarMedicoAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var response = await _medicosClient.EliminarMedicoAsync(
                    new AdminProtos.EliminarMedicoRequest { IdEmpleado = id }
                );
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }
    }
}
