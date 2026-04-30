using System.Text.RegularExpressions;

namespace CodeMetricCollector.Services.Strategies;

/// <summary>
/// Counts HTTP endpoints in Java Spring controllers.
/// Detects @GetMapping, @PostMapping, @PutMapping, @DeleteMapping, @PatchMapping,
/// and @RequestMapping with explicit method= attribute at method level.
/// Combines class-level @RequestMapping prefix with each method-level route.
/// </summary>
public partial class JavaEndpointCounterStrategy : IEndpointCounterStrategy
{
    // Matches @GetMapping, @PostMapping, @PutMapping, @DeleteMapping, @PatchMapping
    // Captures optional arguments, e.g. @GetMapping("/users") or @GetMapping(path = "/users").
    [GeneratedRegex(@"@(GetMapping|PostMapping|PutMapping|DeleteMapping|PatchMapping)\s*(\((?<args>.*?)\))?",
        RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex HttpMethodAnnotationRegex();

    // Matches @RequestMapping with an explicit HTTP method (method-level usage).
    [GeneratedRegex(@"@RequestMapping\s*\((?<args>[^)]*method\s*=\s*RequestMethod\.[^)]*)\)",
        RegexOptions.Multiline)]
    private static partial Regex RequestMappingWithMethodRegex();

    // Matches class-level @RequestMapping without a method= attribute.
    [GeneratedRegex(@"@RequestMapping\s*\((?<args>(?!.*?method\s*=).*?)\)",
        RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex ClassRequestMappingRegex();

    [GeneratedRegex(@"(?:path|value)\s*=\s*""([^""]*)""")]
    private static partial Regex NamedRouteRegex();

    [GeneratedRegex("\"([^\"]*)\"")]
    private static partial Regex QuotedStringRegex();

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".java", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> ExtractRoutes(string fileContent)
    {
        var classMatch = ClassRequestMappingRegex().Match(fileContent);
        var classPrefix = classMatch.Success
            ? NormalizeSegment(ExtractStringFromArgs(classMatch.Groups["args"].Value))
            : string.Empty;

        var routes = new List<string>();

        foreach (Match match in HttpMethodAnnotationRegex().Matches(fileContent))
        {
            var methodSegment = NormalizeSegment(ExtractStringFromArgs(match.Groups["args"].Value));
            routes.Add(CombineRoutes(classPrefix, methodSegment));
        }

        foreach (Match match in RequestMappingWithMethodRegex().Matches(fileContent))
        {
            var methodSegment = NormalizeSegment(ExtractStringFromArgs(match.Groups["args"].Value));
            routes.Add(CombineRoutes(classPrefix, methodSegment));
        }

        return routes;
    }

    private static string ExtractStringFromArgs(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return string.Empty;

        var namedRoute = NamedRouteRegex().Match(args);
        if (namedRoute.Success)
            return namedRoute.Groups[1].Value;

        var quotedRoute = QuotedStringRegex().Match(args);
        return quotedRoute.Success ? quotedRoute.Groups[1].Value : string.Empty;
    }

    private static string NormalizeSegment(string segment)
        => segment.Trim().Trim('/');

    private static string CombineRoutes(string prefix, string suffix)
    {
        if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(suffix))
            return "/";
        if (string.IsNullOrEmpty(prefix))
            return $"/{suffix}";
        if (string.IsNullOrEmpty(suffix))
            return $"/{prefix}";
        return $"/{prefix}/{suffix}";
    }
}
