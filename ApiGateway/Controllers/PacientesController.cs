using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using ClinicaProtos = Microservicio.ClinicaExtension.Protos; // Alias para evitar conflicto
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requiere autenticación JWT para todos los endpoints
    public class PacientesController : ControllerBase
    {
        private readonly ClinicaProtos.PacientesService.PacientesServiceClient _pacientesClient;

        // Inyección del cliente gRPC
        public PacientesController(ClinicaProtos.PacientesService.PacientesServiceClient pacientesClient)
        {
            _pacientesClient = pacientesClient;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var response = await _pacientesClient.ObtenerTodosPacientesAsync(new Empty());
                return Ok(response.Pacientes);
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
                var response = await _pacientesClient.ObtenerPacientePorIdAsync(
                    new ClinicaProtos.PacientePorIdRequest { IdPaciente = id }
                );
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ClinicaProtos.InsertarPacienteRequest request)
        {
            try
            {
                var response = await _pacientesClient.InsertarPacienteAsync(request);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode((int)ex.StatusCode, ex.Status.Detail);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ClinicaProtos.ActualizarPacienteRequest request)
        {
            try
            {
                request.IdPaciente = id;
                var response = await _pacientesClient.ActualizarPacienteAsync(request);
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
                var response = await _pacientesClient.EliminarPacienteAsync(
                    new ClinicaProtos.EliminarPacienteRequest { IdPaciente = id }
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
