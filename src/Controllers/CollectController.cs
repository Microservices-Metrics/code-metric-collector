using CodeMetricCollector.Models;
using CodeMetricCollector.Services;
using Microsoft.AspNetCore.Mvc;

namespace CodeMetricCollector.Controllers;

[ApiController]
[Route("collect")]
public class CollectController : ControllerBase
{
    private readonly EndpointCountService _endpointCountService;
    private readonly ILogger<CollectController> _logger;

    public CollectController(EndpointCountService endpointCountService, ILogger<CollectController> logger)
    {
        _endpointCountService = endpointCountService;
        _logger = logger;
    }

    /// <summary>
    /// Counts the number of HTTP endpoints declared in a GitHub controller folder.
    /// Supports Java (Spring) and .NET controller classes.
    /// </summary>
    /// <param name="request">The URL pointing to a controller folder on GitHub.</param>
    [HttpPost]
    [ProducesResponseType(typeof(CollectResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CollectResponse>> Post([FromBody] CollectRequest request)
    {
        EndpointCountResult endpointResult;

        try
        {
            endpointResult = await _endpointCountService.CountEndpointsAsync(request.UrlControllers);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid URL provided: {Url}", request.UrlControllers);
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fetch repository content from: {Url}", request.UrlControllers);
            return BadRequest(new { error = $"Failed to fetch repository content: {ex.Message}" });
        }

        var response = new CollectResponse
        {
            Metric = new MetricInfo
            {
                Name = "Operations per service",
                CollectorStrategy = "code"
            },
            Measurement = new MeasurementInfo
            {
                ApiIdentifier = Guid.NewGuid().ToString(),
                Routes = endpointResult.Routes.ToList(),
                Unit = "operations",
                Timestamp = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
            }
        };

        return Ok(response);
    }
}
