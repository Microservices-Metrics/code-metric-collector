using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeMetricCollector.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GitHubService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GitHubService(HttpClient httpClient, ILogger<GitHubService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CodeMetricCollector/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

        var token = configuration["GitHub:Token"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        }
    }

    /// <summary>
    /// Fetches all source files from the given GitHub folder URL.
    /// Supports URLs in the format: https://github.com/{owner}/{repo}/tree/{branch}/{path}
    /// </summary>
    public async Task<List<GitHubFile>> GetControllerFilesAsync(string repositoryUrl)
    {
        var (owner, repo, branch, path) = ParseGitHubUrl(repositoryUrl);
        _logger.LogInformation("Fetching files from {Owner}/{Repo} at path '{Path}' (branch: {Branch})",
            owner, repo, path, branch);

        return await GetFilesRecursiveAsync(owner, repo, path, branch);
    }

    private async Task<List<GitHubFile>> GetFilesRecursiveAsync(
        string owner, string repo, string path, string branch)
    {
        var apiUrl = $"https://api.github.com/repos/{owner}/{repo}/contents/{path}?ref={Uri.EscapeDataString(branch)}";
        _logger.LogDebug("Requesting GitHub API: {Url}", apiUrl);

        var response = await _httpClient.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var items = JsonSerializer.Deserialize<List<GitHubContentItem>>(json, JsonOptions)
                    ?? [];

        var files = new List<GitHubFile>();

        foreach (var item in items)
        {
            if (item.Type == "file" && !string.IsNullOrEmpty(item.DownloadUrl))
            {
                var content = await DownloadFileAsync(item.DownloadUrl);
                files.Add(new GitHubFile(item.Name, content));
            }
            else if (item.Type == "dir")
            {
                var subFiles = await GetFilesRecursiveAsync(owner, repo, item.Path, branch);
                files.AddRange(subFiles);
            }
        }

        return files;
    }

    private async Task<string> DownloadFileAsync(string downloadUrl)
    {
        var response = await _httpClient.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static (string owner, string repo, string branch, string path) ParseGitHubUrl(string url)
    {
        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch (UriFormatException ex)
        {
            throw new ArgumentException($"Invalid URL: {url}", ex);
        }

        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"URL must point to github.com, got: {uri.Host}");

        // AbsolutePath: /{owner}/{repo}/tree/{branch}/{path...}
        var segments = uri.AbsolutePath.Trim('/').Split('/');

        if (segments.Length < 4 || !segments[2].Equals("tree", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"URL must be a GitHub tree URL in the format: https://github.com/{{owner}}/{{repo}}/tree/{{branch}}/{{path}}. Got: {url}");

        var owner = segments[0];
        var repo = segments[1];
        var branch = segments[3];
        var path = segments.Length > 4 ? string.Join("/", segments.Skip(4)) : string.Empty;

        return (owner, repo, branch, path);
    }
}

public record GitHubFile(string Name, string Content);

public class GitHubContentItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
}
