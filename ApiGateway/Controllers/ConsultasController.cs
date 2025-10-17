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
                var centroClaim = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroClaim)) headers = new Metadata { { "x-centro-medico", centroClaim } };

                var response = await _client.ObtenerTodasConsultasAsync(request, headers != null ? new CallOptions(headers) : default);
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
                var centroClaim = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroClaim)) headers = new Metadata { { "x-centro-medico", centroClaim } };

                var response = await _client.ObtenerConsultaPorIdAsync(request, headers != null ? new CallOptions(headers) : default);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        // DTO que acepta propiedades en snake_case (compatibilidad con frontends que envían id_paciente)
        public class CrearConsultaDto
        {
            public string? fecha { get; set; }
            public string? hora { get; set; }
            public string? motivo { get; set; }
            public string? diagnostico { get; set; }
            public string? tratamiento { get; set; }
            public int id_paciente { get; set; }
            public int id_medico { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> CrearConsulta([FromBody] CrearConsultaDto dto)
        {
            // Validar que se reciban ids válidos antes de llamar a los microservicios
            if (dto == null || dto.id_paciente <= 0 || dto.id_medico <= 0)
                return BadRequest("id_paciente e id_medico son requeridos y deben ser mayores que 0");

            var request = new InsertarConsultaRequest
            {
                Fecha = dto.fecha ?? string.Empty,
                Hora = dto.hora ?? string.Empty,
                Motivo = dto.motivo ?? string.Empty,
                Diagnostico = dto.diagnostico ?? string.Empty,
                Tratamiento = dto.tratamiento ?? string.Empty,
                IdPaciente = dto.id_paciente,
                IdMedico = dto.id_medico
            };

            try
            {
                var centroClaim = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroClaim)) headers = new Metadata { { "x-centro-medico", centroClaim } };

                var response = await _client.InsertarConsultaAsync(request, headers != null ? new CallOptions(headers) : default);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }

        public class ActualizarConsultaDto
        {
            public string? fecha { get; set; }
            public string? hora { get; set; }
            public string? motivo { get; set; }
            public string? diagnostico { get; set; }
            public string? tratamiento { get; set; }
            public int id_paciente { get; set; }
            public int id_medico { get; set; }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarConsulta(int id, [FromBody] ActualizarConsultaDto dto)
        {
            if (dto == null || dto.id_paciente <= 0 || dto.id_medico <= 0)
                return BadRequest("id_paciente e id_medico son requeridos y deben ser mayores que 0");

            var request = new ActualizarConsultaRequest
            {
                IdConsultaMedica = id,
                Fecha = dto.fecha ?? string.Empty,
                Hora = dto.hora ?? string.Empty,
                Motivo = dto.motivo ?? string.Empty,
                Diagnostico = dto.diagnostico ?? string.Empty,
                Tratamiento = dto.tratamiento ?? string.Empty,
                IdPaciente = dto.id_paciente,
                IdMedico = dto.id_medico
            };

            try
            {
                var centroClaim = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroClaim)) headers = new Metadata { { "x-centro-medico", centroClaim } };

                var response = await _client.ActualizarConsultaAsync(request, headers != null ? new CallOptions(headers) : default);
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
                var centroClaim = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
                Metadata? headers = null;
                if (!string.IsNullOrEmpty(centroClaim)) headers = new Metadata { { "x-centro-medico", centroClaim } };

                var response = await _client.EliminarConsultaAsync(request, headers != null ? new CallOptions(headers) : default);
                return Ok(response);
            }
            catch (RpcException ex)
            {
                return StatusCode(500, ex.Status.Detail);
            }
        }
    }
}
