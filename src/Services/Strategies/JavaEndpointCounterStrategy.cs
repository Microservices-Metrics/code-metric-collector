using System.Text.RegularExpressions;

namespace CodeMetricCollector.Services.Strategies;

/// <summary>
/// Counts HTTP endpoints in Java Spring controllers.
/// Detects @GetMapping, @PostMapping, @PutMapping, @DeleteMapping, @PatchMapping,
/// and @RequestMapping with explicit method= attribute at method level.
/// </summary>
public partial class JavaEndpointCounterStrategy : IEndpointCounterStrategy
{
    // Matches @GetMapping, @PostMapping, @PutMapping, @DeleteMapping, @PatchMapping
    [GeneratedRegex(@"@(GetMapping|PostMapping|PutMapping|DeleteMapping|PatchMapping)\s*[\(\n\r\s]",
        RegexOptions.Multiline)]
    private static partial Regex HttpMethodAnnotationRegex();

    // Matches @RequestMapping with an explicit HTTP method (typically method-level usage)
    [GeneratedRegex(@"@RequestMapping\s*\([^)]*method\s*=\s*RequestMethod\.",
        RegexOptions.Multiline)]
    private static partial Regex RequestMappingWithMethodRegex();

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".java", StringComparison.OrdinalIgnoreCase);

    public int CountEndpoints(string fileContent)
    {
        var count = HttpMethodAnnotationRegex().Matches(fileContent).Count;
        count += RequestMappingWithMethodRegex().Matches(fileContent).Count;
        return count;
    }
}
