using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using Microservicio.Administracion.Protos;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MedicosController : ControllerBase
    {
        private readonly MedicosService.MedicosServiceClient _medicosClient;

        public MedicosController(IConfiguration configuration)
        {
            // Dirección del microservicio de administración (ajusta el puerto si es necesario)
            var adminServiceUrl = configuration["Grpc:AdministracionUrl"] ?? "http://localhost:5002";
            var channel = GrpcChannel.ForAddress(adminServiceUrl);
            _medicosClient = new MedicosService.MedicosServiceClient(channel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var response = await _medicosClient.ObtenerTodosMedicosAsync(new Empty());
            return Ok(response.Medicos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _medicosClient.ObtenerMedicoPorIdAsync(new MedicoPorIdRequest { IdEmpleado = id });
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] InsertarMedicoRequest request)
        {
            var response = await _medicosClient.InsertarMedicoAsync(request);
            return Ok(response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ActualizarMedicoRequest request)
        {
            request.IdEmpleado = id;
            var response = await _medicosClient.ActualizarMedicoAsync(request);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var response = await _medicosClient.EliminarMedicoAsync(new EliminarMedicoRequest { IdEmpleado = id });
            return Ok(response);
        }
    }
}
