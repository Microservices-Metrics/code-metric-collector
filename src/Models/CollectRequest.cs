using System.ComponentModel.DataAnnotations;

namespace CodeMetricCollector.Models;

public class CollectRequest
{
    [Required]
    [Url]
    public string UrlControllers { get; set; } = string.Empty;
}
