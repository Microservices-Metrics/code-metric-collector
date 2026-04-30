using System.Text.RegularExpressions;

namespace CodeMetricCollector.Services.Strategies;

/// <summary>
/// Counts HTTP endpoints in .NET controller classes.
/// Detects [HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch], [HttpHead], [HttpOptions].
/// Combines class-level [Route("...")] prefix with each method-level route.
/// </summary>
public partial class DotNetEndpointCounterStrategy : IEndpointCounterStrategy
{
    // Matches [HttpGet], [HttpPost], [HttpPut], [HttpDelete], [HttpPatch], [HttpHead], [HttpOptions]
    // Handles both [HttpGet] and [HttpGet("route")] forms.
    [GeneratedRegex(@"\[(HttpGet|HttpPost|HttpPut|HttpDelete|HttpPatch|HttpHead|HttpOptions)\s*(\((?<args>.*?)\))?\]",
        RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex HttpAttributeRegex();

    [GeneratedRegex(@"(?<block>(?:\s*\[[^\]]+\]\s*)+)\s*(?:public|private|protected|internal)\s+[^{;=]+\(",
        RegexOptions.Multiline)]
    private static partial Regex MethodAttributeBlockRegex();

    // Matches [Route("...")] at class level.
    [GeneratedRegex(@"\[Route\(""(?<route>[^""]*)""\)\]", RegexOptions.Multiline)]
    private static partial Regex ClassRouteRegex();

    [GeneratedRegex(@"\[Route\s*\((?<args>.*?)\)\]", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex RouteAttributeRegex();

    [GeneratedRegex(@"\bclass\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Multiline)]
    private static partial Regex ClassNameRegex();

    [GeneratedRegex("\"([^\"]*)\"")]
    private static partial Regex QuotedStringRegex();

    [GeneratedRegex(@"\{\s*(?<name>[^}:\s]+)\s*:[^}]+\}")]
    private static partial Regex RouteConstraintRegex();

    public bool CanHandle(string fileName) =>
        fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> ExtractRoutes(string fileContent)
    {
        var classNameMatch = ClassNameRegex().Match(fileContent);
        var controllerName = classNameMatch.Success
            ? GetControllerTokenValue(classNameMatch.Groups["name"].Value)
            : string.Empty;

        var classRouteMatch = ClassRouteRegex().Match(fileContent);
        var classPrefix = classRouteMatch.Success
            ? NormalizeSegment(ResolveRouteTokens(classRouteMatch.Groups["route"].Value, controllerName))
            : string.Empty;

        var routes = new List<string>();
        var methodBlocks = MethodAttributeBlockRegex().Matches(fileContent);

        foreach (Match blockMatch in methodBlocks)
        {
            var block = blockMatch.Groups["block"].Value;
            var methodRouteFromRouteAttribute = ExtractRouteFromBlockRouteAttribute(block);

            foreach (Match httpMatch in HttpAttributeRegex().Matches(block))
            {
                var httpArgs = httpMatch.Groups["args"].Value;
                var routeFromHttpAttribute = ExtractRouteFromArgs(httpArgs);
                var methodRoute = string.IsNullOrWhiteSpace(routeFromHttpAttribute)
                    ? methodRouteFromRouteAttribute
                    : routeFromHttpAttribute;

                methodRoute = NormalizeRouteTemplate(methodRoute);
                methodRoute = ResolveRouteTokens(methodRoute, controllerName);
                routes.Add(CombineRoutes(classPrefix, methodRoute));
            }
        }

        return routes;
    }

    private static string NormalizeSegment(string segment)
        => segment.Trim().Trim('/');

    private static string ResolveRouteTokens(string route, string controllerName)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return string.Empty;
        }

        return route.Replace("[controller]", controllerName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetControllerTokenValue(string className)
    {
        const string suffix = "Controller";

        if (className.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return className[..^suffix.Length];
        }

        return className;
    }

    private static string ExtractRouteFromArgs(string args)
    {
        var quotedRoute = QuotedStringRegex().Match(args);
        return quotedRoute.Success ? quotedRoute.Groups[1].Value : string.Empty;
    }

    private static string ExtractRouteFromBlockRouteAttribute(string methodAttributeBlock)
    {
        var routeAttributeMatch = RouteAttributeRegex().Match(methodAttributeBlock);
        if (!routeAttributeMatch.Success)
        {
            return string.Empty;
        }

        return ExtractRouteFromArgs(routeAttributeMatch.Groups["args"].Value);
    }

    private static string NormalizeRouteTemplate(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return string.Empty;
        }

        return RouteConstraintRegex().Replace(route, m => $"{{{m.Groups["name"].Value}}}");
    }

    private static string CombineRoutes(string prefix, string methodRoute)
    {
        if (string.IsNullOrWhiteSpace(methodRoute) && string.IsNullOrWhiteSpace(prefix))
            return "/";

        if (IsAbsoluteRoute(methodRoute))
        {
            return $"/{NormalizeSegment(StripAbsolutePrefix(methodRoute))}";
        }

        var normalizedPrefix = NormalizeSegment(prefix);
        var normalizedMethod = NormalizeSegment(methodRoute);

        if (string.IsNullOrEmpty(normalizedPrefix) && string.IsNullOrEmpty(normalizedMethod))
            return "/";
        if (string.IsNullOrEmpty(normalizedPrefix))
            return $"/{normalizedMethod}";
        if (string.IsNullOrEmpty(normalizedMethod))
            return $"/{normalizedPrefix}";
        return $"/{normalizedPrefix}/{normalizedMethod}";
    }

    private static bool IsAbsoluteRoute(string route)
        => !string.IsNullOrWhiteSpace(route)
           && (route.StartsWith('/') || route.StartsWith("~/", StringComparison.Ordinal));

    private static string StripAbsolutePrefix(string route)
    {
        if (route.StartsWith("~/", StringComparison.Ordinal))
        {
            return route[2..];
        }

        return route;
    }
}
