using System.Text.Json.Serialization;

namespace GenderClassifierApi.Models;

public sealed class GenderizeResponse
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("probability")]
    public double? Probability { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}
