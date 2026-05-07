using Microsoft.AspNetCore.Mvc;

namespace CodeMetricCollector.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy" });
    }
}
