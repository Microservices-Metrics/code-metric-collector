using CodeMetricCollector.Services.Strategies;

namespace CodeMetricCollector.Services;

public class EndpointCountService
{
    private readonly IEnumerable<IEndpointCounterStrategy> _strategies;
    private readonly GitHubService _gitHubService;
    private readonly ILogger<EndpointCountService> _logger;

    public EndpointCountService(
        IEnumerable<IEndpointCounterStrategy> strategies,
        GitHubService gitHubService,
        ILogger<EndpointCountService> logger)
    {
        _strategies = strategies;
        _gitHubService = gitHubService;
        _logger = logger;
    }

    public async Task<EndpointCountResult> CountEndpointsAsync(string repositoryUrl)
    {
        var files = await _gitHubService.GetControllerFilesAsync(repositoryUrl);

        var routes = new List<string>();
        var seenRoutes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var strategy = _strategies.FirstOrDefault(s => s.CanHandle(file.Name));

            if (strategy is null)
            {
                _logger.LogDebug("No strategy found for file '{FileName}', skipping.", file.Name);
                continue;
            }

            var fileRoutes = strategy.ExtractRoutes(file.Content);
            _logger.LogInformation("File '{FileName}' — {Count} endpoint(s) found.", file.Name, fileRoutes.Count);

            foreach (var route in fileRoutes)
            {
                if (seenRoutes.Add(route))
                {
                    routes.Add(route);
                }
            }
        }

        _logger.LogInformation("Total endpoints found across {FileCount} file(s): {Total}", files.Count, routes.Count);
        return new EndpointCountResult(routes);
    }
}

public record EndpointCountResult(IReadOnlyList<string> Routes);
