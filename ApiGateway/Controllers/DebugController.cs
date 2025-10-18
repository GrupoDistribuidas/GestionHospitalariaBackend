using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers
{
    [ApiController]
    [Route("debug")]
    public class DebugController : ControllerBase
    {
        [HttpGet("claims")]
        [Authorize]
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            var centro = User.Claims.FirstOrDefault(c => c.Type == "id_centro_medico")?.Value;
            return Ok(new { centro, claims });
        }
    }
}
