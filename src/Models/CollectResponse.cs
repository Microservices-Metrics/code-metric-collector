namespace CodeMetricCollector.Models;

public class CollectResponse
{
    public MetricInfo Metric { get; set; } = new();
    public MeasurementInfo Measurement { get; set; } = new();
}

public class MetricInfo
{
    public string Name { get; set; } = string.Empty;
    public string CollectorStrategy { get; set; } = string.Empty;
}

public class MeasurementInfo
{
    public string ApiIdentifier { get; set; } = string.Empty;
    public List<string> Routes { get; set; } = [];
    public int Value => Routes.Count;
    public string Unit { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
}
