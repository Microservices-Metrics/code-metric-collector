namespace CodeMetricCollector.Services.Strategies;

public interface IEndpointCounterStrategy
{
    bool CanHandle(string fileName);
    int CountEndpoints(string fileContent);
}
