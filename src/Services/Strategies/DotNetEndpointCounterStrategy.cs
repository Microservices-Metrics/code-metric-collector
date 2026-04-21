using System.Text.RegularExpressions;

namespace CodeMetricCollector.Services.Strategies;

/// <summary>
/// Counts HTTP endpoints in .NET controller classes.
/// Detects [HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch], [HttpHead], [HttpOptions].
/// </summary>
public partial class DotNetEndpointCounterStrategy : IEndpointCounterStrategy
{
    // Matches [HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch], [HttpHead], [HttpOptions]
    // Handles both [HttpGet] and [HttpGet("route")] forms
    [GeneratedRegex(@"\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch|HttpHead|HttpOptions)\s*[\]\(]",
        RegexOptions.Multiline)]
    private static partial Regex HttpAttributeRegex();

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    public int CountEndpoints(string fileContent)
    {
        return HttpAttributeRegex().Matches(fileContent).Count;
    }
}
