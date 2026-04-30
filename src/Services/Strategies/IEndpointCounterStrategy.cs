namespace CodeMetricCollector.Services.Strategies;

public interface IEndpointCounterStrategy
{
    bool CanHandle(string fileName);
    IReadOnlyList<string> ExtractRoutes(string fileContent);
}
